using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_CLIENT")]
    public class CartosClient
    {
        [Key]
        [Column("CLIENT_CODE")]
        public string ClientCode { get; set; } = "";

        [Column("NAME")]
        public string Name { get; set; } = "";

        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        // --- AJOUT MANQUANT (GUID) ---
        [Column("GLOBAL_ID")]
        public string? GlobalId { get; set; }
        // -----------------------------
        [Column("EMAIL")]
        public string? Email { get; set; }

        [Column("SYS_ROLE_CODE")]
        public string SysRoleCode { get; set; } = "USER";

        [Column("DT_CODE")]
        public string? DtCode { get; set; }

        [Column("DT_NAME")]
        public string? DtName { get; set; }

        [Column("IS_DEFAULT")]
        public int IsDefault { get; set; }

        [Column("IS_ACTIVE")]
        public bool IsActive { get; set; } = true;

        [Column("CONFIG_FOLDER")]
        public string? ConfigFolder { get; set; }

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = "";

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = "";

        // Petit helper d'affichage (optionnel mais pratique)
        [NotMapped]
        public string DisplayName => $"{Name} ({ClientCode})";

        [System.ComponentModel.DataAnnotations.Schema.Column("COD_MOD_ORI")]
        public string CodModOri { get; set; } = "GLO";
    }

    // TABLE DÉTAIL 1 : CARTO5_CLIENT_APP
    [Table("CARTO5_CLIENT_APP")]
    public class CartosClientApp
    {
        [Key]
        [Column("CLIENT_APP_ID")]
        public long ClientAppId { get; set; }

        [Column("CLIENT_CODE")]
        public string ClientCode { get; set; } = string.Empty;

        [Column("APP_CODE")]
        public string AppCode { get; set; } = string.Empty;

        [Column("APP_LABEL")]
        public string AppLabel { get; set; } = string.Empty;

        [Column("APP_KIND")]
        public string AppKind { get; set; } = "WEB";

        [Column("BASE_URL")]
        public string? BaseUrl { get; set; }

        [Column("API_BASE_URL")]
        public string? ApiBaseUrl { get; set; }

        [Column("NOTES")]
        public string? Notes { get; set; }

        [Column("IS_DEFAULT")]
        public bool IsDefault { get; set; }

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // TABLE DÉTAIL 2 : CARTO5_CLIENT_SETTING
    [Table("CARTO5_CLIENT_SETTING")]
    public class CartosClientSetting
    {
        [Key]
        [Column("SETTING_ID")]
        public long SettingId { get; set; }

        [Column("CLIENT_CODE")]
        public string ClientCode { get; set; } = string.Empty;

        [Column("SCOPE")]
        public string Scope { get; set; } = "GLOBAL";

        [Column("SETTING_KEY")]
        public string SettingKey { get; set; } = string.Empty;

        [Column("SETTING_VALUE")]
        public string SettingValue { get; set; } = string.Empty;

        [Column("VALUE_TYPE")]
        public string? ValueType { get; set; } = "STRING";

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [System.ComponentModel.DataAnnotations.Schema.Column("COD_MOD_ORI")]
        public string CodModOri { get; set; } = "GLO";
    }
}