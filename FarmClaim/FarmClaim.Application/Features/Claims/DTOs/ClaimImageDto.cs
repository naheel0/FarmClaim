using System;

namespace FarmClaim.Application.Features.Claims.DTOs
{
    public record ClaimImageDto
    {
        public Guid Id { get; init; }
        public string ImageUrl { get; init; } = string.Empty;
        public string? ThumbnailUrl { get; init; }
        public string? FileName { get; init; }
        public string? FileType { get; init; }
        public long? FileSizeBytes { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsPrimary { get; init; }
    }
}