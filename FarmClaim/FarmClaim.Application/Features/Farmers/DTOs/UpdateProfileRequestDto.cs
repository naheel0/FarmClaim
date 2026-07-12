using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Farmers.DTOs
{
    public record UpdateProfileRequestDto
    {
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? FirstName { get; init; }

        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string? LastName { get; init; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; init; }
    }
}