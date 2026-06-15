// @author: Gilles Lavoie - Architecte principal - 2024-2026
using Microsoft.EntityFrameworkCore;
using GisyWeb.Infrastructure.AdminDb;
using GisyWeb.Infrastructure.AdminDb.Entities;


namespace GisyWeb.Services
{
    public class AdminUserService
    {
        private readonly IDbContextFactory<Carto5AdminContext> _contextFactory;

        public AdminUserService(IDbContextFactory<Carto5AdminContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // =========================================================
        // SECTION 1 : UTILISATEURS (Users)
        // =========================================================

        public async Task<List<CartoUser>> GetAllUsersAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            // CORRECTION: ctx.Users (au lieu de CartoUsers)
            return await ctx.Users.AsNoTracking()
                .OrderBy(u => u.UserCode)
                .ToListAsync();
        }

        public async Task SaveUserAsync(CartoUser user)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var existing = await ctx.Users.FirstOrDefaultAsync(u => u.UserCode == user.UserCode);

            if (existing == null)
            {
                ctx.Users.Add(user);
            }
            else
            {
                existing.FullName = user.FullName;
                existing.Email = user.Email;
                existing.Description = user.Description;
                existing.DtCode = user.DtCode;
                existing.DtName = user.DtName;
                existing.IsActive = user.IsActive;
                existing.SysRoleCode = user.SysRoleCode;
                existing.UpdatedAt = DateTime.Now.ToString("s");
            }

            await ctx.SaveChangesAsync();
        }

        // =========================================================
        // SECTION 2 : PROJETS (Projets)
        // =========================================================

        public async Task<List<CartoProjtPoint>> GetAllProjectsAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();

