using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GisyWeb.Infrastructure.AdminDb.Entities
{
    [Table("CARTO5_USER_ACCESS")]
    public class CartoUserAccess
    {
        [Key]
        [Column("ACCESS_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // ✅ TON FIX CRITIQUE
        public long AccessId { get; set; }

        [Column("USER_CODE")]
        public string UserCode { get; set; } = string.Empty;

        [Column("SYSTEM_CODE")]
        public string SystemCode { get; set; } = string.Empty;

        [Column("ROLE_CODE")]
        public string RoleCode { get; set; } = "USER";

        [Column("GRANTED_AT")]
        public string GrantedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // --- NAVIGATION (Indispensable pour voir le Nom dans la grille) ---
        [ForeignKey(nameof(UserCode))]
        public virtual CartoUser? User { get; set; }
    }
}