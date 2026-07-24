using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> b)
        {
            b.ToTable("PasswordResetTokens");
            b.HasKey(t => t.Id);

            b.Property(t => t.TokenHash)
                .IsRequired()
                .HasMaxLength(128);

            b.Property(t => t.ExpiresAt).IsRequired();
            b.Property(t => t.CreatedByIp).HasMaxLength(45);

            // Unique index on TokenHash — fast lookups + prevents duplicate hashes
            b.HasIndex(t => t.TokenHash).IsUnique();

            // Index on UserId + UsedAt + ExpiresAt — for cleanup queries
            b.HasIndex(t => new { t.UserId, t.UsedAt, t.ExpiresAt });

            b.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // When user deleted, tokens go too
        }
    }
}