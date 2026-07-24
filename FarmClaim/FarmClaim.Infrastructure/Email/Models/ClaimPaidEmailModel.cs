namespace FarmClaim.Infrastructure.Email.Models
{
    public class ClaimPaidEmailModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public Guid ClaimId { get; set; }
        public decimal PayoutAmount { get; set; }
        public DateTime PaidAt { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
    }
}