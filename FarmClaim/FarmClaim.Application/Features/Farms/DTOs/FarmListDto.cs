using System;

namespace FarmClaim.Application.Features.Farms.DTOs
{
    public record FarmListDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public decimal AreaInHectares { get; init; }

        public string? Address { get; init; }

        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        public DateTime CreatedAt { get; init; }

        public bool IsActive { get; init; }

        public int PoliciesCount { get; init; }

        public int ClaimsCount { get; init; }
    }
}