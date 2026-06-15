using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_USER")]
    public class CartoUser
    {
        // --- BLOC D'IDENTITÉ ---
        [Key]
        [Column("USER_CODE")]
        public string UserCode { get; set; } = string.Empty; // ex: GLAVOIE

        [Column("USER_GUID")]
        public string UserGuid { get; set; } = string.Empty;

        [Column("FULL_NAME")]
        public string FullName { get; set; } = string.Empty;

        [Column("EMAIL")]
        public string? Email { get; set; }

        // --- BLOC NOUVEAU (Gestion des Rôles) ---
        [Column("SYS_ROLE_CODE")]
        public string SysRoleCode { get; set; } = "USER"; // Admin, SUPER, USER

        // --- BLOC LEGACY (On garde tout !) ---
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        [Column("DT_CODE")]
        public string? DtCode { get; set; }

        [Column("DT_NAME")]
        public string? DtName { get; set; }

        // --- BLOC TECHNIQUE ---
        [Column("IS_ACTIVE")]
        public long IsActive { get; set; } = 1;

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("s");

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("s");

        // --- BLOC UI (Non mappé en base, mais vital pour ton interface Blazor) ---
        // Sert à stocker temporairement les cases à cocher "Accès Système"
        [NotMapped]
        public List<string> AccessCodes { get; set; } = new();

        // Dans Carto5User.cs
        [System.ComponentModel.DataAnnotations.Schema.Column("COD_MOD_ORI")]
        public string CodModOri { get; set; } = "GLO";
    }
}