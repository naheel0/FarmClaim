using System.ComponentModel.DataAnnotations;
using FarmClaim.Domain.Enums;

namespace FarmClaim.Application.Features.Auth.DTOs
{
    public record RegisterRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; init; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        public string LastName { get; init; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; init; }

        [Required(ErrorMessage = "Role is required")]
        public UserRole Role { get; init; }
    }
}