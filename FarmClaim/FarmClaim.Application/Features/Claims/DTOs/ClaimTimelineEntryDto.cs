using System.Text.Json;

namespace FarmClaim.Application.Features.Claims.DTOs
{
    public record ClaimTimelineEntryDto
    {
        public DateTime Timestamp { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? Description { get; init; }
        public JsonElement? OldValues { get; init; }
        public JsonElement? NewValues { get; init; }
        public string? ChangedColumns { get; init; }
    }
}
