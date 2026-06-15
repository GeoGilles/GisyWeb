using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_USER_CLIENT_ACCESS")]
    public class CartoUserClientAccess
    {
        [Key]
        [Column("ACCESS_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AccessId { get; set; }

        [Column("USER_CLIENT_CODE")]
        public string UserClientCode { get; set; } = string.Empty;

        [Column("APP_CLIENT_CODE")]
        public string AppClientCode { get; set; } = string.Empty;
    }
}