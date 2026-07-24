using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Auth.DTOs
{
    public class ConfirmEmailChangeDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;
    }
}