using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Claims.Commands.UploadClaimImages
{
    public class UploadClaimImagesCommandHandler : IRequestHandler<UploadClaimImagesCommand, List<ClaimImageDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IClaimBackgroundJobService _backgroundJobService;
        private readonly ILogger<UploadClaimImagesCommandHandler> _logger;

        public UploadClaimImagesCommandHandler(
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            IClaimBackgroundJobService backgroundJobService,
            ILogger<UploadClaimImagesCommandHandler> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public async Task<List<ClaimImageDto>> Handle(UploadClaimImagesCommand request, CancellationToken ct)
        {
            var claim = await _context.Claims
                .Include(c => c.Images)
                .Include(c => c.Policy).ThenInclude(p => p!.Farm)
                .FirstOrDefaultAsync(c => c.Id == request.ClaimId
                    && c.UserId == request.UserId
                    && !c.IsDeleted, ct);

            if (claim == null)
                throw new NotFoundException(nameof(Claim), request.ClaimId);

            if (claim.Status != ClaimStatus.Pending)
                throw new ValidationException(new List<string>
                {
                    $"Cannot upload images to claim with status: {claim.Status}"
                });

            FileValidationHelper.ValidateBatchLimits(claim.Images.Count, request.Images.Count);

            var imageUrls = new List<string>();
            var uploadedImages = new List<ClaimImageDto>();

            for (int i = 0; i < request.Images.Count; i++)
            {
                var file = request.Images[i];
                await FileValidationHelper.ValidateFileAsync(file, ct);

                var folder = $"claims/{request.ClaimId}";
                var uploadResult = await _fileStorage.UploadAsync(file.Content, file.FileName, folder, ct);

                var claimImage = new ClaimImage
                {
                    Id = Guid.NewGuid(),
                    ClaimId = request.ClaimId,
                    ImageUrl = uploadResult.Url,
                    FileName = uploadResult.FileName,
                    FileType = uploadResult.FileType,
                    FileSizeBytes = uploadResult.FileSizeBytes,
                    DisplayOrder = claim.Images.Count + i,
                    IsPrimary = claim.Images.Count == 0 && i == 0
                };

                _context.ClaimImages.Add(claimImage);
                await _context.SaveChangesAsync(ct);

                imageUrls.Add(uploadResult.Url);
                claim.Images.Add(claimImage);

                uploadedImages.Add(new ClaimImageDto
                {
                    Id = claimImage.Id,
                    ImageUrl = claimImage.ImageUrl,
                    ThumbnailUrl = claimImage.ThumbnailUrl,
                    FileName = claimImage.FileName,
                    FileType = claimImage.FileType,
                    FileSizeBytes = claimImage.FileSizeBytes,
                    DisplayOrder = claimImage.DisplayOrder,
                    IsPrimary = claimImage.IsPrimary
                });

                _logger.LogInformation("Image saved for claim {ClaimId}: {Url}", request.ClaimId, uploadResult.Url);
            }

            if (imageUrls.Count > 0)
            {
                // Always re-run AI analysis so newly uploaded images are included
                // The background job fetches ALL claim images from DB, so pass claimId only
                claim.AIAnalysisStatus = "Pending";
                await _context.SaveChangesAsync(ct);
                _backgroundJobService.EnqueueAIAnalysis(request.ClaimId);
            }

            return uploadedImages;
        }
    }
}
