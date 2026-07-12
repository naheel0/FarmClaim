using System.Collections.Generic;
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

        [Required]
        [MaxLength(200)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required]
        public decimal CoverageAmount { get; set; }

        [Required]
        public decimal Premium { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string CropType { get; set; } = string.Empty;

        [Required]
        public decimal SumInsured { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}