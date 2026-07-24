namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record RegisterResponseDto
    {
        public string Message { get; init; } = string.Empty;
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public bool RequiresEmailVerification { get; init; }
        public DateTime? OtpExpiresAt { get; init; }
    }
}