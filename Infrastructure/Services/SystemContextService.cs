// @author: Gilles Lavoie - Architecte principal - 2024-2026
using Microsoft.EntityFrameworkCore;
using GisyWeb.Infrastructure.AdminDb;
using GisyWeb.Infrastructure.AdminDb.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace GisyWeb.Services
{
    public class SystemContextService
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private readonly IDbContextFactory<Carto5AdminContext> _dbFactory;


        // === Carto5 — Architecture Distribuée ===
        private Microsoft.Data.Sqlite.SqliteConnection? _connexionFraSession;
        private string _racine = string.Empty;
        private string _cheminFra = string.Empty;
        private string _cheminSat = string.Empty;
        private bool _estInitialiseCarto5 = false;
        private readonly object _verrouInitCarto5 = new object();


       // === Carto5 — Modes Environnement (Simulateur) ===
        public enum ModeEnvironnement { Local, Bureau, Camion, Distant }
        public ModeEnvironnement ModeActif { get; private set; } = ModeEnvironnement.Local;
        public bool RacineEstAccessible => Directory.Exists(_racine);
        public string RacineActive => _racine;
        public string ExternalClientsRootActif { get; private set; } = string.Empty;


        public string InstanceCode { get; private set; } = "UNK";
        public string InstanceName { get; private set; } = "Chargement...";
        public bool IsOfflineMode { get; private set; } = false;
        public bool IsNetworkAvailable { get; private set; } = true;

        public event Action? OnContextChanged;

        private readonly IConfiguration _configuration;

        public SystemContextService(IDbContextFactory<Carto5AdminContext> dbFactory, IConfiguration configuration)
        {
            _dbFactory = dbFactory;
            _configuration = configuration;
            ExternalClientsRootActif = _configuration["Carto5:ExternalClientsRoot"] ?? string.Empty;
        }

        private static readonly System.Threading.SemaphoreSlim _syncLock = new System.Threading.SemaphoreSlim(1, 1);

        public async Task InitializeAsync()
        {
            await _syncLock.WaitAsync();
            try
            {
                string binPath = AppContext.BaseDirectory;
                string spatialitePath = binPath;
                string subFolderPath = Path.Combine(binPath, "SpatialiteLibs");

                if (string.IsNullOrEmpty(_racine))
                {
                    // En dev, utiliser le ContentRootPath (qui pointe vers le projet, pas bin)
                    _racine = _configuration["Environnement:Racine"] ?? AppContext.BaseDirectory;
                }

                if (Directory.Exists(subFolderPath) && File.Exists(Path.Combine(subFolderPath, "mod_spatialite.dll")))
                {
                    spatialitePath = subFolderPath;
                }

                string extensionPath = Path.Combine(spatialitePath, "mod_spatialite.dll");

                if (!File.Exists(extensionPath))
                {
                    throw new FileNotFoundException($"Le fichier mod_spatialite.dll est introuvable : {extensionPath}");
                }

                SetDllDirectory(spatialitePath);

                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                if (!currentPath.Contains(spatialitePath))
                {
                    Environment.SetEnvironmentVariable("PATH", spatialitePath + ";" + currentPath, EnvironmentVariableTarget.Process);
                }

                using var db = await _dbFactory.CreateDbContextAsync();
                await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=TRUNCATE; PRAGMA locking_mode=NORMAL;");

                var connection = db.Database.GetDbConnection() as Microsoft.Data.Sqlite.SqliteConnection;
                if (connection != null)
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                        await connection.OpenAsync();
                }

                var activeClient = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.IsDefault == 1 && c.IsActive);
                if (activeClient != null)
                {
                    InstanceCode = activeClient.ClientCode;
                    InstanceName = activeClient.Name;
                }

                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                throw new Exception($"[GisyWeb] Erreur Initialisation. {ex.Message}", ex);
            }
            finally
            {
                _syncLock.Release();
            }
        }


       private string _racineOriginale = string.Empty;
       public void BasculerMode(ModeEnvironnement mode)
        {
            ModeActif = mode;

            if (string.IsNullOrEmpty(_racineOriginale))
                _racineOriginale = _racine;

            switch (mode)
            {
                case ModeEnvironnement.Local:
                    _racine = Environment.GetEnvironmentVariable("Carto5_DATA_ROOT")
                              ?? _configuration["Environnement:Racine"]
                              ?? _racineOriginale;
                    IsOfflineMode = false;
                    IsNetworkAvailable = true;
                    break;

                case ModeEnvironnement.Bureau:
                    _racine = Environment.GetEnvironmentVariable("Carto5_DATA_ROOT_RESEAU")
                              ?? _configuration["Environnement:RacineReseau"]
                              ?? _racineOriginale;
                    IsOfflineMode = false;
                    IsNetworkAvailable = true;
                    break;

                case ModeEnvironnement.Camion:
                    _racine = Environment.GetEnvironmentVariable("Carto5_DATA_ROOT_CAMION")
                              ?? _configuration["Environnement:RacineCamion"]
                              ?? _racineOriginale;
                    IsOfflineMode = true;
                    IsNetworkAvailable = false;
                    break;

                case ModeEnvironnement.Distant:
                    _racine = Environment.GetEnvironmentVariable("Carto5_DATA_ROOT_RESEAU")
                              ?? _configuration["Environnement:RacineReseau"]
                              ?? _racineOriginale;
                    IsOfflineMode = false;
                    IsNetworkAvailable = true;
                    break;
            }
            NotifyStateChanged();
        }


        public async Task SauvegarderModeAsync()
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var setting = await db.ClientSettings
                    .FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "SYS_MODE_ENVIRONNEMENT");

                var valeur = ModeActif.ToString();
                if (setting == null)
                {
                    db.ClientSettings.Add(new CartoClientSetting
                    {
                        ClientCode = InstanceCode,
                        SettingKey = "SYS_MODE_ENVIRONNEMENT",
                        SettingValue = valeur,
                        Scope = "SYSTEM"
                    });
                }
                else
                {
                    setting.SettingValue = valeur;
                }
                await db.SaveChangesAsync();
            }
            catch { /* Silencieux */ }
        }

        public async Task RestaurerModeAsync()
        {
            try
            {
                // Ne rien faire tant que InitializeAsync n'a pas terminé
                if (InstanceCode == "UNK" || _dbFactory == null)
                    return;

                using var db = await _dbFactory.CreateDbContextAsync();
                var setting = await db.ClientSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "SYS_MODE_ENVIRONNEMENT");

                if (setting != null && Enum.TryParse<ModeEnvironnement>(setting.SettingValue, out var modeSauvegarde))
                {
                    await BasculerModeAvecReconnexionAsync(modeSauvegarde);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Write($"[Carto5] RestaurerModeAsync ignoré : {ex.Message}");
            }
        }


        /// <summary>
        /// Carto5 — Change le mode (Local/Bureau/Distant/Camion).
        /// La FRA et les SAT restent TOUJOURS en local.
        /// Seuls ExternalClientsRootActif et _racine changent selon le mode.
        /// </summary>
        public async Task BasculerModeAvecReconnexionAsync(ModeEnvironnement mode)
        {
            try
            {
                if (string.IsNullOrEmpty(_racine))
                {
                    _racine = _configuration["Environnement:Racine"] ?? AppContext.BaseDirectory;
                }

                FileLogger.Write($"[Carto5-MODE] Demande de changement de mode : {mode}");

                if (string.IsNullOrEmpty(_racineOriginale))
                    _racineOriginale = _racine;

                // 1. Résoudre la racine selon le mode
                string nouvelleRacine = mode switch
                {
                    ModeEnvironnement.Local => Environment.GetEnvironmentVariable("Carto5_DATA_ROOT") ?? _configuration["Environnement:Racine"] ?? (!string.IsNullOrEmpty(_racine) ? _racine : _racineOriginale),
                    ModeEnvironnement.Bureau => ObtenirDataRootSelonEnvironnement(),
                    ModeEnvironnement.Camion => Environment.GetEnvironmentVariable("Carto5_DATA_ROOT_CAMION") ?? _configuration["Environnement:RacineCamion"] ?? (!string.IsNullOrEmpty(_racine) ? _racine : _racineOriginale),
                    ModeEnvironnement.Distant => ObtenirDataRootSelonEnvironnement(),
                    _ => _racineOriginale
                };

                // 2. Résoudre le chemin des clients externes selon le mode
                string clientsRoot = mode switch
                {
                    ModeEnvironnement.Local => Environment.GetEnvironmentVariable("Carto5_EXTERNAL_CLIENTS") ?? _configuration["Carto5:ExternalClientsRoot"] ?? string.Empty,
                    ModeEnvironnement.Bureau => ObtenirRacineReseauSelonEnvironnement(),
                    ModeEnvironnement.Distant => ObtenirRacineReseauSelonEnvironnement(),
                    ModeEnvironnement.Camion => Environment.GetEnvironmentVariable("Carto5_EXTERNAL_CLIENTS") ?? _configuration["Carto5:ExternalClientsRoot"] ?? string.Empty,
                    _ => _configuration["Carto5:ExternalClientsRoot"] ?? string.Empty
                };

                FileLogger.Write($"[Carto5-MODE] Racine actuelle : '{_racine}'");
                FileLogger.Write($"[Carto5-MODE] Nouvelle racine : '{nouvelleRacine}'");
                FileLogger.Write($"[Carto5-MODE] ExternalClientsRoot : '{clientsRoot}'");

                ExternalClientsRootActif = clientsRoot;
                ModeActif = mode;
                IsOfflineMode = mode == ModeEnvironnement.Camion;
                IsNetworkAvailable = mode != ModeEnvironnement.Camion;

                // 3. Mettre à jour _racine SANS reconnecter la FRA
                if (!string.IsNullOrEmpty(nouvelleRacine) && nouvelleRacine != _racine)
                {
                    _racine = nouvelleRacine;
                    FileLogger.Write($"[Carto5-MODE] _racine mise à jour : '{_racine}'");
                    FileLogger.Write($"[Carto5-MODE] Directory.Exists : {Directory.Exists(_racine)}");
                }
                else
                {
                    FileLogger.Write($"[Carto5-MODE] _racine inchangée (déjà à jour ou vide)");
                }

                // Forcer la réinitialisation des connexions SQLite
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

                FileLogger.Write($"[Carto5-MODE] Changement terminé. ModeActif={ModeActif}, IsOffline={IsOfflineMode}");
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                FileLogger.Write($"[Carto5] Erreur BasculerModeAvecReconnexionAsync : {ex.Message}");
            }
        }

        /// <summary>
        /// Détecte automatiquement le réseau MTQ vs Maison et retourne la bonne racine
        /// </summary>
        private string ObtenirRacineReseauSelonEnvironnement()
        {
            bool estSurReseauMTQ = Environment.UserDomainName.Contains("MTQ", StringComparison.OrdinalIgnoreCase)
                                || Environment.UserDomainName.Contains("MIN", StringComparison.OrdinalIgnoreCase);

            if (estSurReseauMTQ)
            {
                return _configuration["Carto5:ExternalClientsRootReseauMTQ"]
                    ?? _configuration["Carto5:ExternalClientsRootReseau"]
                    ?? _configuration["Carto5:ExternalClientsRoot"]
                    ?? string.Empty;
            }
            else
            {
                return _configuration["Carto5:ExternalClientsRootReseau"]
                    ?? _configuration["Carto5:ExternalClientsRoot"]
                    ?? string.Empty;
            }
        }


        private string ObtenirDataRootSelonEnvironnement()
        {
            bool estSurReseauMTQ = Environment.UserDomainName.Contains("MTQ", StringComparison.OrdinalIgnoreCase)
                                || Environment.UserDomainName.Contains("MIN", StringComparison.OrdinalIgnoreCase);

            if (estSurReseauMTQ)
            {
                return _configuration["Environnement:RacineReseauMTQ"]
                    ?? _configuration["Environnement:RacineReseau"]
                    ?? _racine;
            }
            else
            {
                return _configuration["Environnement:RacineReseau"]
                    ?? _racine;
            }
        }

        /// <summary>
        /// Carto5 — Change le satellite attaché pour un nouveau module.
        /// </summary>
        public async Task ChangerSatelliteAsync(string nouveauCode)
        {
            if (_connexionFraSession == null)
                return;

            if (string.IsNullOrEmpty(_cheminSat))
                return;

            var dossierAdmin = "Admin\\Carto5Data";
            var nouveauCheminSat = Path.Combine(_racine, dossierAdmin, $"{nouveauCode.ToUpper()}_SAT_Admin.db");

            // Si c'est le même satellite, on ne fait rien
            if (nouveauCheminSat.Equals(_cheminSat, StringComparison.OrdinalIgnoreCase))
                return;

            // Attendre que les requêtes en cours se terminent
            await Task.Delay(300);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            using (var cmd = _connexionFraSession.CreateCommand())
            {
                // 1. Détacher l'ancien
                try
                {
                    cmd.CommandText = "DETACH DATABASE sat;";
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    FileLogger.Write($"[Carto5] Détachement ancien satellite ignoré : {ex.Message}");
                }

                // 2. Attacher le nouveau
                cmd.CommandText = $"ATTACH DATABASE '{nouveauCheminSat}' AS sat;";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = "PRAGMA sat.journal_mode=TRUNCATE; PRAGMA sat.synchronous=NORMAL;";
                await cmd.ExecuteNonQueryAsync();
            }

            _cheminSat = nouveauCheminSat;
        }


        /// <summary>
        /// Carto5 — Initialise l'architecture distribuée FRA/SAT.
        /// Appel UNIQUE au bootstrap de session (jamais dans une action UI).
        /// </summary>
        public async Task InitialiserArchitectureDistribueeAsync(string systemeCode = "SIAS")
        {
            if (_estInitialiseCarto5)
                return;

            lock (_verrouInitCarto5)
            {
                if (_estInitialiseCarto5)
                    return;
            }

            // 1. Résoudre la racine depuis IConfiguration (appsettings.json)
            _racine = Environment.GetEnvironmentVariable("Carto5_DATA_ROOT")
            ?? _configuration["Environnement:Racine"]
            ?? AppContext.BaseDirectory;
            var dossierAdmin = "Admin\\Carto5Data";

            // 2. Construire les chemins FRA et SAT
            _cheminFra = Path.Combine(_racine, dossierAdmin, "Carto5_FRA_Admin.db");
            _cheminSat = Path.Combine(_racine, dossierAdmin, $"{systemeCode.ToUpper()}_SAT_Admin.db");

            // 3. Ouvrir la connexion persistante à la FRA
            var fraConnString = $"Data Source={_cheminFra};Cache=Shared;Mode=ReadWrite";
            _connexionFraSession = new Microsoft.Data.Sqlite.SqliteConnection(fraConnString);
            await _connexionFraSession.OpenAsync();

            // 4. PRAGMA d'optimisation sur la FRA
            using (var cmd = _connexionFraSession.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=TRUNCATE; PRAGMA locking_mode=NORMAL;";
                await cmd.ExecuteNonQueryAsync();
            }

            // 5. Attacher le satellite (alias 'sat')
            using (var cmd = _connexionFraSession.CreateCommand())
            {
                cmd.CommandText = $"ATTACH DATABASE '{_cheminSat}' AS sat;";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = "PRAGMA sat.journal_mode=TRUNCATE; PRAGMA sat.synchronous=NORMAL;";
                await cmd.ExecuteNonQueryAsync();
            }

            _estInitialiseCarto5 = true;
        }

        /// <summary>
        /// Carto5 — Connexion persistante à la FRA avec satellite attaché.
        /// </summary>
        public Microsoft.Data.Sqlite.SqliteConnection? ConnexionFraSession => _connexionFraSession;

        /// <summary>
        /// Carto5 — Libère la connexion persistante FRA.
        /// </summary>
        public async Task FermerArchitectureDistribueeAsync()
        {
            if (_connexionFraSession != null)
            {
                await _connexionFraSession.CloseAsync();
                await _connexionFraSession.DisposeAsync();
                _connexionFraSession = null;
            }
            _estInitialiseCarto5 = false;
        }

        public async Task SetInstanceAsync(string code)
        {
            if (string.IsNullOrEmpty(code) || code == InstanceCode) return;
            using var db = await _dbFactory.CreateDbContextAsync();
            var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientCode == code);
            if (client != null)
            {
                InstanceCode = client.ClientCode;
                InstanceName = client.Name;
                NotifyStateChanged();
            }
        }

        public void ToggleOfflineMode(bool forceOffline)
        {
            IsOfflineMode = forceOffline;
            NotifyStateChanged();
        }

        public bool IsToolAvailable(string toolId, bool requiresNetwork)
        {
            if (requiresNetwork && IsOfflineMode) return false;
            if (requiresNetwork && !IsNetworkAvailable) return false;
            return true;
        }

        public async Task<List<CartosClient>> GetAvailableSystemsAsync()
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                return await db.Clients.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClientCode)
                    .ToListAsync();
            }
            catch
            {
                return new List<CartosClient>();
            }
        }

        public async Task SaveLastUserAsync(string userCode, string userName)
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var setCode = await db.ClientSettings.FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "SYS_LAST_USER_CODE");
                if (setCode == null) db.ClientSettings.Add(new CartoClientSetting { ClientCode = InstanceCode, SettingKey = "SYS_LAST_USER_CODE", SettingValue = userCode, Scope = "SYSTEM" });
                else setCode.SettingValue = userCode;

                var setName = await db.ClientSettings.FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "SYS_LAST_USER_NAME");
                if (setName == null) db.ClientSettings.Add(new CartoClientSetting { ClientCode = InstanceCode, SettingKey = "SYS_LAST_USER_NAME", SettingValue = userName, Scope = "SYSTEM" });
                else setName.SettingValue = userName;

                await db.SaveChangesAsync();
            }
            catch { }
        }

        public async Task SaveLastModuleAsync(string moduleCode)
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var setting = await db.ClientSettings.FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "ACTIVE_MODULE");

                if (setting == null)
                {
                    db.ClientSettings.Add(new CartoClientSetting { ClientCode = InstanceCode, SettingKey = "ACTIVE_MODULE", SettingValue = moduleCode, Scope = "SYSTEM" });
                }
                else
                {
                    setting.SettingValue = moduleCode;
                }
                await db.SaveChangesAsync();
            }
            catch
            {
                // BOUCLIER DE NAVIGATION : On ne bloque plus jamais l'utilisateur
            }
        }

        public async Task<string> GetLastModuleAsync()
        {
            using var db = await _dbFactory.CreateDbContextAsync();
            var setting = await db.ClientSettings.AsNoTracking().FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "ACTIVE_MODULE");
            return setting?.SettingValue ?? "FRA";
        }

        public async Task<(string Code, string Name)?> GetLastUserAsync()
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var sCode = await db.ClientSettings.AsNoTracking().FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "SYS_LAST_USER_CODE");
                var sName = await db.ClientSettings.AsNoTracking().FirstOrDefaultAsync(s => s.ClientCode == InstanceCode && s.SettingKey == "SYS_LAST_USER_NAME");
                if (sCode != null && sName != null) return (sCode.SettingValue, sName.SettingValue);
            }
            catch { }
            return null;
        }

        private void NotifyStateChanged() => OnContextChanged?.Invoke();

        public async Task<ModuleConfig> GetModuleConfigFromDbAsync(string code)
        {
            using var db = _dbFactory.CreateDbContext();
            var client = await db.Clients.FirstOrDefaultAsync(x => x.ClientCode == code);
            if (client != null)
            {
                return new ModuleConfig { Code = client.ClientCode, DisplayName = client.Name, Description = client.Description, ThemeColor = "#ffc107", IsActive = client.IsActive };
            }
            return new ModuleConfig { Code = code, DisplayName = "Inconnu", Description = "Non trouvé" };
        }
    }
}