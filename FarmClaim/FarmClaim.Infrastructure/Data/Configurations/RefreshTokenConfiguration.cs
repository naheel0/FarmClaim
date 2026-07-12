using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmClaim.Domain.Entities;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.ToTable("RefreshTokens"); b.HasKey(rt => rt.Id); b.HasIndex(rt => rt.Token).IsUnique();
            b.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
            b.HasOne(rt => rt.User).WithOne(u => u.RefreshToken).HasForeignKey<RefreshToken>(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}