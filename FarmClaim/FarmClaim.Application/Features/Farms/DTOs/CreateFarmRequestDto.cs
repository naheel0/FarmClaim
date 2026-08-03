using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Farms.DTOs;

public class CreateFarmRequestDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 100000, ErrorMessage = "Area must be greater than 0")]
    public decimal AreaInHectares { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? CropType { get; set; }

    // GEO: Required for weather/AI analysis. Frontend farmer dashboard depends on these.
    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }
}