using FarmClaim.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("InsurancePolicies")]
    public class InsurancePolicy : BaseEntity
    {
        [Required]
        public Guid FarmId { get; set; }

        [ForeignKey(nameof(FarmId))]
        public virtual Farm Farm { get; set; } = null!;

        // === InsurancePlan link (NULLABLE for backward compat with pre-existing policies) ===
        public Guid? InsurancePlanId { get; set; }

        [ForeignKey(nameof(InsurancePlanId))]
        public virtual InsurancePlan? InsurancePlan { get; set; }

        [Required]
        [MaxLength(200)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal CoverageAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Premium { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string CropType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SumInsured { get; set; }

        [Required]
        [MaxLength(20)]
        public PolicyStatus Status { get; set; } = PolicyStatus.Pending;

        // Approval tracking
        public DateTime? ApprovedAt { get; set; }
        public Guid? ApprovedByUserId { get; set; }

        [ForeignKey(nameof(ApprovedByUserId))]
        public virtual User? ApprovedByUser { get; set; }

        // Rejection tracking
        public DateTime? RejectedAt { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        // Cancellation tracking
        public DateTime? CancelledAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}