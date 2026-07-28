using System;

namespace FarmClaim.Application.Features.AuditLogs.DTOs
{
    public record AuditLogDetailDto
    {
        public Guid Id { get; init; }
        public Guid? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? UserRole { get; init; }
        public string Action { get; init; } = string.Empty;
        public string EntityType { get; init; } = string.Empty;
        public string? EntityId { get; init; }
        public string? OldValues { get; init; }
        public string? NewValues { get; init; }
        public string? ChangedColumns { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? Description { get; init; }
        public DateTime Timestamp { get; init; }

        // NEW: HTTP context tracking (matches AuditLog entity)
        public string? CorrelationId { get; init; }
        public string? HttpMethod { get; init; }
        public string? HttpPath { get; init; }
    }
}