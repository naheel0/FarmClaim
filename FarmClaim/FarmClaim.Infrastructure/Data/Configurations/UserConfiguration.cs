using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> b)
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Email).IsUnique();

            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            b.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            b.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            b.Property(u => u.Role).IsRequired().HasConversion<string>();

            // === NEW: UserStatus configuration ===
            b.Property(u => u.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(UserStatus.Active);

            b.Property(u => u.StatusChangeReason).HasMaxLength(500);

            b.HasIndex(u => u.Status);

            // Self-referencing FK: User modified by another User (Admin)
            b.HasOne(u => u.StatusChangedBy)
                .WithMany()
                .HasForeignKey(u => u.StatusChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(u => u.RefreshToken)
                .WithOne(rt => rt.User)
                .HasForeignKey<RefreshToken>(rt => rt.UserId);
        }
    }
}