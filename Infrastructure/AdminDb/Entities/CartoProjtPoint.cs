using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    // ✅ CORRECTION 1 : On pointe vers le vrai nom technique du GeoPackage
    [Table("Carto_G_PROJT_POINT")]
    public class CartoProjtPoint
    {
        [Key]
        [Column("OBJECTID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Objectid { get; set; }

        [Column("IDE_PROJT")]
        public int? IdeProjt { get; set; }

        [Column("NOM_PROJT")]
        public string? NomProjt { get; set; }

        [Column("DES_PROJT")]
        public string? DesProjt { get; set; }

        [Column("IDE_UNITE_ADMNS_PROPR")]
        public int? IdeUniteAdmnsPropr { get; set; }

        [Column("NOM_UNITE_ADMNS_PROPR")]
        public string? NomUniteAdmnsPropr { get; set; }

        [Column("IND_SUPRS_LOGQ")]
        public int? IndSuprsLogq { get; set; }

        [Column("GEO_COORD_X")]
        public double? GeoCoordX { get; set; }

        [Column("GEO_COORD_Y")]
        public double? GeoCoordY { get; set; }

       // [Column("SHAPE")]
       // public Point? Shape { get; set; }

        [Column("DAH_CRETN")]
        public string? DahCretn { get; set; }

        [Column("NUM_UTILS_CRETN")]
        public string? NumUtilsCretn { get; set; }

        [Column("DAH_MODFC")]
        public string? DahModfc { get; set; }

        [Column("NUM_UTILS_MODFC")]
        public string? NumUtilsModfc { get; set; }

        // ✅ CORRECTION 2 : Ajout du GlobalID obligatoire (NOT NULL dans le DDL)
        // On l'initialise avec un GUID pour éviter le crash à l'insertion
        [Column("GlobalID")]
        public string GlobalID { get; set; } = Guid.NewGuid().ToString();

        // Champs optionnels vus dans le DDL (Pour être complet)
        [Column("SELCT_GEOMT")]
        public int? SelctGeomt { get; set; }
    }
}