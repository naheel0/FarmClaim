namespace FarmClaim.Application.Features.AuditLogs.DTOs
{
    public record AuditLogListDto
    {
        public Guid Id { get; init; }
        public Guid? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? UserRole { get; init; }
        public string Action { get; init; } = string.Empty;
        public string EntityType { get; init; } = string.Empty;
        public string? EntityId { get; init; }
        public string? Description { get; init; }
        public string? IpAddress { get; init; }
        public DateTime Timestamp { get; init; }
    }
}