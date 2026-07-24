using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("EmailChangeTokens")]
    public class EmailChangeToken : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(256)]
        public string NewEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [MaxLength(45)]
        public string? CreatedByIp { get; set; }
    }
}