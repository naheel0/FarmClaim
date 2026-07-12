namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record RefreshTokenDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public string RefreshToken { get; init; } = string.Empty;

        public int ExpiresIn { get; init; }
    }
}