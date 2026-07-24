namespace FarmClaim.Application.Features.Auth.Commands.ChangeEmail
{
    public class ChangeEmailTemplateModel
    {
        public string UserName { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string FrontendBaseUrl { get; set; } = string.Empty;
    }
}