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

            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == request.UserId && !t.IsRevoked)
                .ToListAsync(ct);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = "User logged out";
            }

            if (activeTokens.Count > 0)
                await _context.SaveChangesAsync(ct);

            return true;
        }
    }
}