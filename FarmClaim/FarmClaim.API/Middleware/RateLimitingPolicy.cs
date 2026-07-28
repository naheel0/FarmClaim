using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.Threading.RateLimiting;

namespace FarmClaim.API.Middleware
{
    /// <summary>
    /// Custom rate limiting policy that partitions by:
    /// - IP address (anonymous endpoints like /login)
    /// - User ID (authenticated endpoints)
    /// - Endpoint path (different limits per route)
    /// </summary>
    public class RateLimitingPolicy : IRateLimiterPolicy<string>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RateLimitingPolicy> _logger;

        public RateLimitingPolicy(
            IHttpContextAccessor httpContextAccessor,
            ILogger<RateLimitingPolicy> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ✅ Correct .NET 8 signature: Func<OnRejectedContext, CancellationToken, ValueTask>
        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
            async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var clientIp = GetClientIp(httpContext);

                _logger.LogWarning(
                    "🚫 Rate limit exceeded: IP={Ip}, Path={Path}",
                    clientIp, httpContext.Request.Path);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/json";

                // Try to get RetryAfter from metadata
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? (int)retry.TotalSeconds
                    : 60;

                var response = new
                {
                    statusCode = 429,
                    error = "Too Many Requests",
                    message = "You're doing that too often. Please slow down.",
                    retryAfterSeconds = retryAfter,
                    traceId = httpContext.TraceIdentifier,
                    path = httpContext.Request.Path.ToString()
                };

                await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };

        public RateLimitPartition<string> GetPartition(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value?.ToLower() ?? "";
            var clientIp = GetClientIp(httpContext);

            // Try to get authenticated user ID
            var userIdClaim = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userKey = !string.IsNullOrEmpty(userIdClaim) ? $"user:{userIdClaim}" : $"ip:{clientIp}";

            // === Per-endpoint rate limits ===
            var (permitLimit, window) = path switch
            {
                // Strict auth endpoints (prevent abuse)
                "/api/v1/auth/login" => (5, TimeSpan.FromMinutes(1)),
                "/api/v1/auth/register" => (5, TimeSpan.FromMinutes(1)),
                "/api/v1/auth/forgot-password" => (3, TimeSpan.FromMinutes(1)),
                "/api/v1/auth/resend-otp" => (3, TimeSpan.FromMinutes(1)),
                "/api/v1/auth/reset-password" => (5, TimeSpan.FromMinutes(1)),

                // Moderate limits
                "/api/v1/auth/verify-email" => (10, TimeSpan.FromMinutes(1)),
                "/api/v1/auth/confirm-email-change" => (5, TimeSpan.FromMinutes(1)),
                "/api/v1/auth/change-email" => (5, TimeSpan.FromMinutes(1)),

                // Hangfire dashboard — high limit, mostly internal
                "/hangfire" => (200, TimeSpan.FromMinutes(1)),

                // Default for all other endpoints
                _ => string.IsNullOrEmpty(userIdClaim)
                    ? (100, TimeSpan.FromMinutes(1))  // Anonymous: 100/min
                    : (200, TimeSpan.FromMinutes(1))  // Authenticated: 200/min
            };

            // Partition key = endpoint + user/ip
            var partitionKey = $"{path}:{userKey}";

            // ✅ Use positional args (not named) to avoid version-specific param name issues
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
        }

        private static string GetClientIp(HttpContext context)
        {
            // Only trust proxy headers if the direct connection comes from a known proxy
            var directIp = context.Connection.RemoteIpAddress?.ToString();

            // If no X-Forwarded-For header, always use direct IP
            if (!context.Request.Headers.ContainsKey("X-Forwarded-For"))
                return directIp ?? "unknown";

            // Only trust X-Forwarded-For when the direct connection is from localhost
            // (i.e., a reverse proxy running on the same machine or in the same container network)
            var trustedProxies = new[] { "127.0.0.1", "::1", "localhost" };
            if (directIp != null && trustedProxies.Contains(directIp, StringComparer.OrdinalIgnoreCase))
            {
                var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwarded))
                    return forwarded.Split(',')[0].Trim();
            }

            // Not behind a trusted proxy — use direct IP
            return directIp ?? "unknown";
        }
    }
}