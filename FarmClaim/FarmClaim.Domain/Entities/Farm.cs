using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("Farms")]
    public class Farm : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        // FIXED: Added explicit column type for decimal
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AreaInHectares { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        // GeoLocation data
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? LocationGeoJson { get; set; }

        // FIXED: Added IsActive property
        [Required]
        public bool IsActive { get; set; } = true;

        // FIXED: Added navigation collections
        public virtual ICollection<InsurancePolicy> InsurancePolicies { get; set; } = new List<InsurancePolicy>();

        public virtual ICollection<FarmClaim.Domain.Entities.Claim> Claims { get; set; } = new List<FarmClaim.Domain.Entities.Claim>();
        [MaxLength(100)]
        public string? CropType { get; set; }
    }
}