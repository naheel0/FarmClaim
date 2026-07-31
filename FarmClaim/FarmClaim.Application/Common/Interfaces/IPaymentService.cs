using FarmClaim.Application.Features.Payments.DTOs;

namespace FarmClaim.Application.Common.Interfaces
{
    public interface IPaymentService
    {
        Task<CreateOrderResponseDto> CreateOrderAsync(
            decimal amountInRupees,
            string currency,
            string receipt,
            Guid policyId,
            Guid userId,
            CancellationToken ct = default);

        Task<bool> VerifySignatureAsync(
            string orderId,
            string paymentId,
            string signature);

        Task<PaymentDetailsDto> FetchPaymentDetailsAsync(string paymentId);

        bool VerifyWebhookSignature(string payload, string signature);

        Task<RefundResultDto> RefundPaymentAsync(
            string razorpayPaymentId,
            decimal amountInRupees,
            string reason,
            CancellationToken ct = default);
    }
}