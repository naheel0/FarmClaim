using FarmClaim.Application.Features.Notifications.DTOs;

namespace FarmClaim.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task SendClaimUpdateAsync(Guid userId, ClaimNotificationDto notification);
    }
}