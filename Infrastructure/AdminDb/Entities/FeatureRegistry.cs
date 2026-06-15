using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_FEATURE_REGISTRY")]
    public class FeatureRegistry
    {
        [Key]
        [Column("FEATURE_CODE")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column("LABEL")]
        public string Label { get; set; } = string.Empty;

        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        [Column("DEFAULT_OPTIONS")]
        public string? DefaultOptions { get; set; }

        [Column("COD_MOD_ORI")]
        public string CodModOri { get; set; } = "GLO";

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}