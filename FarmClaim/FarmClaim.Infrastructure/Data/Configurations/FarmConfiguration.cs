using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmClaim.Domain.Entities;

namespace FarmClaim.Infrastructure.Data.Configurations
{
    public class FarmConfiguration : IEntityTypeConfiguration<Farm>
    {
        public void Configure(EntityTypeBuilder<Farm> builder)
        {
            builder.ToTable("Farms");
            builder.HasKey(f => f.Id);

            builder.HasIndex(f => f.UserId).HasDatabaseName("IX_Farms_UserId");

            builder.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.AreaInHectares)
                .IsRequired()
                .HasColumnType("decimal(18, 2)");

            builder.Property(f => f.Address)
                .HasMaxLength(500);

            builder.Property(f => f.Latitude);
            builder.Property(f => f.Longitude);

            builder.Property(f => f.LocationGeoJson)
                .IsUnicode(false);

            builder.Property(f => f.IsActive)
                .IsRequired();

            // Relationships
            builder.HasOne(f => f.User)
                  .WithMany(u => u.Farms)
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.InsurancePolicies)
                  .WithOne(p => p.Farm)
                  .HasForeignKey(p => p.FarmId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(f => f.Claims)
                  .WithOne(c => c.Farm)
                  .HasForeignKey(c => c.FarmId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}