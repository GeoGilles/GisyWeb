using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_PARMT_SYSTM")]
    public class CartoParmtSystm
    {
        // C'est ici que le compilateur bloquait. On met le bon nom.
        [Key]
        [Column("IDE_PARMT_SYSTM")]
        public long IdeParmtSystm { get; set; }

        [Column("COD_REGRP")]
        public string? CodRegrp { get; set; }

        [Column("COD_SOUS_REGRP")]
        public string? CodSousRegrp { get; set; }

        [Column("COD_PARMT")]
        public string? CodParmt { get; set; }

        [Column("DES_PARMT")]
        public string? DesParmt { get; set; }

        [Column("DES_LIBL_COURT")]
        public string? DesLiblCourt { get; set; }

        [Column("DES_LIBL_LONG")]
        public string? DesLiblLong { get; set; }

        [Column("VAL_PARMT")]
        public string? ValParmt { get; set; }

        [Column("COD_FORMT")]
        public string? CodFormt { get; set; }

        [Column("IND_SUPRS_LOGQ")]
        public long IndSuprsLogq { get; set; } = 0;

        // Important : string pour matcher le TEXT de SQLite
        [Column("DAH_CRETN")]
        public string? DahCretn { get; set; }

        [Column("NUM_UTILS_CRETN")]
        public string? NumUtilsCretn { get; set; }

        // Important : string pour matcher le TEXT de SQLite
        [Column("DAH_MODFC")]
        public string? DahModfc { get; set; }

        [Column("NUM_UTILS_MODFC")]
        public string? NumUtilsModfc { get; set; }
    }
}