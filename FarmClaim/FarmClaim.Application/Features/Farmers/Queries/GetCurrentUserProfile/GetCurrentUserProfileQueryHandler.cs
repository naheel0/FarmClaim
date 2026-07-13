using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farmers.DTOs;
using FarmClaim.Application.Features.Farmers.Queries.GetCurrentUserProfile;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.Farmers.Queries.GetCurrentUserProfile;

public class GetCurrentUserProfileQueryHandler
    : IRequestHandler<GetCurrentUserProfileQuery, FarmerProfileDto>
{
    private readonly IApplicationDbContext _context;

    public GetCurrentUserProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FarmerProfileDto> Handle(
        GetCurrentUserProfileQuery request, CancellationToken ct)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null)
            throw new NotFoundException($"User with ID '{request.UserId}' not found.");

        var totalFarms = await _context.Farms
            .CountAsync(f => f.UserId == request.UserId, ct);

        var farmIds = await _context.Farms
            .Where(f => f.UserId == request.UserId)
            .Select(f => f.Id)
            .ToListAsync(ct);

        int totalPolicies = 0;
        if (farmIds.Any())
        {
            totalPolicies = await _context.InsurancePolicies
                .CountAsync(p => farmIds.Contains(p.FarmId), ct);
        }

        var totalClaims = await _context.Claims
            .CountAsync(c => c.UserId == request.UserId, ct);

        return new FarmerProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalFarms = totalFarms,
            TotalPolicies = totalPolicies,
            TotalClaims = totalClaims
        };
    }
}