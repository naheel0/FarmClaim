using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Auth.DTOs
{
    public class ChangeEmailRequestDto
    {
        [Required(ErrorMessage = "New email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(256)]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}