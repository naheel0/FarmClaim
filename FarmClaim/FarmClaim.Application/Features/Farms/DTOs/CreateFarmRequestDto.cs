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
}