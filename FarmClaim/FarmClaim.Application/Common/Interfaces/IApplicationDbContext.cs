using Microsoft.EntityFrameworkCore;
using FarmClaim.Domain.Entities;

namespace FarmClaim.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<Farm> Farms { get; set; }
        DbSet<InsurancePolicy> InsurancePolicies { get; set; }
        DbSet<Claim> Claims { get; set; }
        DbSet<ClaimImage> ClaimImages { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}