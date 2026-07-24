namespace FarmClaim.Infrastructure.Configuration
{
    public class EmailSettings
    {
        public string Provider { get; set; } = "SendGrid"; // "SendGrid" or "Smtp"

        // === SendGrid API settings (primary) ===
        public string SendGridApiKey { get; set; } = string.Empty;

        // === SMTP fallback settings ===
        public string SmtpHost { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        // === Common ===
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "FarmClaim";
        public bool EnableSsl { get; set; } = true;
        public bool UseDefaultCredentials { get; set; } = false;

        /// <summary>
        /// When true, emails are written to console/logs instead of being sent.
        /// Useful for local dev without an SMTP server or SendGrid key.
        /// </summary>
        public bool DummyMode { get; set; } = false;
    }
}