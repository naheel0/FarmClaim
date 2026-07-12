namespace FarmClaim.Application.Features.Farms.DTOs
{
    public record LocationRequestDto
    {
        public double Latitude { get; init; }

        public double Longitude { get; init; }

        public string? GeoJson { get; init; } // GeoJSON format for complex shapes
    }
}