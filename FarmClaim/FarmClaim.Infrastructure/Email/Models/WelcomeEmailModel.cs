namespace FarmClaim.Infrastructure.Email.Models
{
    public class WelcomeEmailModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
    }
}