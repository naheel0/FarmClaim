using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FarmClaim.API.Services
{
    public class UserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // H16 FIX: Return null (not empty string) when claim is missing.
            // Empty string causes SignalR to bucket anonymous connections under user-"",
            // which could collide with a real user ID if someone passes Guid.Empty.
            return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}