using FarmClaim.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("Claims")]
    public class Claim : BaseEntity
    {
        [Required]
        public Guid PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public virtual InsurancePolicy Policy { get; set; } = null!;

        [Required]
        public Guid FarmId { get; set; }

        [ForeignKey(nameof(FarmId))]
        public virtual Farm Farm { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        public DateTime IncidentDate { get; set; }

        [Required]
        [MaxLength(50)]
        public IncidentType IncidentType { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(2000)]
        public string? DamageDescription { get; set; }

        [Required]
        [MaxLength(30)]
        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public decimal? ApprovedAmount { get; set; }

        // Review tracking
        // OLD: public string? ReviewedBy { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        [ForeignKey(nameof(ReviewedByUserId))]
        public virtual User? ReviewedByUser { get; set; }

        [MaxLength(255)]
        public string? ReviewedBy { get; set; }  // kept for backward compat / display

        public DateTime? ReviewedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Payment tracking
        public DateTime? PaidAt { get; set; }
        [MaxLength(100)]
        public string? PaymentReference { get; set; }

        // AI & Weather (your existing fields — unchanged)
        [Column(TypeName = "nvarchar(max)")]
        public string? WeatherSnapshot { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? AIAnalysisResult { get; set; }

        public virtual ICollection<ClaimImage> Images { get; set; } = new List<ClaimImage>();
    }
}