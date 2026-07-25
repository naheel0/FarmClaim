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

        /// <summary>
        /// Who performed the action (null for anonymous/system actions)
        /// </summary>
        public Guid? UserId { get; set; }

        [MaxLength(256)]
        public string? UserEmail { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        /// <summary>
        /// What happened: "policy.approved", "user.suspended", "payment.captured", "entity.updated"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Entity type affected: "InsurancePolicy", "User", "Payment", "Claim"
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the affected entity (string for flexibility)
        /// </summary>
        [MaxLength(100)]
        public string? EntityId { get; set; }

        /// <summary>
        /// JSON snapshot of values before change (null for creates)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON snapshot of values after change (null for deletes)
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }

        /// <summary>
        /// Comma-separated list of changed property names
        /// </summary>
        [MaxLength(2000)]
        public string? ChangedColumns { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Optional human-readable description
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}