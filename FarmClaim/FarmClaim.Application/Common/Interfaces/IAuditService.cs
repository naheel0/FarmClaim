namespace FarmClaim.Application.Common.Interfaces
{
    /// <summary>
    /// Service for explicitly logging business actions (not auto-tracked entity changes).
    /// </summary>
    public interface IAuditService
    {
        Task LogActionAsync(
            string action,
            string entityType,
            string? entityId = null,
            string? description = null,
            object? oldValue = null,
            object? newValue = null,
            CancellationToken ct = default);
    }
}