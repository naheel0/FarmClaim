using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record RejectPolicyRequestDto
    {
        [Required(ErrorMessage = "Rejection reason is required")]
        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string Reason { get; init; } = string.Empty;
    }
}