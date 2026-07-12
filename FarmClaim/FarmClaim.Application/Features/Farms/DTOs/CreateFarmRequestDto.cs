using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Farms.DTOs
{
    public record CreateFarmRequestDto
    {
        [Required(ErrorMessage = "Farm name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; init; } = string.Empty;

        [Range(0.01, 1000000, ErrorMessage = "Area must be between 0.01 and 1,000,000 hectares")]
        public decimal AreaInHectares { get; init; }

        [MaxLength(500, ErrorMessage = "Address too long")]
        public string? Address { get; init; }
    }
}