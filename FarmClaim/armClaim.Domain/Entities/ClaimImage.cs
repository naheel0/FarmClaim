using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("ClaimImages")]
    public class ClaimImage : BaseEntity
    {
        [Required]
        public Guid ClaimId { get; set; }

        [ForeignKey(nameof(ClaimId))]
        public virtual Claim Claim { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        [MaxLength(100)]
        public string? FileName { get; set; }

        [MaxLength(10)]
        public string? FileType { get; set; }

        public long? FileSizeBytes { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;
    }
}