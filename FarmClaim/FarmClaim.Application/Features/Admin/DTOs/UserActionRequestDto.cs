using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Admin.DTOs
{
    public class UserActionRequestDto
    {
        [Required(ErrorMessage = "Reason is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}