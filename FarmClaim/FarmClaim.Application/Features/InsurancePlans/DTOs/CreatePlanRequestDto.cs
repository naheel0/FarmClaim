using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.InsurancePlans.DTOs
{
    public class CreatePlanRequestDto
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

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Premium rate must be greater than 0")]
        public decimal PremiumRatePerHectare { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Sum insured per hectare must be greater than 0")]
        public decimal SumInsuredPerHectare { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Coverage percentage must be between 1 and 100")]
        public decimal CoveragePercentage { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? MinAreaInHectares { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? MaxAreaInHectares { get; set; }

        [Required]
        [Range(1, 60)]
        public int PolicyDurationMonths { get; set; } = 12;

        public bool IsActive { get; set; } = true;
    }
}