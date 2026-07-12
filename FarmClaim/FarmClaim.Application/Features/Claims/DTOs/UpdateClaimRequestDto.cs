using System;
using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Claims.DTOs
{
    public class UpdateClaimRequestDto
    {
        [MaxLength(50, ErrorMessage = "Incident type cannot exceed 50 characters")]
        public string? IncidentType { get; set; }

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [MaxLength(2000, ErrorMessage = "Damage description cannot exceed 2000 characters")]
        public string? DamageDescription { get; set; }

        public DateTime? IncidentDate { get; set; }
    }
}