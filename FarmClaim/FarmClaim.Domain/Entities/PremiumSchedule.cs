using FarmClaim.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("PremiumSchedules")]
    public class PremiumSchedule : BaseEntity
    {
        [Required]
        public Guid PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public virtual InsurancePolicy Policy { get; set; } = null!;

        [Required]
        public int InstallmentNumber { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AmountDue { get; set; }

        public Guid? PaymentId { get; set; }

        [ForeignKey(nameof(PaymentId))]
        public virtual Payment? Payment { get; set; }

        [Required]
        [MaxLength(20)]
        public PremiumScheduleStatus Status { get; set; } = PremiumScheduleStatus.Pending;

        public DateTime? PaidAt { get; set; }
    }
}
