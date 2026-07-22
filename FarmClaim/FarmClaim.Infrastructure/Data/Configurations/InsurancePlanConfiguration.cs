using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class InsurancePlanConfiguration : IEntityTypeConfiguration<InsurancePlan>
    {
        public void Configure(EntityTypeBuilder<InsurancePlan> b)
        {
            b.ToTable("InsurancePlans");
            b.HasKey(p => p.Id);

            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Description).HasMaxLength(2000);
            b.Property(p => p.CropType).IsRequired().HasMaxLength(100);
            b.Property(p => p.Provider).IsRequired().HasMaxLength(200);

            b.Property(p => p.PremiumRatePerHectare)
                .IsRequired()
                .HasColumnType("decimal(18, 2)");

            b.Property(p => p.SumInsuredPerHectare)
                .IsRequired()
                .HasColumnType("decimal(18, 2)");

            b.Property(p => p.CoveragePercentage)
                .IsRequired()
                .HasColumnType("decimal(5, 2)");

            b.Property(p => p.MinAreaInHectares).HasColumnType("decimal(18, 2)");
            b.Property(p => p.MaxAreaInHectares).HasColumnType("decimal(18, 2)");

            b.Property(p => p.PolicyDurationMonths).IsRequired();
            b.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

            b.HasIndex(p => p.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            b.HasIndex(p => p.CropType);
            b.HasIndex(p => p.IsActive);

            b.HasMany(p => p.Policies)
                .WithOne(p => p.InsurancePlan!)
                .HasForeignKey(p => p.InsurancePlanId)
             .OnDelete(DeleteBehavior.SetNull);
        }
    }
}