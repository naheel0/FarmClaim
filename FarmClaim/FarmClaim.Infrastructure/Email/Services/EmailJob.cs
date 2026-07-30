using FarmClaim.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;
using Polly;

namespace FarmClaim.Infrastructure.Email.Services
{
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public class EmailJob
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailJob> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public EmailJob(
            IEmailService emailService,
            ILogger<EmailJob> logger,
            IAsyncPolicy emailRetryPolicy)
        {
            _emailService = emailService;
            _logger = logger;
            _retryPolicy = emailRetryPolicy;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _emailService.SendEmailAsync(toEmail, subject, htmlBody);
                });

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