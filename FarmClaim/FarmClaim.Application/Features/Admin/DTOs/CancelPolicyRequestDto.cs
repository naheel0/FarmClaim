namespace FarmClaim.Application.Features.Admin.DTOs
{
    public record CancelPolicyRequestDto
    {
        public string Reason { get; init; } = string.Empty;
    }
}
