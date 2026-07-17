using FarmClaim.API.Hubs;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Notifications.DTOs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace FarmClaim.API.Services
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendClaimUpdateAsync(Guid userId, ClaimNotificationDto notification)
        {
            try
            {
                await _hubContext.Clients.User(userId.ToString())
                    .SendAsync("ClaimUpdated", notification);

                _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to user {UserId}", userId);
            }
        }
    }
}