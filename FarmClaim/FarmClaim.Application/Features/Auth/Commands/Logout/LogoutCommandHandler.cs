using FarmClaim.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<LogoutCommandHandler> _logger;

        public LogoutCommandHandler(
            IApplicationDbContext context,
            ILogger<LogoutCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> Handle(LogoutCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Logging out: {UserId}", request.UserId);

            var user = await _context.Users
                .Include(u => u.RefreshToken)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

            if (user?.RefreshToken != null)
            {
                user.RefreshToken.IsRevoked = true;
                user.RefreshToken.RevokedAt = DateTime.UtcNow;
                user.RefreshToken.ReasonRevoked = "User logged out";
                await _context.SaveChangesAsync(ct);
            }
            return true;
        }
    }
}