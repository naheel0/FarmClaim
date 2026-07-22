using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("InsurancePlans")]
    public class InsurancePlan : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string CropType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Premium cost per hectare (e.g., 1500.00 ₹/ha).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PremiumRatePerHectare { get; set; }

        /// <summary>
        /// Maximum sum insured per hectare (e.g., 50000.00 ₹/ha).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SumInsuredPerHectare { get; set; }

        /// <summary>
        /// Percentage of sum insured that is covered (e.g., 80.00 = 80%).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal CoveragePercentage { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MinAreaInHectares { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? MaxAreaInHectares { get; set; }

        [Required]
        public int PolicyDurationMonths { get; set; } = 12;

        [Required]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<InsurancePolicy> Policies { get; set; } = new List<InsurancePolicy>();
    }
}