using System.ComponentModel.DataAnnotations;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class VerifyPaymentRequestDto
    {
        [Required(ErrorMessage = "Razorpay Order ID is required")]
        [MaxLength(100)]
        public string RazorpayOrderId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Razorpay Payment ID is required")]
        [MaxLength(100)]
        public string RazorpayPaymentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Razorpay Signature is required")]
        [MaxLength(500)]
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}