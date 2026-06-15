using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_CLIENT_SETTING")]
    public class CartoClientSetting
    {
        [Key]
        [Column("SETTING_ID")]
        public long SettingId { get; set; }

        [Required]
        [Column("CLIENT_CODE")]
        public string ClientCode { get; set; } = string.Empty;

        [Required]
        [Column("SCOPE")]
        public string Scope { get; set; } = "GLOBAL";

        [Required]
        [Column("SETTING_KEY")]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        [Column("SETTING_VALUE")]
        public string SettingValue { get; set; } = string.Empty;

        [Column("VALUE_TYPE")]
        public string? ValueType { get; set; }

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        [Column("COD_MOD_ORI")]
        public string CodModOri { get; set; } = "GLO";
    }
}