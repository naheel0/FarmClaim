using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Admin.Commands.ActivateUser
{
    public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, UserActionResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ActivateUserCommandHandler> _logger;

        public ActivateUserCommandHandler(
            IApplicationDbContext context,
            ILogger<ActivateUserCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserActionResponseDto> Handle(ActivateUserCommand cmd, CancellationToken ct)
        {
            _logger.LogInformation("Admin {AdminId} activating user {UserId}",
                cmd.AdminUserId, cmd.TargetUserId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == cmd.TargetUserId && !u.IsDeleted, ct);

            if (user == null)
                throw new NotFoundException(nameof(User), cmd.TargetUserId);

            if (user.Role == UserRole.Admin)
                throw new ForbiddenException("Cannot activate an Admin user.");

            if (user.Status == UserStatus.Blocked)
                throw new ForbiddenException("Blocked users cannot be re-activated (blocking is permanent).");

            if (user.Status == UserStatus.Active)
                throw new ValidationException(new List<string> { "User is already active." });

            var previousStatus = user.Status;

            user.Status = UserStatus.Active;
            user.StatusChangedAt = DateTime.UtcNow;
            user.StatusChangedByUserId = cmd.AdminUserId;
            user.StatusChangeReason = cmd.Request.Reason.Trim();

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} activated by Admin {AdminId}. Reason: {Reason}",
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
    }
}