using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Claims.Commands.CreateClaim;
using FarmClaim.Application.Features.Claims.Commands.DeleteClaim;
using FarmClaim.Application.Features.Claims.Commands.UpdateClaim;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Application.Features.Claims.Queries.GetClaimById;
using FarmClaim.Application.Features.Claims.Queries.GetMyClaims;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class ClaimsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IGeminiVisionService _geminiService;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(
            IMediator mediator,
            IApplicationDbContext context,
            IFileStorageService fileStorage,
            IGeminiVisionService geminiService,
            ILogger<ClaimsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================
        // POST /api/v1/Claims
        // ============================================
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                var command = new CreateClaimCommand(userId, request);
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetById), new { claimId = result.Id }, result);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // ============================================
        // POST /api/v1/Claims/{claimId}/images
        // ============================================
        [HttpPost("{claimId}/images")]
        [Authorize(Roles = "Farmer")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> UploadImages(
            Guid claimId,
            [FromForm] IFormFileCollection images,
            [FromForm] string? cropType = null)
        {
            try
            {
                var userId = GetUserId();

                var claim = await _context.Claims
                    .Include(c => c.Images)
                    .Include(c => c.Policy).ThenInclude(p => p!.Farm)
                    .FirstOrDefaultAsync(c => c.Id == claimId
                        && c.UserId == userId
                        && !c.IsDeleted);

                if (claim == null)
                    return NotFound(new { error = $"Claim {claimId} not found" });

                if (claim.Status != ClaimStatus.Pending)
                    return BadRequest(new { error = $"Cannot upload images to claim with status: {claim.Status}" });

                if (claim.Images.Count + images.Count > 10)
                    return BadRequest(new { error = "Maximum 10 images per claim" });

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                var imageUrls = new List<string>();

                for (int i = 0; i < images.Count; i++)
                {
                    var file = images[i];

                    if (file.Length == 0)
                        return BadRequest(new { error = $"File {file.FileName} is empty" });

                    if (!allowedTypes.Contains(file.ContentType))
                        return BadRequest(new { error = $"File {file.FileName}: only jpg, png, webp allowed" });

                    if (file.Length > 10 * 1024 * 1024)
                        return BadRequest(new { error = $"File {file.FileName} exceeds 10MB" });

                    // 1. Upload to Cloudinary
                    var folder = $"claims/{claimId}";
                    var uploadResult = await _fileStorage.UploadAsync(file.OpenReadStream(), file.FileName, folder);

                    // 2. Save to DB
                    var claimImage = new ClaimImage
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId,
                        ImageUrl = uploadResult.Url,
                        FileName = uploadResult.FileName,
                        FileType = uploadResult.FileType,
                        FileSizeBytes = uploadResult.FileSizeBytes,
                        DisplayOrder = claim.Images.Count + i,
                        IsPrimary = claim.Images.Count == 0 && i == 0
                    };

                    _context.ClaimImages.Add(claimImage);
                    await _context.SaveChangesAsync();

                    imageUrls.Add(uploadResult.Url);
                    claim.Images.Add(claimImage);

                    _logger.LogInformation("Image saved: {Url}", uploadResult.Url);
                }

                // 3. Run Gemini AI in background
                if (imageUrls.Count > 0)
                {
                    var claimIdCopy = claimId;
                    var cropTypeCopy = cropType ?? claim.Policy?.Farm?.CropType ?? "unknown";
                    var urlsCopy = imageUrls.ToList();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = HttpContext.RequestServices.CreateScope();
                            var gemini = scope.ServiceProvider.GetRequiredService<IGeminiVisionService>();
                            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                            var aiResult = await gemini.AnalyzeImagesAsync(urlsCopy, cropTypeCopy);
                            var existingClaim = await db.Claims.FirstOrDefaultAsync(c => c.Id == claimIdCopy);
                            if (existingClaim != null)
                            {
                                existingClaim.AIAnalysisResult = System.Text.Json.JsonSerializer.Serialize(aiResult,
                                    new System.Text.Json.JsonSerializerOptions
                                    {
                                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                                    });
                                await db.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "AI analysis failed for claim {ClaimId}", claimIdCopy);
                        }
                    });
                }

                var uploadedImages = claim.Images
                    .Where(img => imageUrls.Contains(img.ImageUrl))
                    .Select(img => new ClaimImageDto
                    {
                        Id = img.Id,
                        ImageUrl = img.ImageUrl,
                        ThumbnailUrl = img.ThumbnailUrl,
                        FileName = img.FileName,
                        FileType = img.FileType,
                        FileSizeBytes = img.FileSizeBytes,
                        DisplayOrder = img.DisplayOrder,
                        IsPrimary = img.IsPrimary
                    }).ToList();

                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = $"{images.Count} image(s) uploaded successfully",
                    images = uploadedImages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed for claim {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        // ============================================
        // DELETE /api/v1/Claims/{claimId}/images/{imageId}
        // ============================================
        [HttpDelete("{claimId}/images/{imageId}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> DeleteImage(Guid claimId, Guid imageId)
        {
            try
            {
                var userId = GetUserId();

                var claim = await _context.Claims
                    .Include(c => c.Images)
                    .FirstOrDefaultAsync(c => c.Id == claimId
                        && c.UserId == userId
                        && !c.IsDeleted);

                if (claim == null)
                    return NotFound(new { error = "Claim not found" });

                if (claim.Status != ClaimStatus.Pending)
                    return BadRequest(new { error = "Cannot delete images from a non-pending claim" });

                var image = claim.Images.FirstOrDefault(i => i.Id == imageId);
                if (image == null)
                    return NotFound(new { error = "Image not found" });

                // Delete from Cloudinary
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var publicId = ExtractPublicId(image.ImageUrl);
                    if (!string.IsNullOrEmpty(publicId))
                        await _fileStorage.DeleteAsync(publicId);
                }

                image.IsDeleted = true;
                claim.Images.Remove(image);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // ============================================
        // GET /api/v1/Claims
        // ============================================
        [HttpGet]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(PagedResult<ClaimListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyClaims(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var userId = GetUserId();
                var query = new GetMyClaimsQuery(userId, pageNumber, pageSize, status, searchTerm);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // ============================================
        // GET /api/v1/Claims/{claimId}
        // ============================================
        [HttpGet("{claimId}")]
        [Authorize(Roles = "Farmer,Admin")]
        [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid claimId)
        {
            try
            {
                var userId = GetUserId();
                var query = new GetClaimByIdQuery(claimId, userId);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ============================================
        // PUT /api/v1/Claims/{claimId}
        // ============================================
        [HttpPut("{claimId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateClaim(Guid claimId, [FromBody] UpdateClaimRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                var command = new UpdateClaimCommand(claimId, userId, request);
                var result = await _mediator.Send(command);
                return Ok(new { message = "Claim updated successfully", claim = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ============================================
        // DELETE /api/v1/Claims/{claimId}
        // ============================================
        [HttpDelete("{claimId}")]
        [Authorize(Roles = "Farmer")]
        public async Task<IActionResult> DeleteClaim(Guid claimId)
        {
            try
            {
                var userId = GetUserId();
                var command = new DeleteClaimCommand(claimId, userId);
                await _mediator.Send(command);
                return Ok(new { message = "Claim deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ============================================
        // HELPERS
        // ============================================
        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(claim, out var id))
                throw new UnauthorizedException("Invalid user identity");
            return id;
        }

        private IActionResult HandleError(Exception ex)
        {
            switch (ex)
            {
                case NotFoundException _:
                    return StatusCode(StatusCodes.Status404NotFound, new { error = ex.Message });
                case UnauthorizedAccessException _:
                    return StatusCode(StatusCodes.Status401Unauthorized, new { error = "Access denied" });
                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { error = "An unexpected error occurred", detail = ex.Message });
            }
        }

        private static string? ExtractPublicId(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex < 0 || uploadIndex >= segments.Length - 1)
                    return null;

                var publicIdWithExt = string.Join('/', segments.Skip(uploadIndex + 1));
                var dotIndex = publicIdWithExt.LastIndexOf('.');
                return dotIndex > 0 ? publicIdWithExt.Substring(0, dotIndex) : publicIdWithExt;
            }
            catch
            {
                return null;
            }
        }
    }
}