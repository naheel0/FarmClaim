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

        // Financial fields (CoverageAmount, Premium, SumInsured) are intentionally excluded.
        // They are set by the admin and must not be user-editable.

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}