using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("WebhookEvents")]
    public class WebhookEvent : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string EventId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? OrderId { get; set; }

        [MaxLength(100)]
        public string? PaymentId { get; set; }

        public DateTime? ProcessedAt { get; set; }

        [MaxLength(500)]
        public string? ProcessingError { get; set; }
    }
}
