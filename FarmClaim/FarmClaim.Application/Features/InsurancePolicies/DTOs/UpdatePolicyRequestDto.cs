using System;
using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.InsurancePolicies.DTOs
{
    public class UpdatePolicyRequestDto
    {
        [MaxLength(50, ErrorMessage = "Policy number cannot exceed 50 characters")]
        public string? PolicyNumber { get; set; }

        [MaxLength(200, ErrorMessage = "Provider name cannot exceed 200 characters")]
        public string? Provider { get; set; }

        [MaxLength(100, ErrorMessage = "Crop type cannot exceed 100 characters")]
        public string? CropType { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage amount must be greater than 0")]
        public decimal? CoverageAmount { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Premium must be greater than 0")]
        public decimal? Premium { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Sum insured must be greater than 0")]
        public decimal? SumInsured { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool? IsActive { get; set; }
    }
}