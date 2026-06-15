using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_USER_PROFIL")]
    public class CartoUserProfil
    {
        [Key]
        [Column("LINK_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // <--- C'EST LA CORRECTION
        public long LinkId { get; set; }

        [Column("USER_CODE")]
        public string UserCode { get; set; } = string.Empty;

        [Column("PROFIL_GUID")]
        public string ProfilGuid { get; set; } = string.Empty;

        [Column("IS_DEFAULT")]
        public long IsDefault { get; set; } = 0;
    }
}