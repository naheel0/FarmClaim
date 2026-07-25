using System.Security.Claims;
using System.Text.Json;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Data.Audit
{
    public class AuditService : IAuditService
    {
        private readonly IApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task LogActionAsync(
            string action,
            string entityType,
            string? entityId = null,
            string? description = null,
            object? oldValue = null,
            object? newValue = null,
            CancellationToken ct = default)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var (userId, userEmail, userRole) = ExtractUserInfo(httpContext);

                var log = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    UserRole = userRole,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    OldValues = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                    NewValues = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers.UserAgent.ToString() ?? string.Empty,
                    Timestamp = DateTime.UtcNow
                };

                await _context.AuditLogs.AddAsync(log, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
            }
        }

        private static (Guid? userId, string? email, string? role) ExtractUserInfo(HttpContext? httpContext)
        {
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return (null, null, null);

            var user = httpContext.User;
            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            Guid.TryParse(userIdStr, out var userId);
            return (userId, email, role);
        }
    }
}