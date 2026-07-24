using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RefreshTokenEntity = FarmClaim.Domain.Entities.RefreshToken;

namespace FarmClaim.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshTokenEntity> RefreshTokens { get; set; } = null!;
        public DbSet<Farm> Farms { get; set; } = null!;
        public DbSet<InsurancePlan> InsurancePlans { get; set; } = null!;
        public DbSet<InsurancePolicy> InsurancePolicies { get; set; } = null!;
        public DbSet<FarmClaim.Domain.Entities.Claim> Claims { get; set; } = null!;
        public DbSet<ClaimImage> ClaimImages { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<EmailChangeToken> EmailChangeTokens { get; set; } = null!;
        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Farm>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InsurancePlan>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InsurancePolicy>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<FarmClaim.Domain.Entities.Claim>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ClaimImage>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
            }

            return await base.SaveChangesAsync(ct);
        }
    }
}