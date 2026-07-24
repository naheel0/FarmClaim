using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
    {
        public void Configure(EntityTypeBuilder<EmailVerificationCode> b)
        {
            b.ToTable("EmailVerificationCodes");
            b.HasKey(c => c.Id);

            b.Property(c => c.CodeHash).IsRequired().HasMaxLength(128);
            b.Property(c => c.ExpiresAt).IsRequired();
            b.Property(c => c.CreatedByIp).HasMaxLength(45);

            b.HasIndex(c => c.CodeHash).IsUnique();
            b.HasIndex(c => new { c.UserId, c.UsedAt, c.ExpiresAt });

            b.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}