using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_PROFIL")]
    public class CartoProfil
    {
        // =========================================================
        // 1. LES VRAIES COLONNES (MAPPING SQL)
        // =========================================================

        [Key]
        [Column("PROFILE_ID")]
        public long ProfileId { get; set; }

        [Column("LABEL")]
        public string Label { get; set; } = string.Empty;

        [Column("CODE")]
        public string Code { get; set; } = string.Empty;

        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        [Column("CLIENT_CODE")]
        public string ClientCode { get; set; } = "SYSTEM";

        [Column("ENGINE_KIND")]
        public string EngineKind { get; set; } = "Sqlite";

        [Column("GEOMETRY_KIND")]
        public string GeometryKind { get; set; } = "Point";

        [Column("IS_ACTIVE")]
        public long IsActive { get; set; } = 1;

        [Column("IS_PUBLISHED")]
        public long IsPublished { get; set; }

        [Column("SQLITE_DATA_PATH")]
        public string? SqliteDataPath { get; set; }

        [Column("GLOBAL_ID")]
        public string? GlobalId { get; set; }

        [Column("DIR_PATH")]
        public string? DirPath { get; set; }
        
        [Column("TILE_CACHE_PATH")]
        public string? TileCachePath { get; set; }

        [Column("DB_NAME")]
        public string? DbName { get; set; }

        [Column("JSON_CONFIG")]
        public string? JsonConfig { get; set; }

        [Column("WRITER_KIND")]
        public string? WriterKind { get; set; }

        [Column("ESRI_MOBILE_PATH")]
        public string? EsriMobilePath { get; set; }

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = string.Empty;

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = string.Empty;
        
        [System.ComponentModel.DataAnnotations.Schema.Column("COD_MOD_ORI")]
        public string CodModOri { get; set; } = "GLO";


        // =========================================================
        // 2. LES ALIAS MAGIQUES (POUR CALMER LES ERREURS)
        // =========================================================

        // Pour le vieux code qui veut "IdeProfil"
        [NotMapped]
        public int IdeProfil
        {
            get => (int)ProfileId;
            set => ProfileId = value;
        }

        // Pour le vieux code qui veut "Nom"
        [NotMapped]
        public string Nom
        {
            get => Label;
            set => Label = value;
        }
    }
}