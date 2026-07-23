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

            // Status instead of IsActive
            b.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PolicyStatus.Pending);

            b.Property(p => p.RejectionReason).HasMaxLength(1000);

            // Existing FK: Farm
            b.HasOne(p => p.Farm)
                .WithMany(f => f.InsurancePolicies)
                .HasForeignKey(p => p.FarmId);

            // Admin who approved this policy
            b.HasOne(p => p.ApprovedByUser)
                .WithMany(u => u.ApprovedPolicies)
                .HasForeignKey(p => p.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(p => p.Status);

            // === InsurancePlan relationship (nullable FK, soft-delete safe) ===
            b.HasOne(p => p.InsurancePlan)
                .WithMany(plan => plan.Policies)
                .HasForeignKey(p => p.InsurancePlanId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(p => p.InsurancePlanId)
                .HasColumnType("uniqueidentifier")
                .IsRequired(false);

            b.HasIndex(p => p.InsurancePlanId);
        }
    }
}