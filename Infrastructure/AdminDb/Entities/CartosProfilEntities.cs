using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    // TABLE MAÎTRE : CARTO5_PROFIL
    [Table("CARTO5_PROFIL")]
    public class CartosProfil
    {
        [Key]
        [Column("PROFILE_ID")]
        public long ProfileId { get; set; }

        [Column("CODE")]
        public string Code { get; set; } = string.Empty;

        [Column("LABEL")]
        public string Label { get; set; } = string.Empty;

        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        [Column("CLIENT_CODE")]
        public string ClientCode { get; set; } = "SYSTEM"; // FK vers CARTO5_CLIENT

        // Valeurs possibles : Sqlite, SpatiaLite, Esri, Carto5
        [Column("ENGINE_KIND")]
        public string EngineKind { get; set; } = "Sqlite";

        // Valeurs possibles : Point, Polyline, Polygon
        [Column("GEOMETRY_KIND")]
        public string GeometryKind { get; set; } = "Point";

        [Column("DB_NAME")]
        public string? DbName { get; set; }

        [Column("DIR_PATH")]
        public string? DirPath { get; set; }

        [Column("IS_ACTIVE")]
        public bool IsActive { get; set; }

        [Column("WRITER_KIND")]
        public string? WriterKind { get; set; } = "Sqlite";

        [Column("JSON_CONFIG")]
        public string? JsonConfig { get; set; }

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // DÉTAIL 1 : DÉFINITION (Mapping Panel / Code)
    [Table("CARTO5_PROFIL_DEF")]
    public class CartosProfilDef
    {
        [Key]
        [Column("PANEL_CODE")]
        public string PanelCode { get; set; } = string.Empty;

        // Attention : Lien par CODE et non par ID selon le DDL
        [Column("PROFILE_CODE")]
        public string ProfileCode { get; set; } = string.Empty;

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // DÉTAIL 2 : HISTORIQUE
    [Table("CARTO5_PROFIL_HISTORY")]
    public class CartosProfilHistory
    {
        [Key]
        [Column("HISTORY_ID")]
        public long HistoryId { get; set; }

        [Column("PROFILE_ID")]
        public long ProfileId { get; set; }

        [Column("TIMESTAMP")]
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("WRITER_KIND")]
        public string WriterKind { get; set; } = string.Empty;

        [Column("GEOMETRY_KIND")]
        public string GeometryKind { get; set; } = string.Empty;

        [Column("DB_NAME")]
        public string? DbName { get; set; }

        [Column("DIR_PATH")]
        public string? DirPath { get; set; }
    }

    // DÉTAIL 3 : PROJETS ASSOCIÉS
    [Table("CARTO5_PROFIL_PROJT")]
    public class CartosProfilProjt
    {
        // Clé composite à gérer via Fluent API ou HasKey dans le contexte, 
        // mais pour Blazor simple, on va utiliser l'ordre des colonnes.

        [Column("PROFILE_ID")]
        public long ProfileId { get; set; }

        [Column("IDE_PROJT")]
        public long IdeProjt { get; set; }

        [Column("IS_ACTIVE")]
        public bool IsActive { get; set; }

        [Column("CREATED_AT")]
        public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("UPDATED_AT")]
        public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}