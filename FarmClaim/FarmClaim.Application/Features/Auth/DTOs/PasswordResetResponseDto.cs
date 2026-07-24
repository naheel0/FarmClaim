namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record PasswordResetResponseDto
    {
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Only set on forgot-password response (for testing). 
        /// Always null on reset-password response.
        /// </summary>
        public DateTime? ExpiresAt { get; init; }
    }
}