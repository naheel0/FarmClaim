using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Farmers.DTOs;
using FarmClaim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace FarmClaim.Application.Features.Farmers.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, FarmerProfileDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<UpdateProfileCommandHandler> _logger;

        public UpdateProfileCommandHandler(
            IApplicationDbContext context,
            ILogger<UpdateProfileCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FarmerProfileDto> Handle(UpdateProfileCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Updating profile for user: {UserId}", command.UserId);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == command.UserId && !u.IsDeleted, ct);

            if (user == null)
            {
                _logger.LogWarning("User not found for update: {UserId}", command.UserId);
                throw new NotFoundException(nameof(User), command.UserId);
            }

            // Update only provided fields (partial update pattern)
            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(command.Request.FirstName))
            {
                user.FirstName = command.Request.FirstName.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Request.LastName))
            {
                user.LastName = command.Request.LastName.Trim();
                hasChanges = true;
            }

            if (command.Request.PhoneNumber != null)
            {
                user.PhoneNumber = command.Request.PhoneNumber?.Trim();
                hasChanges = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("No changes detected for user: {UserId}", command.UserId);
                return MapToDto(user);
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Profile updated successfully for user: {UserId}", command.UserId);

            return MapToDto(user);
        }

        private static FarmerProfileDto MapToDto(User user)
        {
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
                TotalFarms = 0, // Could query if needed
                TotalPolicies = 0,
                TotalClaims = 0
            };
        }
    }
}