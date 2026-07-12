using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmClaim.Domain.Entities;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("Users"); b.HasKey(u => u.Id); b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            b.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            b.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            b.Property(u => u.Role).IsRequired().HasConversion<string>();
            b.HasOne(u => u.RefreshToken).WithOne(rt => rt.User).HasForeignKey<RefreshToken>(rt => rt.UserId);
        }
    }
}