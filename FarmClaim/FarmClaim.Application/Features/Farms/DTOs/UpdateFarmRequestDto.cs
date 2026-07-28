using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Farms.DTOs
{
    public record UpdateFarmRequestDto
    {
        [MaxLength(200)]
        public string? Name { get; init; }

        [Range(0.01, 1000000)]
        public decimal? AreaInHectares { get; init; }

        [MaxLength(500)]
        public string? Address { get; init; }

        public bool? IsActive { get; init; }

        // FIXED: Added all missing properties
        public double? Latitude { get; init; }

        public double? Longitude { get; init; }

        public string? LocationGeoJson { get; init; }
    }
}