namespace FarmClaim.Infrastructure.Email.Models
{
    public class UserSuspendedEmailModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime SuspendedAt { get; set; }
    }
}