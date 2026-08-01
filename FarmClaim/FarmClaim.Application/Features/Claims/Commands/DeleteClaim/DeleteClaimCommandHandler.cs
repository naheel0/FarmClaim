using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.DeleteClaim
{
    public class DeleteClaimCommandHandler : IRequestHandler<DeleteClaimCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DeleteClaimCommandHandler> _logger;

        public DeleteClaimCommandHandler(
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<DeleteClaimCommandHandler> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Unit> Handle(DeleteClaimCommand command, CancellationToken ct)
        {
            _logger.LogInformation("Deleting claim: {ClaimId} for user: {UserId}", command.ClaimId, command.UserId);

            var claim = await _context.Claims
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == command.ClaimId
                    && c.UserId == command.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
            {
                _logger.LogWarning("Claim not found: {ClaimId} or not owned by user: {UserId}", command.ClaimId, command.UserId);
                throw new NotFoundException(nameof(Claim), command.ClaimId);
            }

            if (claim.Status != ClaimStatus.Pending)
                throw new ValidationException(new List<string> { $"Cannot delete claim with status '{claim.Status}'. Only pending claims can be deleted." });

            // Clean up images from Cloudinary before soft-deleting
            if (claim.Images != null && claim.Images.Count > 0)
            {
                foreach (var image in claim.Images)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(image.ImageUrl))
                        {
                            var publicId = FileValidationHelper.ExtractPublicId(image.ImageUrl);
                            if (!string.IsNullOrEmpty(publicId))
                            {
                                await _fileStorage.DeleteAsync(publicId, ct);
                                _logger.LogInformation("Deleted image {ImageId} from storage for claim {ClaimId}", image.Id, command.ClaimId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete image {ImageId} from storage. Continuing with claim deletion.", image.Id);
                    }
                }
            }

            claim.IsDeleted = true;
            claim.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Claim deleted: {ClaimId}", claim.Id);
            return Unit.Value;
        }
    }
}