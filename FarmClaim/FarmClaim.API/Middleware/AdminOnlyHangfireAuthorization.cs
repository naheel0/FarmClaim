using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace FarmClaim.API.Middleware
{
    public class AdminOnlyHangfireAuthorization : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow access only if user is authenticated AND is Admin
            return httpContext.User?.Identity?.IsAuthenticated == true
                   && httpContext.User.IsInRole("Admin");
        }
    }
}
