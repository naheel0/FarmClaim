using System;
using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.InsurancePolicies.DTOs
{
    public class CreatePolicyRequestDto
    {
        [Required(ErrorMessage = "Farm ID is required")]
        public Guid FarmId { get; set; }

        [Required(ErrorMessage = "Policy number is required")]
        [MaxLength(50, ErrorMessage = "Policy number cannot exceed 50 characters")]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Provider is required")]
        [MaxLength(200, ErrorMessage = "Provider name cannot exceed 200 characters")]
        public string Provider { get; set; } = string.Empty;

        [Required(ErrorMessage = "Crop type is required")]
        [MaxLength(100, ErrorMessage = "Crop type cannot exceed 100 characters")]
        public string CropType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Coverage amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage amount must be greater than 0")]
        public decimal CoverageAmount { get; set; }

        [Required(ErrorMessage = "Premium is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Premium must be greater than 0")]
        public decimal Premium { get; set; }

        [Required(ErrorMessage = "Sum insured is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Sum insured must be greater than 0")]
        public decimal SumInsured { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }
    }
}