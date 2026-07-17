using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record ApproveClaimRequestDto
    {
        [Required(ErrorMessage = "Approved amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal ApprovedAmount { get; init; }

        [MaxLength(500)]
        public string? AdminNotes { get; init; }
    }

    public record RejectClaimRequestDto
    {
        [Required(ErrorMessage = "Rejection reason is required")]
        [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string RejectionReason { get; init; } = string.Empty;
    }
}