using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.SuspendUser
{
    public class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand, UserActionResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<SuspendUserCommandHandler> _logger;

        public SuspendUserCommandHandler(
            IApplicationDbContext context,
            ILogger<SuspendUserCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserActionResponseDto> Handle(SuspendUserCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} suspending user {UserId}",
                cmd.AdminUserId, cmd.TargetUserId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == cmd.TargetUserId && !u.IsDeleted, ct);

            if (user == null)
                throw new NotFoundException(nameof(User), cmd.TargetUserId);

            if (user.Role == UserRole.Admin)
                throw new ForbiddenException("Cannot suspend an Admin user.");

            if (user.Status == UserStatus.Suspended)
                throw new ValidationException(new List<string> { "User is already suspended." });

            if (user.Status == UserStatus.Blocked)
                throw new ValidationException(new List<string>
                {
                    "User is blocked. Activate first, then suspend if needed."
                });

            var previousStatus = user.Status;

            user.Status = UserStatus.Suspended;
            user.StatusChangedAt = DateTime.UtcNow;
            user.StatusChangedByUserId = cmd.AdminUserId;
            user.StatusChangeReason = cmd.Request.Reason.Trim();

            // Revoke all active refresh tokens (force re-login on next attempt)
            await RevokeAllUserRefreshTokensAsync(user.Id, cmd.AdminUserId, "User suspended by admin", ct);

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} suspended by Admin {AdminId}. Reason: {Reason}",
                user.Id, cmd.AdminUserId, cmd.Request.Reason);

            var admin = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == cmd.AdminUserId, ct);

            return new UserActionResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                PreviousStatus = previousStatus,
                NewStatus = user.Status,
                StatusChangedAt = user.StatusChangedAt,
                StatusChangedByUserId = user.StatusChangedByUserId,
                StatusChangedByName = admin != null
                    ? $"{admin.FirstName} {admin.LastName}"
                    : null,
                Reason = user.StatusChangeReason
            };
        }

        private async Task RevokeAllUserRefreshTokensAsync(
            Guid userId, Guid adminId, string reason, CancellationToken ct)
        {
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync(ct);

            foreach (var t in tokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
                t.ReasonRevoked = $"{reason} (by admin {adminId})";
            }
        }
    }
}