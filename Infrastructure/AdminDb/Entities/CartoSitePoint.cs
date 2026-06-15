using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_SITE_POINT")]
    public class CartoSitePoint
    {
        [Key]
        [Column("OBJECTID")]
        public int Objectid { get; set; }

        [NotMapped]
        public int Id => Objectid;

        [Column("IDE_PROJT")]
        public int IdeProjt { get; set; }

        [Column("IDE_SITE")]
        public int? IdeSite { get; set; }

        [Column("NOM_SITE")]
        public string? NomSite { get; set; }

        [Column("DES_SITE")]
        public string? DesSite { get; set; }

        [Column("NUM_CARRF")]
        public string? NumCarrf { get; set; }

        [Column("VAL_RAYON_METRE")]
        public double? ValRayonMetre { get; set; } = 50;

        [Column("GEO_COORD_X")]
        public double? GeoCoordX { get; set; }

        [Column("GEO_COORD_Y")]
        public double? GeoCoordY { get; set; }

        [Column("DAH_CRETN")]
        public string DahCretn { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Column("NUM_UTILS_CRETN")]
        public string NumUtilsCretn { get; set; } = "SYSTEM";

        [Column("DAH_MODFC")]
        public string? DahModfc { get; set; }

        [Column("NUM_UTILS_MODFC")]
        public string? NumUtilsModfc { get; set; }

        [Column("IND_SUPRS_LOGQ")]
        public int? IndSuprsLogq { get; set; } = 0;

        [Column("SHAPE")]
      //public Point? Shape { get; set; }

        // --- C'EST ICI QUE ÇA PLANTAIT AVANT ---
        [NotMapped]
        public string? ZoneWkt { get; set; }

        // --- AJOUT POUR LE DASHBOARD (Indicateur visuel) ---
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool EstExtrait { get; set; } = false;
    }
}