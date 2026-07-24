using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class EmailChangeTokenConfiguration : IEntityTypeConfiguration<EmailChangeToken>
    {
        public void Configure(EntityTypeBuilder<EmailChangeToken> b)
        {
            b.ToTable("EmailChangeTokens");
            b.HasKey(t => t.Id);

            b.Property(t => t.NewEmail).IsRequired().HasMaxLength(256);
            b.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            b.Property(t => t.ExpiresAt).IsRequired();
            b.Property(t => t.CreatedByIp).HasMaxLength(45);

            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => new { t.UserId, t.UsedAt, t.ExpiresAt });

            b.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}