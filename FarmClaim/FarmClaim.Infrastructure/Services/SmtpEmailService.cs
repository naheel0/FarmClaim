using System.Threading;
using System.Threading.Tasks;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Infrastructure.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FarmClaim.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IOptions<EmailSettings> settings,
            ILogger<SmtpEmailService> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default)
        {
            await SendEmailAsync(new[] { toEmail }, subject, htmlBody, ct);
        }

        public async Task SendEmailAsync(
            string[] toEmails,
            string subject,
            string htmlBody,
            CancellationToken ct = default)
        {
            if (toEmails == null || toEmails.Length == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(toEmails));

            // Dummy mode — for local dev without SMTP
            if (_settings.DummyMode)
            {
                _logger.LogInformation(
                    "📧 [DUMMY EMAIL] To: {Recipients} | Subject: {Subject}\nBody: {Body}",
                    string.Join(", ", toEmails), subject, htmlBody);
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

                foreach (var email in toEmails)
                    message.To.Add(MailboxAddress.Parse(email));

                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();

                // Port 587 → StartTls; Port 465 → SslOnConnect
                var sslOption = _settings.Port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

                await client.ConnectAsync(_settings.SmtpHost, _settings.Port, sslOption, ct);

                if (!_settings.UseDefaultCredentials)
                    await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                _logger.LogInformation("📧 Email sent to {Recipients}. Subject: {Subject}",
                    string.Join(", ", toEmails), subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Failed to send email to {Recipients}. Subject: {Subject}",
                    string.Join(", ", toEmails), subject);
                throw;
            }
        }
    }
}