            try
            {
                // Utilise le nom exact de la table dans votre base
                var sql = "SELECT * FROM CARTO5_PROJT_POINT ORDER BY DAH_CRETN DESC LIMIT 1000";
                return await ctx.Projets.FromSqlRaw(sql).ToListAsync();
            }
            catch
            {
                return new List<CartoProjtPoint>();
            }
        }

        // =========================================================
        // SECTION 3 : DROITS D'ACCÈS PROJETS (CartoAccesProjt)
        // =========================================================

        public async Task<List<CartoAccesProjt>> GetAllAccessAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            // CORRECTION: ctx.CartoAccesProjt (Singulier dans ton context)
            return await ctx.CartoAccesProjt.AsNoTracking()
                .Include(a => a.User)
                .OrderByDescending(a => a.IdeAccesProjt)
                .ToListAsync();
        }

        public async Task SaveAccessAsync(CartoAccesProjt access)
        {
            using var ctx = _contextFactory.CreateDbContext();

            if (access.IdeAccesProjt == 0)
            {
                ctx.CartoAccesProjt.Add(access);
            }
            else
            {
                ctx.CartoAccesProjt.Update(access);
            }

            await ctx.SaveChangesAsync();
        }

        public async Task DeleteAccessAsync(int id)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var item = await ctx.CartoAccesProjt.FindAsync(id);
            if (item != null)
            {
                ctx.CartoAccesProjt.Remove(item);
                await ctx.SaveChangesAsync();
            }
        }

        // =========================================================
        // SECTION 4 : PROFILS TECHNIQUES
        // =========================================================

        public async Task<List<CartoProfil>> GetProfilsAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            // On trie par Label (Nom officiel)
            return await ctx.CartoProfils.AsNoTracking()
                .OrderBy(p => p.Label)
                .ToListAsync();
        }

        public async Task UpsertProfilAsync(CartoProfil profil)
        {
            using var ctx = _contextFactory.CreateDbContext();
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            profil.UpdatedAt = now;

            // On utilise ProfileId
            if (profil.ProfileId == 0)
            {
                profil.CreatedAt = now;
                // Valeurs par défaut
                if (string.IsNullOrEmpty(profil.Code)) profil.Code = Guid.NewGuid().ToString().ToUpper();
                if (string.IsNullOrEmpty(profil.ClientCode)) profil.ClientCode = "SYSTEM";

                ctx.CartoProfils.Add(profil);
            }
            else
            {
                ctx.CartoProfils.Update(profil);
            }

            await ctx.SaveChangesAsync();
        }

        // =========================================================
        // SECTION 5 : CLIENTS (Clients)
        // =========================================================

        public async Task<List<CartosClient>> GetClientsAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            // CORRECTION: ctx.Clients
            return await ctx.Clients.AsNoTracking()
                .OrderBy(c => c.ClientCode)
                .ToListAsync();
        }

        public async Task UpsertClientAsync(CartosClient client)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var existing = await ctx.Clients.FirstOrDefaultAsync(c => c.ClientCode == client.ClientCode);

            if (existing == null)
            {
                ctx.Clients.Add(client);
            }
            else
            {
                existing.Name = client.Name;
                existing.Description = client.Description;
                existing.IsActive = client.IsActive;
                existing.IsDefault = client.IsDefault;
                existing.ConfigFolder = client.ConfigFolder;
                existing.Email = client.Email;
                existing.SysRoleCode = client.SysRoleCode;
                existing.DtCode = client.DtCode;
                existing.DtName = client.DtName;
                existing.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            await ctx.SaveChangesAsync();
        }

        // =========================================================
        // SECTION 6 : ACCÈS SYSTÈMES (UserAccesses)
        // =========================================================

        public async Task<List<CartoUserAccess>> GetUserAccessAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            // CORRECTION: ctx.UserAccesses
            return await ctx.UserAccesses.AsNoTracking()
                .Include(a => a.User)
                .OrderBy(a => a.UserCode)
                .ToListAsync();
        }

        public async Task UpsertUserAccessAsync(CartoUserAccess access)
        {
            using var ctx = _contextFactory.CreateDbContext();

            if (access.AccessId == 0)
            {
                ctx.UserAccesses.Add(access);
            }
            else
            {
                ctx.UserAccesses.Update(access);
            }

            await ctx.SaveChangesAsync();
        }

        // =========================================================
        // SECTION 7 : ACCÈS CLIENTS (UserClientAccesses)
        // =========================================================

        public async Task<List<CartoUserClientAccess>> GetUserClientAccessAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            // CORRECTION: ctx.UserClientAccesses
            return await ctx.UserClientAccesses.AsNoTracking()
                .OrderBy(a => a.UserClientCode)
                .ToListAsync();
        }

        public async Task UpsertUserClientAccessAsync(CartoUserClientAccess access)
        {
            using var ctx = _contextFactory.CreateDbContext();

            if (access.AccessId == 0)
            {
                ctx.UserClientAccesses.Add(access);
            }
            else
            {
                ctx.UserClientAccesses.Update(access);
            }

            await ctx.SaveChangesAsync();
        }

        // =========================================================
        // SECTION 8 : MÉTHODES COMPLEXES (Dashboard V1)
        // =========================================================

        public async Task<List<string>> GetUserAccessCodesAsync(string userCode)
        {
            using var ctx = _contextFactory.CreateDbContext();
            return await ctx.UserAccesses.AsNoTracking()
                .Where(a => a.UserCode == userCode)
                .Select(a => a.SystemCode)
                .ToListAsync();
        }

        public async Task<string?> GetUserDefaultProfilGuidAsync(string userCode)
        {
            using var ctx = _contextFactory.CreateDbContext();
            // CORRECTION: ctx.UserProfils
            var link = await ctx.UserProfils.AsNoTracking()
                .FirstOrDefaultAsync(l => l.UserCode == userCode && l.IsDefault == 1);
            return link?.ProfilGuid;
        }

        public async Task<(bool Success, string Message)> SaveUserCompleteAsync(CartoUser user, List<string> systemCodes, string? defaultProfilGuid)
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var transaction = await ctx.Database.BeginTransactionAsync();

            try
            {
                // A. User (Sauvegarde Standard)
                var existingUser = await ctx.Users.FirstOrDefaultAsync(u => u.UserCode == user.UserCode);
                if (existingUser == null)
                {
                    ctx.Users.Add(user);
                }
                else
                {
                    existingUser.FullName = user.FullName;
                    existingUser.SysRoleCode = user.SysRoleCode;
                    existingUser.IsActive = user.IsActive;
                    existingUser.UpdatedAt = DateTime.Now.ToString("s");
                }
                await ctx.SaveChangesAsync();

                // B. Accès Systèmes (Nettoyage + Insert)
                var oldAccess = await ctx.UserAccesses.Where(a => a.UserCode == user.UserCode).ToListAsync();
                ctx.UserAccesses.RemoveRange(oldAccess);

                foreach (var code in systemCodes)
                {
                    ctx.UserAccesses.Add(new CartoUserAccess
                    {
                        UserCode = user.UserCode,
                        SystemCode = code,
                        GrantedAt = DateTime.Now.ToString("s")
                    });
                }

                // C. Profil Visuel (Table UserProfils - Pour l'UI)
                var oldProfils = await ctx.UserProfils.Where(p => p.UserCode == user.UserCode).ToListAsync();
                ctx.UserProfils.RemoveRange(oldProfils);

                // D. [ARCHITECTE FIX] Configuration Moteur (Table ClientSettings - Pour le Writer)
                // On nettoie les anciennes configs d'écriture pour éviter les conflits
                var oldSettings = await ctx.ClientSettings
                    .Where(s => s.ClientCode == user.UserCode && s.SettingKey == "GUID_SITE_WRITER")
                    .ToListAsync();
                ctx.ClientSettings.RemoveRange(oldSettings);

                if (!string.IsNullOrEmpty(defaultProfilGuid))
                {
                    // 1. Liaison UI (Pour que la liste déroulante se souvienne)
                    ctx.UserProfils.Add(new CartoUserProfil
                    {
                        UserCode = user.UserCode,
                        ProfilGuid = defaultProfilGuid,
                        IsDefault = 1
                    });

                   
                    // 2. Liaison TECHNIQUE (Pour que le moteur sache où écrire le Site)
                    ctx.ClientSettings.Add(new CartoClientSetting
                    {
                        ClientCode = user.UserCode,
                        Scope = "USER_CONFIG",
                        SettingKey = "GUID_SITE_WRITER",
                        SettingValue = defaultProfilGuid,
                        ValueType = "GUID",

                        // CORRECTION : Conversion explicite en String ISO
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                await ctx.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, "Sauvegarde réussie et Profil Écriture configuré.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, "Erreur Critique : " + ex.Message);
            }
        }

        public async Task<List<CartoProjtPoint>> GetProjetsParUtilisateurAsync(string userCode)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var allowedIds = await ctx.CartoAccesProjt
               .Where(a => a.CodUtils == userCode)
               .Select(a => a.IdeProjt)
               .ToListAsync();

            if (!allowedIds.Any()) return new List<CartoProjtPoint>();

            return await ctx.Projets.AsNoTracking()
               .Where(p => p.IdeProjt.HasValue && allowedIds.Contains(p.IdeProjt.Value))
               .ToListAsync();
        }
    }
}