using GisyWeb.Infrastructure.AdminDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace GisyWeb.Infrastructure.AdminDb
{
    public class Carto5AdminContext : DbContext
    {
        public Carto5AdminContext(DbContextOptions<Carto5AdminContext> options) : base(options) { }

        public DbSet<CartosClient> Clients => Set<CartosClient>();
        public DbSet<CartoClientSetting> ClientSettings => Set<CartoClientSetting>();
        public DbSet<CartoProfil> CartoProfils => Set<CartoProfil>();
        public DbSet<CartoAccesProjt> CartoAccesProjt => Set<CartoAccesProjt>();
        public DbSet<CartoProjtPoint> Projets => Set<CartoProjtPoint>();
        public DbSet<CartoSitePoint> Sites => Set<CartoSitePoint>();
        public DbSet<CartoUser> Users => Set<CartoUser>();
        public DbSet<CartoUserAccess> UserAccesses => Set<CartoUserAccess>();
        public DbSet<CartoUserProfil> UserProfils => Set<CartoUserProfil>();
        public DbSet<CartoUserClientAccess> UserClientAccesses => Set<CartoUserClientAccess>();
        public DbSet<CartoLegendState> LegendStates => Set<CartoLegendState>();
        public DbSet<FeatureRegistry> FeatureRegistries => Set<FeatureRegistry>();
        public DbSet<CartosClientApp> ClientApps => Set<CartosClientApp>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CartosClient>().ToTable("CARTO5_CLIENT");
            modelBuilder.Entity<CartoClientSetting>().ToTable("CARTO5_CLIENT_SETTING");
            modelBuilder.Entity<CartoProfil>().ToTable("CARTO5_PROFIL");
            modelBuilder.Entity<CartoAccesProjt>().ToTable("CARTO5_ACCES_PROJT");
            modelBuilder.Entity<CartoProjtPoint>().ToTable("Carto_G_PROJT_POINT");
            modelBuilder.Entity<CartoSitePoint>().ToTable("CARTO5_SITE_POINT");
            modelBuilder.Entity<CartoUser>().ToTable("CARTO5_USER");
            modelBuilder.Entity<CartoUserAccess>().ToTable("CARTO5_USER_ACCESS");
            modelBuilder.Entity<CartoUserProfil>().ToTable("CARTO5_USER_PROFIL");
            modelBuilder.Entity<CartoUserClientAccess>().ToTable("CARTO5_USER_CLIENT_ACCESS");
            modelBuilder.Entity<CartoLegendState>().ToTable("CARTO5_LEGEND_STATE");
            modelBuilder.Entity<FeatureRegistry>().ToTable("CARTO5_FEATURE_REGISTRY");

            modelBuilder.Entity<CartoUserAccess>(entity => {
                entity.HasKey(e => e.AccessId);
                entity.HasIndex(e => new { e.UserCode, e.SystemCode }).IsUnique();
            });

            modelBuilder.Entity<CartoUserProfil>(entity => {
                entity.HasKey(e => e.LinkId);
                entity.HasIndex(e => new { e.UserCode, e.ProfilGuid }).IsUnique();
            });
        }
    }
}