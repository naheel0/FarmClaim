using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmClaim.Domain.Entities;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class ClaimImageConfiguration : IEntityTypeConfiguration<ClaimImage>
    {
        public void Configure(EntityTypeBuilder<ClaimImage> b)
        {
            b.ToTable("ClaimImages"); b.HasKey(i => i.Id);
            b.Property(i => i.ImageUrl).IsRequired().HasMaxLength(500);
            b.HasOne(i => i.Claim).WithMany(c => c.Images).HasForeignKey(i => i.ClaimId);
        }
    }
}