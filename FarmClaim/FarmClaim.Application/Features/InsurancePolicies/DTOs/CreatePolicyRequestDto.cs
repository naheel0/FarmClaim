using System;
using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.InsurancePolicies.DTOs
{
    public class CreatePolicyRequestDto
    {
        [Required(ErrorMessage = "Farm ID is required")]
        public Guid FarmId { get; set; }

        [Required(ErrorMessage = "Insurance Plan ID is required")]
        public Guid InsurancePlanId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string? PolicyNumber { get; set; }
    }
}