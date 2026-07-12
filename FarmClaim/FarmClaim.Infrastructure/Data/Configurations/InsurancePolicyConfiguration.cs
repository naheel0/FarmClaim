using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmClaim.Domain.Entities;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class InsurancePolicyConfiguration : IEntityTypeConfiguration<InsurancePolicy>
    {
        public void Configure(EntityTypeBuilder<InsurancePolicy> b)
        {
            b.ToTable("InsurancePolicies"); b.HasKey(p => p.Id); b.HasIndex(p => p.PolicyNumber).IsUnique();
            b.Property(p => p.PolicyNumber).IsRequired().HasMaxLength(50);
            b.HasOne(p => p.Farm).WithMany(f => f.InsurancePolicies).HasForeignKey(p => p.FarmId);
        }
    }
}