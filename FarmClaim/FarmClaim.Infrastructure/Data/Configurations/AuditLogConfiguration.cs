using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> b)
        {
            b.ToTable("AuditLogs");
            b.HasKey(a => a.Id);

            b.Property(a => a.Action).IsRequired().HasMaxLength(100);
            b.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
            b.Property(a => a.EntityId).HasMaxLength(100);
            b.Property(a => a.UserEmail).HasMaxLength(256);
            b.Property(a => a.UserRole).HasMaxLength(50);
            b.Property(a => a.IpAddress).HasMaxLength(45);
            b.Property(a => a.UserAgent).HasMaxLength(500);
            b.Property(a => a.Description).HasMaxLength(1000);
            b.Property(a => a.ChangedColumns).HasMaxLength(2000);

            b.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
            b.Property(a => a.NewValues).HasColumnType("nvarchar(max)");

            // Indexes for fast querying
            b.HasIndex(a => a.Timestamp);
            b.HasIndex(a => a.UserId);
            b.HasIndex(a => a.EntityType);
            b.HasIndex(a => a.Action);
            b.HasIndex(a => new { a.EntityType, a.EntityId });
            // === NEW INDEXES ===
            b.HasIndex(a => a.CorrelationId);
        }
    }
}