using System.Text.Json.Serialization;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class WebhookPayload
    {
        [JsonPropertyName("payment")]
        public WebhookPaymentEntity? Payment { get; set; }

        [JsonPropertyName("refund")]
        public WebhookRefundEntity? Refund { get; set; }

        [JsonPropertyName("order")]
        public WebhookOrderEntity? Order { get; set; }
    }
}