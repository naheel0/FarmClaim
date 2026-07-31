using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.Application.Features.Admin.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, AdminUserDetailDto>
    {
        private readonly IApplicationDbContext _context;

        public GetUserByIdQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminUserDetailDto> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.StatusChangedBy)
                .Include(u => u.Farms)
                .Include(u => u.Claims)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

            if (user == null)
                throw new NotFoundException(nameof(User), request.UserId);

            return new AdminUserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                Status = user.Status,
                LastLoginAt = user.LastLoginAt,
                StatusChangedAt = user.StatusChangedAt,
                StatusChangedByUserId = user.StatusChangedByUserId,
                StatusChangedByName = user.StatusChangedBy != null
                    ? $"{user.StatusChangedBy.FirstName} {user.StatusChangedBy.LastName}"
                    : null,
                StatusChangeReason = user.StatusChangeReason,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                FarmsCount = user.Farms.Count(f => !f.IsDeleted),
                PoliciesCount = user.Farms.SelectMany(f => f.InsurancePolicies).Count(p => !p.IsDeleted),
                ClaimsCount = user.Claims.Count(c => !c.IsDeleted)
            };
        }
    }
}