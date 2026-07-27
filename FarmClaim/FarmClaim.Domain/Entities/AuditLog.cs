using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? UserId { get; set; }

        [MaxLength(256)]
        public string? UserEmail { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }

        [MaxLength(2000)]
        public string? ChangedColumns { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // === NEW FIELDS ===

        /// <summary>Groups all audit entries from a single HTTP request.</summary>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        /// <summary>HTTP method: GET, POST, PUT, PATCH, DELETE</summary>
        [MaxLength(10)]
        public string? HttpMethod { get; set; }

        /// <summary>HTTP path: /api/v1/Admin/Users/123/suspend</summary>
        [MaxLength(500)]
        public string? HttpPath { get; set; }
    }
}