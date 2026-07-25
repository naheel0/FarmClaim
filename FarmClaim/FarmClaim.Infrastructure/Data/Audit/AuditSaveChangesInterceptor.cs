using System.Text.Json;
using FarmClaim.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FarmClaim.Infrastructure.Data.Audit
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var dbContext = eventData.Context;
            if (dbContext == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

            var (userId, userEmail, userRole) = ExtractUserInfo();
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();

            foreach (var entry in dbContext.ChangeTracker.Entries().ToList())
            {
                if (entry.Entity.GetType() == typeof(AuditLog)) continue;
                if (entry.Entity is not BaseEntity) continue;

                var entityType = entry.Entity.GetType().Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        AddAuditLog(dbContext, entry, "entity.created", entityType, userId, userEmail, userRole, ip, userAgent,
                            oldValues: null,
                            newValues: SerializeEntry(entry, isOld: false),
                            changedColumns: null);
                        break;

                    case EntityState.Modified:
                        AddAuditLog(dbContext, entry, "entity.updated", entityType, userId, userEmail, userRole, ip, userAgent,
                            oldValues: SerializeModifiedOld(entry),
                            newValues: SerializeModifiedNew(entry),
                            changedColumns: string.Join(",", GetModifiedProperties(entry)));
                        break;

                    case EntityState.Deleted:
                        AddAuditLog(dbContext, entry, "entity.deleted", entityType, userId, userEmail, userRole, ip, userAgent,
                            oldValues: SerializeEntry(entry, isOld: true),
                            newValues: null,
                            changedColumns: null);
                        break;
                }
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void AddAuditLog(
            DbContext dbContext,
            EntityEntry entry,
            string action,
            string entityType,
            Guid? userId, string? userEmail, string? role,
            string? ip, string? userAgent,
            string? oldValues, string? newValues, string? changedColumns)
        {
            var entityId = entry.Property("Id")?.CurrentValue?.ToString();

            var auditLog = new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                UserRole = role,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedColumns = changedColumns,
                IpAddress = ip,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
            };

            dbContext.Set<AuditLog>().Add(auditLog);
        }

        private static string? SerializeEntry(EntityEntry entry, bool isOld)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties)
            {
                dict[prop.Metadata.Name] = isOld ? prop.OriginalValue : prop.CurrentValue;
            }
            return JsonSerializer.Serialize(dict);
        }

        private static string? SerializeModifiedOld(EntityEntry entry)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                dict[prop.Metadata.Name] = prop.OriginalValue;
            }
            return dict.Count > 0 ? JsonSerializer.Serialize(dict) : null;
        }

        private static string? SerializeModifiedNew(EntityEntry entry)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                dict[prop.Metadata.Name] = prop.CurrentValue;
            }
            return dict.Count > 0 ? JsonSerializer.Serialize(dict) : null;
        }

        private static List<string> GetModifiedProperties(EntityEntry entry)
        {
            return entry.Properties
                .Where(p => p.IsModified)
                .Select(p => p.Metadata.Name)
                .ToList();
        }

        private (Guid? userId, string? email, string? role) ExtractUserInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return (null, null, null);

            var user = httpContext.User;
            var userIdStr = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            Guid.TryParse(userIdStr, out var userId);
            return (userId, email, role);
        }
    }
}