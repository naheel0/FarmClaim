using FarmClaim.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Email.Services
{
    // M4: Polly retry was redundant with Hangfire's [AutomaticRetry] retrier.
    // Hangfire already retries 3 times (60s/300s/900s), so removing inner Polly
    // avoids up to 9 total attempts (3 Polly x 3 Hangfire).
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public class EmailJob
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailJob> _logger;

        public EmailJob(
            IEmailService emailService,
            ILogger<EmailJob> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                await _emailService.SendEmailAsync(toEmail, subject, htmlBody);
                _logger.LogInformation("Email sent: To={To}, Subject={Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email permanently failed after retries: To={To}", toEmail);
                throw;
            }
        }
    }
}
