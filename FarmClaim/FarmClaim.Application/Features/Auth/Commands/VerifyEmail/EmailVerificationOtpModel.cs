namespace FarmClaim.Application.Features.Auth.Commands.VerifyEmail
{
    public class EmailVerificationOtpModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; }
    }
}