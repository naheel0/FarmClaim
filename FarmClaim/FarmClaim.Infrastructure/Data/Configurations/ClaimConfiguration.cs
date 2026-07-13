using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FarmClaim.Domain.Entities;

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
            b.HasOne(c => c.Policy).WithMany(p => p.Claims).HasForeignKey(c => c.PolicyId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(c => c.User).WithMany(u => u.Claims).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}