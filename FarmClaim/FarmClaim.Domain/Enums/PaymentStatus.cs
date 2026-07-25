namespace FarmClaim.Domain.Enums
{
    public enum PaymentStatus
    {
        Created = 0,      // Order created on Razorpay, awaiting payment
        Attempted = 1,    // Payment attempted but not captured
        Captured = 2,     // Payment successful (verified)
        Failed = 3,       // Payment failed
        Refunded = 4,     // Payment refunded
        Expired = 5       // Order expired (no payment attempt in 15 min)
    }
}