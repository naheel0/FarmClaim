using System;

namespace FarmClaim.Application.Features.Farms.DTOs
{
    public record FarmResponseDto
    {
        public Guid Id { get; init; }

        public Guid UserId { get; init; }

        public string Name { get; init; } = string.Empty;

        public decimal AreaInHectares { get; init; }

        public string? Address { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        public string? LocationGeoJson { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? UpdatedAt { get; init; }

        public bool IsActive { get; init; }

        // Aggregated data
        public int PoliciesCount { get; init; }

        public int ClaimsCount { get; init; }
    }
}