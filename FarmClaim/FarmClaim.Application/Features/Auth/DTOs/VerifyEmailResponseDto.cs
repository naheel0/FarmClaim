namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record VerifyEmailResponseDto
    {
        public string Message { get; init; } = string.Empty;
        public string? AccessToken { get; init; }
        public string? RefreshToken { get; init; }
        public int? ExpiresIn { get; init; }
        public UserDto? User { get; init; }
    }
}