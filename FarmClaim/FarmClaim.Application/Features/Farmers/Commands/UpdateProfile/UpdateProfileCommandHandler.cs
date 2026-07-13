using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farmers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.Farmers.Commands.UpdateProfile;

public class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommand, FarmerProfileDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FarmerProfileDto> Handle(
        UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null)
            throw new NotFoundException($"User with ID '{request.UserId}' not found.");

        if (request.Request.FirstName is not null)
            user.FirstName = request.Request.FirstName;

        if (request.Request.LastName is not null)
            user.LastName = request.Request.LastName;

        if (request.Request.PhoneNumber is not null)
            user.PhoneNumber = request.Request.PhoneNumber;

        await _context.SaveChangesAsync(ct);

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