using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FarmClaim.Infrastructure.Email.Services
{
    /// <summary>
    /// Elastic Email API implementation.
    /// Docs: https://elasticemail.com/developers
    /// Free tier: 100 emails/day forever.
    /// </summary>
    public class ElasticEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<ElasticEmailService> _logger;
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://api.elasticemail.com/v2/email/send";

        public ElasticEmailService(
            IOptions<EmailSettings> settings,
            ILogger<ElasticEmailService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("ElasticEmail");
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            await SendEmailAsync(new[] { toEmail }, subject, htmlBody, ct);
        }

        public async Task SendEmailAsync(string[] toEmails, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (toEmails == null || toEmails.Length == 0)
                throw new ArgumentException("At least one recipient is required.", nameof(toEmails));

            // Dummy mode — for local dev
            if (_settings.DummyMode)
            {
                _logger.LogInformation(
                    "📧 [DUMMY] To: {Recipients} | Subject: {Subject}\nBody: {Body}",
                    string.Join(", ", toEmails), subject, htmlBody);
                return;
            }

            try
            {
                // Elastic Email API uses form-urlencoded POST
                var formData = new Dictionary<string, string>
                {
                    ["apikey"] = _settings.SendGridApiKey, // reuse this field for Elastic API key
                    ["from"] = _settings.FromEmail,
                    ["fromName"] = _settings.FromName,
                    ["subject"] = subject,
                    ["bodyHtml"] = htmlBody,
                    ["bodyText"] = StripHtml(htmlBody),
                    ["to"] = string.Join(",", toEmails),
                    ["isTransactional"] = "true"
                };

                var content = new FormUrlEncodedContent(formData);

                var response = await _httpClient.PostAsync(ApiBaseUrl, content, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    // Elastic Email returns "200 OK" with JSON like {"success":true,"data":"..."}
                    var result = JsonDocument.Parse(responseBody);
                    if (result.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                    {
                        _logger.LogInformation("✅ Elastic Email accepted email for {Recipients}",
                            string.Join(", ", toEmails));
                    }
                    else
                    {
                        var error = result.RootElement.TryGetProperty("error", out var errProp)
                            ? errProp.GetString()
                            : "Unknown error";
                        _logger.LogError("❌ Elastic Email API error: {Error}", error);
                        throw new HttpRequestException($"Elastic Email API error: {error}");
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    // 429 — rate limited, throw to trigger Polly retry
                    throw new HttpRequestException("Elastic Email rate limited (429)");
                }
                else
                {
                    _logger.LogError("Elastic Email HTTP {Status}: {Body}", response.StatusCode, responseBody);
                    throw new HttpRequestException($"Elastic Email returned {response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex) when (ex is not HttpRequestException)
            {
                _logger.LogError(ex, "❌ Failed to send email via Elastic Email to {Recipients}",
                    string.Join(", ", toEmails));
                throw;
            }
        }

        private static string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Trim();
        }
    }
}