using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Auth.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}