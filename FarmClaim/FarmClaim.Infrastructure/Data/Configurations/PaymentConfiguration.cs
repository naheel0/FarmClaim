using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> b)
        {
            b.ToTable("Payments");
            b.HasKey(p => p.Id);

            b.Property(p => p.OrderId).IsRequired().HasMaxLength(100);
            b.Property(p => p.PaymentId).HasMaxLength(100);
            b.Property(p => p.Currency).IsRequired().HasMaxLength(3);
            b.Property(p => p.Signature).HasMaxLength(500);
            b.Property(p => p.Method).HasMaxLength(50);
            b.Property(p => p.MethodDescription).HasMaxLength(200);
            b.Property(p => p.BankReference).HasMaxLength(100);
            b.Property(p => p.FailureReason).HasMaxLength(1000);
            b.Property(p => p.ReceiptNumber).HasMaxLength(50);
            b.Property(p => p.ClientIp).HasMaxLength(45);
            b.Property(p => p.UserAgent).HasMaxLength(500);
            b.Property(p => p.Notes).HasMaxLength(500);

            b.Property(p => p.AmountInRupees).HasColumnType("decimal(18, 2)");
            b.Property(p => p.Fee).HasColumnType("decimal(18, 2)");
            b.Property(p => p.Tax).HasColumnType("decimal(18, 2)");

            b.Property(p => p.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(PaymentStatus.Created);

            b.HasIndex(p => p.OrderId).IsUnique();
            b.HasIndex(p => p.PaymentId);
            b.HasIndex(p => p.PolicyId);
            b.HasIndex(p => p.UserId);
            b.HasIndex(p => p.Status);
            b.HasIndex(p => p.ReceiptNumber).IsUnique();

            b.HasOne(p => p.Policy)
                .WithMany(p => p.Payments)
                .HasForeignKey(p => p.PolicyId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}