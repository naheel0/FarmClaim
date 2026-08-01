using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class PremiumScheduleConfiguration : IEntityTypeConfiguration<PremiumSchedule>
    {
        public void Configure(EntityTypeBuilder<PremiumSchedule> b)
        {
            b.ToTable("PremiumSchedules");
            b.HasKey(s => s.Id);

            b.Property(s => s.InstallmentNumber).IsRequired();
            b.Property(s => s.DueDate).IsRequired();
            b.Property(s => s.AmountDue).HasColumnType("decimal(18, 2)");

            b.Property(s => s.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PremiumScheduleStatus.Pending);

            b.HasOne(s => s.Policy)
                .WithMany(p => p.PremiumSchedules)
                .HasForeignKey(s => s.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Payment)
                .WithMany()
                .HasForeignKey(s => s.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(s => s.PolicyId);
            b.HasIndex(s => s.Status);
            b.HasIndex(s => s.DueDate);
            b.HasIndex(s => new { s.PolicyId, s.Status, s.InstallmentNumber });
        }
    }
}
