using System.Reflection;
using System.Threading.Tasks;
using RazorLight;
using FarmClaim.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FarmClaim.Infrastructure.Email.Services
{
    public interface IEmailTemplateService
    {
        Task<(string subject, string htmlBody)> RenderAsync<T>(string templateName, T model);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly RazorLightEngine _engine;

        public EmailTemplateService()
        {
            var templatesRoot = Path.Combine(AppContext.BaseDirectory, "Email", "Templates");

            _engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(templatesRoot)
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task<(string subject, string htmlBody)> RenderAsync<T>(string templateName, T model)
        {
            // templateName e.g. "PasswordResetEmail" → renders PasswordResetEmail.cshtml
            var html = await _engine.CompileRenderAsync($"{templateName}.cshtml", model);

            // Extract <title> as subject, fallback to templateName
            var subject = ExtractTitle(html) ?? templateName;

            return (subject, html);
        }

        private static string? ExtractTitle(string html)
        {
            var match = System.Text.RegularExpressions.Regex.Match(html, @"<title>(.*?)</title>",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}