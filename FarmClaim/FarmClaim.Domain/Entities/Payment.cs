using FarmClaim.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("Payments")]
    public class Payment : BaseEntity
    {
        [Required]
        public Guid PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public virtual InsurancePolicy Policy { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string OrderId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? PaymentId { get; set; }

        [Required]
        public long AmountInPaise { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AmountInRupees { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "INR";

        [Required]
        [MaxLength(20)]
        public PaymentStatus Status { get; set; } = PaymentStatus.Created;

        [MaxLength(500)]
        public string? Signature { get; set; }

        [MaxLength(50)]
        public string? Method { get; set; }

        [MaxLength(200)]
        public string? MethodDescription { get; set; }

        [MaxLength(100)]
        public string? BankReference { get; set; }

        [MaxLength(1000)]
        public string? FailureReason { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Fee { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Tax { get; set; }

        public DateTime? CapturedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? RefundedAt { get; set; }

        [MaxLength(45)]
        public string? ClientIp { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(50)]
        public string? ReceiptNumber { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}