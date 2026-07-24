using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("EmailVerificationCodes")]
    public class EmailVerificationCode : BaseEntity
    {
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// SHA256 hash of the 6-digit OTP. Raw code is NEVER stored.
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string CodeHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Number of failed verification attempts. Max 3, then code is invalidated.
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        [MaxLength(45)]
        public string? CreatedByIp { get; set; }

        public bool IsUsed => UsedAt.HasValue;
        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
        public bool IsLocked => AttemptCount >= 3;
    }
}