namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record AuthResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public string RefreshToken { get; init; } = string.Empty;

        public int ExpiresIn { get; init; }

        public UserDto User { get; init; } = null!;
    }
}