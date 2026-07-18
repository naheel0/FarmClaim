using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class InsurancePolicyConfiguration : IEntityTypeConfiguration<InsurancePolicy>
    {
        public void Configure(EntityTypeBuilder<InsurancePolicy> b)
        {
            b.ToTable("InsurancePolicies");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.PolicyNumber).IsUnique();

            b.Property(p => p.PolicyNumber).IsRequired().HasMaxLength(50);

            // NEW: Status instead of IsActive
            b.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PolicyStatus.Pending);

            b.Property(p => p.RejectionReason).HasMaxLength(1000);

            // Existing FK
            b.HasOne(p => p.Farm)
                .WithMany(f => f.InsurancePolicies)
                .HasForeignKey(p => p.FarmId);

            // NEW: Admin who approved this policy
            b.HasOne(p => p.ApprovedByUser)
                .WithMany(u => u.ApprovedPolicies)
                .HasForeignKey(p => p.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // NEW: Index for filtering by status
            b.HasIndex(p => p.Status);
        }
    }
}