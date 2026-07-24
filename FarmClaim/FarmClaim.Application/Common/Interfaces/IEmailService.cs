using System.Threading;
using System.Threading.Tasks;

namespace FarmClaim.Application.Common.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends an HTML email to a single recipient.
        /// </summary>
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody,
            CancellationToken ct = default);

        /// <summary>
        /// Sends an HTML email to multiple recipients.
        /// </summary>
        Task SendEmailAsync(
            string[] toEmails,
            string subject,
            string htmlBody,
            CancellationToken ct = default);
    }
}