using FarmClaim.Domain.Enums;
using System.Collections.Generic;
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

        // Navigation Properties
        public virtual RefreshToken? RefreshToken { get; set; }

        public virtual ICollection<Farm> Farms { get; set; } = new List<Farm>();

        public virtual ICollection<InsurancePolicy> Policies { get; set; } = new List<InsurancePolicy>();

        public virtual ICollection<FarmClaim.Domain.Entities.Claim> Claims { get; set; } = new List<FarmClaim.Domain.Entities.Claim>();

        // NEW: Admin review tracking
        public virtual ICollection<InsurancePolicy> ApprovedPolicies { get; set; } = new List<InsurancePolicy>();
        public virtual ICollection<FarmClaim.Domain.Entities.Claim> ReviewedClaims { get; set; } = new List<FarmClaim.Domain.Entities.Claim>();
    }
}