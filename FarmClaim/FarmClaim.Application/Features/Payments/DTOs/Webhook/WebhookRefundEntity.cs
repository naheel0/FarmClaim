using System.Text.Json.Serialization;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class WebhookRefundEntity
    {
        [JsonPropertyName("entity")]
        public string Entity { get; set; } = "refund";

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("payment_id")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("speed_processed")]
        public string? Speed { get; set; }
    }
}