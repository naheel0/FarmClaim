using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record PayClaimRequestDto
    {
        [MaxLength(100)]
        public string? PaymentReference { get; init; }
    }
}