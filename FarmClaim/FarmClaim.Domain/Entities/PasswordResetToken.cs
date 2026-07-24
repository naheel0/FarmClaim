using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("PasswordResetTokens")]
    public class PasswordResetToken : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// SHA256 hash of the raw token. Raw token is NEVER stored.
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public bool IsUsed => UsedAt.HasValue;

        /// <summary>
        /// IP address of the client that requested the reset (for audit).
        /// </summary>
        [MaxLength(45)]
        public string? CreatedByIp { get; set; }
    }
}