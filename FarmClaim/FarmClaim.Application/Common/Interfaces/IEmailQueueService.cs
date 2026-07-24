namespace FarmClaim.Application.Common.Interfaces
{
    public interface IEmailQueueService
    {
        /// <summary>Enqueue an email to be sent asynchronously via Hangfire.</summary>
        Task EnqueueEmailAsync<T>(string toEmail, string templateName, T model, string? subject = null);

        /// <summary>Enqueue a simple HTML email.</summary>
        Task EnqueueEmailAsync(string toEmail, string subject, string htmlBody);

        /// <summary>Send immediately (bypass queue) — use only for critical emails.</summary>
        Task SendImmediateAsync(string toEmail, string subject, string htmlBody);
    }
}