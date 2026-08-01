using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Claims.Commands.CreateClaim;
using FarmClaim.Application.Features.Claims.Commands.DeleteClaim;
using FarmClaim.Application.Features.Claims.Commands.DeleteClaimImage;
using FarmClaim.Application.Features.Claims.Commands.UpdateClaim;
using FarmClaim.Application.Features.Claims.Commands.UploadClaimImages;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Application.Features.Claims.Queries.GetClaimById;
using FarmClaim.Application.Features.Claims.Queries.GetClaimTimeline;
using FarmClaim.Application.Features.Claims.Queries.GetMyClaims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(IMediator mediator, ILogger<ClaimsController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public async Task<IActionResult> UploadImages(
            Guid claimId,
            [FromForm] IFormFileCollection images,
            [FromForm] string? cropType = null)
        {
            try
            {
                var userId = GetUserId();
                var imageFiles = images.Select(f => new UploadClaimImageFile
                {
                    Content = f.OpenReadStream(),
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    Length = f.Length
                }).ToList();
                var command = new UploadClaimImagesCommand(claimId, userId, imageFiles, cropType);
                var result = await _mediator.Send(command);
                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = $"{images.Count} image(s) uploaded successfully",
                    images = result
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
                var command = new DeleteClaimImageCommand(claimId, imageId, userId);
                await _mediator.Send(command);
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
                pageSize = Math.Clamp(pageSize, 1, 100);
                pageNumber = Math.Max(1, pageNumber);
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
        // GET /api/v1/Claims/{claimId}/timeline
        // ============================================
        [HttpGet("{claimId}/timeline")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(List<ClaimTimelineEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClaimTimeline(Guid claimId)
        {
            try
            {
                var userId = GetUserId();
                var query = new GetClaimTimelineQuery(claimId, userId);
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
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "Resource not found" });
                case UnauthorizedException _:
                    return StatusCode(StatusCodes.Status401Unauthorized, new { error = "Access denied" });
                case ValidationException validationEx:
                    return BadRequest(new { errors = validationEx.Errors });
                default:
                    _logger.LogError(ex, "Unexpected error in ClaimsController");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }
    }
}
