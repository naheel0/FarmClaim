using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
    {
        public void Configure(EntityTypeBuilder<Claim> b)
        {
            b.ToTable("Claims");
            b.HasKey(c => c.Id);

            b.Property(c => c.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(c => c.IncidentType).HasConversion<string>().HasMaxLength(50);

            // Existing FKs (untouched)
            b.HasOne(c => c.Policy)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // NEW: Admin who reviewed this claim
            b.HasOne(c => c.ReviewedByUser)
                .WithMany(u => u.ReviewedClaims)
                .HasForeignKey(c => c.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // NEW: Payment tracking
            b.Property(c => c.PaymentReference).HasMaxLength(100);

            // Existing indexes (untouched)
            b.HasIndex(c => c.UserId);
            b.HasIndex(c => c.PolicyId);
            b.HasIndex(c => c.Status);
        }
    }
}