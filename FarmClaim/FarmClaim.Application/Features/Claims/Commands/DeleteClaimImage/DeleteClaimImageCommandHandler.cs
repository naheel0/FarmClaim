using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.DeleteClaimImage
{
    public class DeleteClaimImageCommandHandler : IRequestHandler<DeleteClaimImageCommand, Unit>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DeleteClaimImageCommandHandler> _logger;

        public DeleteClaimImageCommandHandler(
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<DeleteClaimImageCommandHandler> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeleteClaimImageCommand request, CancellationToken ct)
        {
            var claim = await _context.Claims
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId
                    && c.UserId == request.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException(nameof(Claim), request.ClaimId);

            if (claim.Status != ClaimStatus.Pending)
                throw new ValidationException(new List<string>
                {
                    "Cannot delete images from a non-pending claim"
                });

            var image = claim.Images.FirstOrDefault(i => i.Id == request.ImageId && !i.IsDeleted);
            if (image == null)
                throw new NotFoundException("Claim image", request.ImageId);

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var publicId = FileValidationHelper.ExtractPublicId(image.ImageUrl);
                if (!string.IsNullOrEmpty(publicId))
                    await _fileStorage.DeleteAsync(publicId, ct);
            }

            image.IsDeleted = true;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Image {ImageId} deleted from claim {ClaimId}", request.ImageId, request.ClaimId);

            return Unit.Value;
        }
    }
}
