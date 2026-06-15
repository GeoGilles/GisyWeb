using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_ACCES_PROJT")]
    public class CartoAccesProjt
    {
        [Key]
        [Column("IDE_ACCES_PROJT")]
        public int IdeAccesProjt { get; set; }

        [Column("IDE_PROJT")]
        public int IdeProjt { get; set; }

        [Column("COD_UTILS")]
        public string CodUtils { get; set; } = "";

        [Column("ROLE_CODE")]
        public string RoleCode { get; set; } = "VIEWER";

        [Column("IND_ECRIT")]
        public int IndEcrit { get; set; }

        [Column("IND_PROPR")]
        public int IndPropr { get; set; }

        [Column("DAT_DEBUT_APPLQ")]
        public string? DatDebutApplq { get; set; }

        [Column("DAT_FIN_APPLQ")]
        public string? DatFinApplq { get; set; }

        [ForeignKey(nameof(CodUtils))]
        public virtual CartoUser? User { get; set; }
    }

   
    
}