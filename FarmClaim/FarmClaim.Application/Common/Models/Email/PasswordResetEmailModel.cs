namespace FarmClaim.Application.Common.Models.Email
{
    public class PasswordResetEmailModel
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string FrontendBaseUrl { get; set; } = string.Empty;
    }
}
