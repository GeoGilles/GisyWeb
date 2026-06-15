using GisyWeb.Infrastructure.AdminDb;
using GisyWeb.Infrastructure.AdminDb.Entities;
using GisyWeb.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GisyWeb.Infrastructure.Services
{
    public class AdminService
    {
        private readonly IDbContextFactory<Carto5AdminContext> _factory;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly SystemContextService _sysContext;

        public AdminService(IDbContextFactory<Carto5AdminContext> factory, IConfiguration configuration, SystemContextService sysContext)
        {
            _factory = factory;
            _configuration = configuration;
            _sysContext = sysContext;
        }

        // =========================================================
        // 1. LECTURE : UTILISATEURS & SÉCURITÉ (Logic SAS)
        // =========================================================

        public async Task<List<CartosClient>> GetTousLesClientsAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Clients
                           .AsNoTracking()
                           .OrderBy(c => c.Name)
                           .ToListAsync();
        }

        /// <summary>
        /// Retourne uniquement les comptes de type UTILISATEURS (Humains/Gabarits) pour la liste de gauche.
        /// </summary>
        public async Task<List<CartosClient>> GetUniquementUtilisateursAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Clients
                           .AsNoTracking()
                           // Humains = Tout ce qui n'est pas explicitement un SYSTEM
                           .Where(c => c.SysRoleCode != "SYSTEM")
                           .OrderBy(c => c.Name)
                           .ToListAsync();
        }

        /// <summary>
        /// Retourne uniquement les MODULES / SYSTÈMES (SIAS, MRG, VHR...) pour l'orchestration.
        /// </summary>
        public async Task<List<CartosClient>> GetUniquementSystemesAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Clients
                           .AsNoTracking()
                           // Systèmes = Tout ce qui porte le rôle SYSTEM (Modules + Gabarits)
                           .Where(c => c.SysRoleCode == "SYSTEM")
                           .OrderBy(c => c.ClientCode)
                           .ToListAsync();
        }


        public async Task<List<CartosClient>> GetEnvironnementsDisponiblesAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            var codesAvecApp = await db.ClientApps
                                       .Select(a => a.ClientCode)
                                       .Distinct()
                                       .ToListAsync();

            return await db.Clients
                           .AsNoTracking()
                           .Where(c => codesAvecApp.Contains(c.ClientCode))
                           .OrderBy(c => c.ClientCode)
                           .ToListAsync();
        }

        public async Task<List<string>> GetAccesUtilisateurAsync(string userCode)
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.UserClientAccesses
                           .AsNoTracking()
                           .Where(a => a.UserClientCode == userCode)
                           .Select(a => a.AppClientCode)
                           .ToListAsync();
        }

        // =========================================================
        // 2. LECTURE : UI & CONFIGURATION (Logic Carto5)
        // =========================================================

        public async Task<List<CartoProfil>> GetProfilsTechniquesAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            // Retourne tous les profils, triés par état actif d'abord
            return await db.CartoProfils
                .AsNoTracking()
                .OrderByDescending(p => p.IsActive)
                .ThenBy(p => p.Label)
                .ToListAsync();
        }

        public async Task<string?> GetUserDefaultProfilGuidAsync(string userCode)
        {
            using var db = await _factory.CreateDbContextAsync();
            var lien = await db.UserProfils
                               .AsNoTracking()
                               .FirstOrDefaultAsync(p => p.UserCode == userCode && p.IsDefault == 1);
            return lien?.ProfilGuid;
        }

        // =========================================================
        // 3. ÉCRITURE : CŒUR TRANSACTIONNEL
        // =========================================================

        public async Task<(bool Success, string Message)> SauvegarderClientAvecAccesAsync(
        CartosClient client,
        List<string> codesEnvironnements,
        string? profilGuid)
        {
            using var db = await _factory.CreateDbContextAsync();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                string userCode = client.ClientCode?.Trim().ToUpper() ?? throw new Exception("CLIENT_CODE manquant.");

                // --- FIX FERRARI : SÉCURISATION DU GUID ---
                // Si le client n'a pas de GlobalId, on en génère un TOUT DE SUITE
                if (string.IsNullOrEmpty(client.GlobalId))
                {
                    client.GlobalId = Guid.NewGuid().ToString().ToUpper();
                }

                // 1. ÉTAPE 1 : GESTION DE CARTO5_CLIENT
                var existing = await db.Clients.FirstOrDefaultAsync(u => u.ClientCode == userCode);
                if (existing == null)
                {
                    client.ClientCode = userCode;
                    client.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    client.IsActive = true;
                    db.Clients.Add(client);
                }
                else
                {
                    existing.Name = client.Name;
                    existing.Email = client.Email;
                    existing.IsActive = client.IsActive;
                    existing.SysRoleCode = client.SysRoleCode;
                    existing.GlobalId = client.GlobalId; // On s'assure qu'il est synchronisé
                    existing.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                await db.SaveChangesAsync();

                // 2. ÉTAPE 2 : SYNCHRO AVEC CARTO5_USER (Le parent des FK)
                // Note l'utilisation explicite de client.GlobalId pour le paramètre {1}
                await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO CARTO5_USER (USER_CODE, USER_GUID, FULL_NAME, EMAIL, IS_ACTIVE, SYS_ROLE_CODE)
            VALUES ({0}, {1}, {2}, {3}, 1, {4})
            ON CONFLICT(USER_CODE) DO UPDATE SET 
                USER_GUID = excluded.USER_GUID,
                FULL_NAME = excluded.FULL_NAME, 
                EMAIL = excluded.EMAIL,
                SYS_ROLE_CODE = excluded.SYS_ROLE_CODE",
                    userCode, client.GlobalId, client.Name, client.Email, client.SysRoleCode);

                // 3. ÉTAPE 3 : LES ACCÈS SYSTÈMES (Maintenant la FK va passer !)
                await db.Database.ExecuteSqlRawAsync("DELETE FROM CARTO5_USER_ACCESS WHERE USER_CODE = {0}", userCode);
                if (codesEnvironnements != null)
                {
                    foreach (var env in codesEnvironnements)
                    {
                        await db.Database.ExecuteSqlRawAsync(
                            "INSERT INTO CARTO5_USER_ACCESS (USER_CODE, SYSTEM_CODE, ROLE_CODE) VALUES ({0}, {1}, {2})",
                            userCode, env, client.SysRoleCode ?? "USER");
                    }
                }

                // 4. ÉTAPE 4 : LE PILOTAGE TECHNIQUE (SETTINGS)
                await db.Database.ExecuteSqlRawAsync("DELETE FROM CARTO5_CLIENT_SETTING WHERE CLIENT_CODE = {0} AND SETTING_KEY = 'GUID_SITE_WRITER'", userCode);
                if (!string.IsNullOrEmpty(profilGuid))
                {
                    await db.Database.ExecuteSqlRawAsync(
                        @"INSERT INTO CARTO5_CLIENT_SETTING (CLIENT_CODE, SCOPE, SETTING_KEY, SETTING_VALUE, VALUE_TYPE, CREATED_AT) 
                  VALUES ({0}, 'USER_CONFIG', 'GUID_SITE_WRITER', {1}, 'STRING', {2})",
                        userCode, profilGuid, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                // 5. ÉTAPE 5 : LE PROFIL
                await db.Database.ExecuteSqlRawAsync("DELETE FROM CARTO5_USER_PROFIL WHERE USER_CODE = {0}", userCode);
                if (!string.IsNullOrEmpty(profilGuid))
                {
                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO CARTO5_USER_PROFIL (USER_CODE, PROFIL_GUID, IS_DEFAULT) VALUES ({0}, {1}, 1)",
                        userCode, profilGuid);
                }

                await transaction.CommitAsync();
                return (true, "Provisionning réussi.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "DÉTAIL SQL : " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        public async Task<bool> AutoProvisionnerUsagerAsync(string code, string nom)
        {
            using var db = await _factory.CreateDbContextAsync();

            // 1. Sécurité : On vérifie s'il n'a pas été créé par une autre session entre temps
            if (await db.Clients.AnyAsync(u => u.ClientCode == code)) return true;

            Console.WriteLine($"🛠️ [AUTO-PROVISION] Création du compte pour {nom} ({code})...");

            // 2. On utilise notre logique de clonage basée sur le modèle GLAVOIE
            return await ProvisionnerUsagerDepuisModeleAsync(code, nom, "GLAVOIE");
        }

        // =========================================================
        // 4. DONNÉES MÉTIERS & STATS
        // =========================================================

        public async Task<List<CartoProjtPoint>> GetProjetsParUtilisateurAsync(string userCode)
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Projets
                           .AsNoTracking()
                           .Where(p => p.NumUtilsCretn == userCode && (p.IndSuprsLogq == 0 || p.IndSuprsLogq == null))
                           .OrderByDescending(p => p.DahCretn)
                           .ToListAsync();
        }

        public async Task<List<CartoSitePoint>> GetSitesParProjetAsync(int idProjet)
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Sites
                           .AsNoTracking()
                           .Where(s => s.IdeProjt == idProjet)
                           .OrderBy(s => s.NomSite)
                           .ToListAsync();
        }

        public class AdminStatsDto { public int Users; public int Projets; public int Sites; }

        public async Task<AdminStatsDto> GetStatsAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return new AdminStatsDto
            {
                Users = await db.Clients.CountAsync(),
                Projets = await db.Projets.CountAsync(),
                Sites = await db.Sites.CountAsync()
            };
        }

        // =========================================================
        // 5. HELPERS (DT & PROVISIONNING)
        // =========================================================
        public class DtItem { public string Code { get; set; } = ""; public string Nom { get; set; } = ""; }

        public async Task<List<DtItem>> GetListeDirectionsTerritorialesAsync()
        {
            var liste = new List<DtItem>();
            string dbPath = "";
            const string GUID_DATA = "74FCA8F1-887B-4387-AEA9-F6A1F1598A04";

            try
            {
                using (var db = await _factory.CreateDbContextAsync())
                {
                    var profil = await db.CartoProfils.AsNoTracking().FirstOrDefaultAsync(p => p.GlobalId == GUID_DATA);
                    if (profil != null)
                    {
                       // var mocService = new MocService(null, null, _sysContext);
                       // dbPath = mocService.ResoudreCheminComplet(profil);
                    }
                }

                if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath)) return liste;

                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Mode=ReadOnly"))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COD_NIV_HIERC_1, COD_NIV_HIERC_2, NOM_DIRCT_TERRT FROM ADM_DIRECTION_TERRITORIALE ORDER BY NOM_DIRCT_TERRT";

                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            if (r.IsDBNull(2)) continue;
                            string c1 = r.IsDBNull(0) ? "00" : r.GetString(0);
                            string c2 = r.IsDBNull(1) ? "00" : r.GetString(1);
                            string nom = r.GetString(2);
                            string fullCode = $"{c1}{c2}0000";
                            liste.Add(new DtItem { Code = fullCode, Nom = nom });
                        }
                    }
                }
            }
            catch { }
            return liste;
        }

        public async Task<bool> ProvisionnerUsagerDepuisModeleAsync(string codeNouveau, string nomNouveau, string codeModele = "GLAVOIE")
        {
            using var db = await _factory.CreateDbContextAsync();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var settingModele = await db.ClientSettings
                    .FirstOrDefaultAsync(s => s.ClientCode == codeModele && s.SettingKey == "GUID_SITE_WRITER");

                if (settingModele == null) throw new Exception("Profil modèle introuvable.");

                var profilModele = await db.CartoProfils
                    .FirstOrDefaultAsync(p => p.GlobalId == settingModele.SettingValue);

                var nouveauProfil = new CartoProfil
                {
                    GlobalId = Guid.NewGuid().ToString().ToUpper(),
                    Code = $"{codeNouveau}_PROF",
                    Label = $"Profil de {nomNouveau}",
                    EngineKind = profilModele.EngineKind,
                    GeometryKind = profilModele.GeometryKind,
                    IsActive = 1,
                    EsriMobilePath = profilModele.EsriMobilePath.Replace(codeModele, codeNouveau, StringComparison.OrdinalIgnoreCase),
                    SqliteDataPath = profilModele.SqliteDataPath,
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                db.CartoProfils.Add(nouveauProfil);

                db.ClientSettings.Add(new CartoClientSetting
                {
                    ClientCode = codeNouveau,
                    SettingKey = "GUID_SITE_WRITER",
                    SettingValue = nouveauProfil.GlobalId,
                    Scope = "USER_CONFIG",
                    CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"[ERR] Échec du clonage usager : {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProvisionnerUsagerDepuisGabaritAsync(string codeUsager, string nomUsager, string moduleActif)
        {
            using var db = await _factory.CreateDbContextAsync();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                codeUsager = codeUsager.ToUpper().Trim();
                moduleActif = moduleActif.ToUpper().Trim();
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // =====================================================================
                // ÉTAPE 1 : RÉSOLUTION DYNAMIQUE DE LA RACINE (via SystemContextService)
                // =====================================================================
                // ⚠️ IDÉALEMENT : injecte SystemContextService dans le constructeur
                // Pour l'instant, on utilise la variable d'environnement ou la config
                string racine = _sysContext?.ExternalClientsRootActif
                  ?? _configuration["Carto5:ExternalClientsRoot"]
                  ?? string.Empty;

                // CHEMIN RELATIF
                string dossierRelatif = $"Carto5Data_{moduleActif}\\{codeUsager}";
                string fichierRelatifGpkg = $"{dossierRelatif}\\{codeUsager}_SPATAdmin_100.gpkg";

                // CHEMIN ABSOLU (pour création physique)
                string dossierAbsolu = Path.Combine(racine, dossierRelatif);
                string fichierAbsoluGpkg = Path.Combine(racine, fichierRelatifGpkg);

                string cheminGabaritGpkg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Admin", "Cartotheque", "Gabarit_SPATAdmin_100.gpkg");

                // =====================================================================
                // ÉTAPE 2 : CRÉATION PHYSIQUE SUR LE DISQUE
                // =====================================================================
                if (!Directory.Exists(dossierAbsolu))
                {
                    Directory.CreateDirectory(dossierAbsolu);
                    Console.WriteLine($"📂 [INFRA] Nouveau dossier créé : {dossierAbsolu}");
                }

                if (!File.Exists(fichierAbsoluGpkg))
                {
                    if (File.Exists(cheminGabaritGpkg))
                    {
                        File.Copy(cheminGabaritGpkg, fichierAbsoluGpkg);
                        Console.WriteLine($"📄 [INFRA] Gabarit GPKG déployé : {fichierAbsoluGpkg}");
                    }
                    else
                    {
                        throw new FileNotFoundException("ERREUR CRITIQUE : Gabarit GPKG source introuvable.", cheminGabaritGpkg);
                    }
                }

                // =====================================================================
                // ÉTAPE 3 : IDENTITÉ - TABLE 'CARTO5_CLIENT'
                // =====================================================================
                string masterGuid;
                var client = await db.Clients.FirstOrDefaultAsync(c => c.ClientCode == codeUsager);

                if (client == null)
                {
                    masterGuid = Guid.NewGuid().ToString().ToUpper();
                    client = new CartosClient
                    {
                        ClientCode = codeUsager,
                        Name = nomUsager,
                        IsActive = true,
                        CodModOri = "GLO",
                        GlobalId = masterGuid
                    };
                    db.Clients.Add(client);
                }
                else
                {
                    masterGuid = client.GlobalId;
                }
                await db.SaveChangesAsync();

                // =====================================================================
                // ÉTAPE 4 : IDENTITÉ - TABLE 'CARTO5_USER'
                // =====================================================================
                var identity = await db.Users.FirstOrDefaultAsync(u => u.UserCode == codeUsager);
                if (identity == null)
                {
                    db.Users.Add(new CartoUser
                    {
                        UserCode = codeUsager,
                        UserGuid = masterGuid,
                        FullName = nomUsager,
                        IsActive = 1,
                        SysRoleCode = "USER",
                        CodModOri = "GLO"
                    });
                    await db.SaveChangesAsync();
                }

                // =====================================================================
                // ÉTAPE 5 : PROFIL TECHNIQUE — CHEMINS RELATIFS, GPKG UNIQUEMENT
                // =====================================================================
                string codeProfilFormate = $"250_SPATCLIENT_{codeUsager}_{moduleActif}";
                string labelProfilFormate = $"250. {codeUsager}_SPATAdmin_100.gpkg ({moduleActif})";

                var profil = await db.CartoProfils.FirstOrDefaultAsync(p => p.Code == codeProfilFormate);
                string profilGuid = profil?.GlobalId ?? Guid.NewGuid().ToString().ToUpper();

                if (profil == null)
                {
                    profil = new CartoProfil
                    {
                        GlobalId = profilGuid,
                        Code = codeProfilFormate,
                        Label = labelProfilFormate,
                        EngineKind = "Sqlite",
                        GeometryKind = "Point",
                        EsriMobilePath = "",
                        SqliteDataPath = fichierRelatifGpkg,
                        IsActive = 1,
                        ClientCode = codeUsager,
                        CodModOri = "GLO",
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    db.CartoProfils.Add(profil);
                }
                else
                {
                    profil.Label = labelProfilFormate;
                    profil.EngineKind = "Sqlite";
                    profil.EsriMobilePath = "";
                    profil.SqliteDataPath = fichierRelatifGpkg;
                    profil.IsActive = 1;
                    profil.UpdatedAt = now;
                }
                await db.SaveChangesAsync();

                // =====================================================================
                // ÉTAPE 6 : PONTS POUR L'INTERFACE
                // =====================================================================
                await db.Database.ExecuteSqlRawAsync("DELETE FROM CARTO5_USER_PROFIL WHERE USER_CODE = {0}", codeUsager);
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO CARTO5_USER_PROFIL (USER_CODE, PROFIL_GUID, IS_DEFAULT, COD_MOD_ORI) VALUES ({0}, {1}, 1, 'GLO')",
                    codeUsager, profilGuid);

                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO CARTO5_USER_ACCESS (USER_CODE, SYSTEM_CODE, ROLE_CODE, COD_MOD_ORI) VALUES ({0}, {1}, 'USER', 'GLO') ON CONFLICT DO NOTHING",
                    codeUsager, moduleActif);

                // =====================================================================
                // ÉTAPE 7 : SETTINGS — GUID_SITE_WRITER pour le module actif
                // =====================================================================
                string settingKey = $"GUID_SITE_WRITER_{moduleActif}";
                var setting = await db.ClientSettings.FirstOrDefaultAsync(s =>
                    s.ClientCode == codeUsager && s.SettingKey == settingKey);

                if (setting == null)
                {
                    db.ClientSettings.Add(new CartoClientSetting
                    {
                        ClientCode = codeUsager,
                        Scope = "USER_CONFIG",
                        SettingKey = settingKey,
                        SettingValue = profilGuid,
                        CodModOri = "GLO"
                    });
                }
                else
                {
                    setting.SettingValue = profilGuid;
                }

                // Aussi sauvegarder le GUID_SITE_WRITER générique (fallback)
                var settingGenerique = await db.ClientSettings.FirstOrDefaultAsync(s =>
                    s.ClientCode == codeUsager && s.SettingKey == "GUID_SITE_WRITER");
                if (settingGenerique == null)
                {
                    db.ClientSettings.Add(new CartoClientSetting
                    {
                        ClientCode = codeUsager,
                        Scope = "USER_CONFIG",
                        SettingKey = "GUID_SITE_WRITER",
                        SettingValue = profilGuid,
                        CodModOri = "GLO"
                    });
                }
                else
                {
                    settingGenerique.SettingValue = profilGuid;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"✅ [Admin] Provisionning réussi pour : {codeUsager} dans {moduleActif}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ [Admin-ERREUR] Échec pour {codeUsager} : {ex.Message}");
                return false;
            }
        }


    }
}