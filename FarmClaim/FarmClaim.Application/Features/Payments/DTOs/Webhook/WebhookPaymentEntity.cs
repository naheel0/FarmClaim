using System.Text.Json.Serialization;

namespace FarmClaim.Application.Features.Payments.DTOs
{
    public class WebhookPaymentEntity
    {
        [JsonPropertyName("entity")]
        public string Entity { get; set; } = "payment";

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "INR";

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string? Method { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }

        [JsonPropertyName("fee")]
        public long? Fee { get; set; }

        [JsonPropertyName("tax")]
        public long? Tax { get; set; }
    }
}