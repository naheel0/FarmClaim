namespace FarmClaim.Application.Features.Payments.Commands.VerifyPayment
{
    public class PaymentSuccessEmailModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public DateTime CapturedAt { get; set; }
        public string DashboardUrl { get; set; } = string.Empty;
    }
}