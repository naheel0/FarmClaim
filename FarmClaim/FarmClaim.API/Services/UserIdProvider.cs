using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FarmClaim.API.Services
{
    public class UserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}