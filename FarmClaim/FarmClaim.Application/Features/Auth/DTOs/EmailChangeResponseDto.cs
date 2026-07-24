namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record EmailChangeResponseDto
    {
        public string Message { get; init; } = string.Empty;
        public DateTime? ExpiresAt { get; init; }
    }
}