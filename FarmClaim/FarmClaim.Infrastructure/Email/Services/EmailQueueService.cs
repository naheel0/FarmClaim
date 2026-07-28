using Hangfire;
using FarmClaim.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Email.Services
{
    public class EmailQueueService : IEmailQueueService
    {
        private readonly IBackgroundJobClient _jobClient;
        private readonly IEmailTemplateService _templateService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailQueueService> _logger;

        public EmailQueueService(
            IBackgroundJobClient jobClient,
            IEmailTemplateService templateService,
            IEmailService emailService,
            ILogger<EmailQueueService> logger)
        {
            _jobClient = jobClient;
            _templateService = templateService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task EnqueueEmailAsync<T>(string toEmail, string templateName, T model, string? subject = null)
        {
            var (renderedSubject, htmlBody) = await _templateService.RenderAsync(templateName, model);
            if (subject != null) renderedSubject = subject;

            // Enqueue to Hangfire — survives app restarts, retries automatically
            _jobClient.Enqueue<EmailJob>(job => job.SendAsync(toEmail, renderedSubject, htmlBody));

            _logger.LogInformation("Email enqueued: To={To}, Template={Template}", toEmail, templateName);
        }

        public async Task EnqueueEmailAsync(string toEmail, string subject, string htmlBody)
        {
            _jobClient.Enqueue<EmailJob>(job => job.SendAsync(toEmail, subject, htmlBody));
        }

        public async Task SendImmediateAsync(string toEmail, string subject, string htmlBody)
        {
            // Bypass queue — for critical emails that must send now
            await _emailService.SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}