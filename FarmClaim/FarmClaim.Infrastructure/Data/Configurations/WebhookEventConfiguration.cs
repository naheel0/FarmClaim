using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
    {
        public void Configure(EntityTypeBuilder<WebhookEvent> b)
        {
            b.ToTable("WebhookEvents");
            b.HasKey(w => w.Id);

            b.Property(w => w.EventId).IsRequired().HasMaxLength(100);
            b.Property(w => w.EventType).IsRequired().HasMaxLength(50);
            b.Property(w => w.Payload).HasColumnType("nvarchar(max)");
            b.Property(w => w.OrderId).HasMaxLength(200);
            b.Property(w => w.PaymentId).HasMaxLength(100);
            b.Property(w => w.ProcessingError).HasMaxLength(500);

            b.HasIndex(w => w.EventId).IsUnique();
            b.HasIndex(w => w.ProcessedAt);
            b.HasIndex(w => w.OrderId);
            b.HasIndex(w => w.PaymentId);
        }
    }
}
