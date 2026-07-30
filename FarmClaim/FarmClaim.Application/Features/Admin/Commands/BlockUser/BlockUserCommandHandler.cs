using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Common.Models;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.BlockUser
{
    public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, UserActionResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailQueueService _emailQueue;
        private readonly ILogger<BlockUserCommandHandler> _logger;

        public BlockUserCommandHandler(
            IApplicationDbContext context,
            IEmailQueueService emailQueue,
            ILogger<BlockUserCommandHandler> logger)
        {
            _context = context;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        public async Task<UserActionResponseDto> Handle(BlockUserCommand cmd, CancellationToken ct)
        {
            _logger.LogWarning("Admin {AdminId} BLOCKING user {UserId}", cmd.AdminUserId, cmd.TargetUserId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == cmd.TargetUserId && !u.IsDeleted, ct);

            if (user == null)
                throw new NotFoundException(nameof(User), cmd.TargetUserId);

            if (user.Id == cmd.AdminUserId)
                throw new ForbiddenException("Cannot block your own account.");

            if (user.Role == UserRole.Admin)
                throw new ForbiddenException("Cannot block an Admin user.");

            if (user.Status == UserStatus.Blocked)
                throw new ValidationException(new List<string> { "User is already blocked." });

            var previousStatus = user.Status;

            user.Status = UserStatus.Blocked;
            user.StatusChangedAt = DateTime.UtcNow;
            user.StatusChangedByUserId = cmd.AdminUserId;
            user.StatusChangeReason = cmd.Request.Reason.Trim();

            await RevokeAllUserRefreshTokensAsync(user.Id, cmd.AdminUserId, "User blocked by admin", ct);
            await _context.SaveChangesAsync(ct);

            // Send block email (non-blocking — logged on failure)
            try
            {
                await _emailQueue.EnqueueEmailAsync(
                    toEmail: user.Email,
                    templateName: "UserBlockedEmail",
                    model: new UserSuspendedEmailModel
                    {
                        UserName = $"{user.FirstName} {user.LastName}",
                        UserEmail = user.Email,
                        Reason = cmd.Request.Reason,
                        SuspendedAt = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send block email to {Email}", user.Email);
            }

            _logger.LogWarning("User {UserId} PERMANENTLY BLOCKED by Admin {AdminId}. Reason: {Reason}",
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
                StatusChangedByName = admin != null ? $"{admin.FirstName} {admin.LastName}" : null,
                Reason = user.StatusChangeReason
            };
        }

        private async Task RevokeAllUserRefreshTokensAsync(Guid userId, Guid adminId, string reason, CancellationToken ct)
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