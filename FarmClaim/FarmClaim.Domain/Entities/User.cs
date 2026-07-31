using FarmClaim.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmClaim.Domain.Entities
{
    [Table("Users")]
    public class User : BaseEntity
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        [Column(TypeName = "nvarchar(256)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        [MaxLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [MaxLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string? PhoneNumber { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public UserRole Role { get; set; } = UserRole.Farmer;

        public DateTime? LastLoginAt { get; set; }

        // === User Status (suspend/block/reactivate) ===
        [Required]
        [Column(TypeName = "nvarchar(20)")]
        public UserStatus Status { get; set; } = UserStatus.Active;

        public DateTime? StatusChangedAt { get; set; }

        public Guid? StatusChangedByUserId { get; set; }

        [ForeignKey(nameof(StatusChangedByUserId))]
        public virtual User? StatusChangedBy { get; set; }

        [MaxLength(500)]
        public string? StatusChangeReason { get; set; }
        // Navigation Properties
        public virtual RefreshToken? RefreshToken { get; set; }

        public virtual ICollection<Farm> Farms { get; set; } = new List<Farm>();

        public virtual ICollection<FarmClaim.Domain.Entities.Claim> Claims { get; set; } = new List<FarmClaim.Domain.Entities.Claim>();

        // Admin review tracking
        public virtual ICollection<InsurancePolicy> ApprovedPolicies { get; set; } = new List<InsurancePolicy>();
        public virtual ICollection<FarmClaim.Domain.Entities.Claim> ReviewedClaims { get; set; } = new List<FarmClaim.Domain.Entities.Claim>();
    }
}