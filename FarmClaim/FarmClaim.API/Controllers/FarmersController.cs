using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Farmers.Commands.UpdateProfile;
using FarmClaim.Application.Features.Farmers.DTOs;
using FarmClaim.Application.Features.Farmers.Queries.GetCurrentUserProfile;
using FarmClaim.Application.Features.Farmers.Queries.GetAllFarmers;
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
    public class FarmersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FarmersController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpGet("me")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(FarmerProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user identity" });
            }

            try
            {
                var query = new GetCurrentUserProfileQuery(userId);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("me")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(FarmerProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Invalid user identity" });
            }

            try
            {
                var command = new UpdateProfileCommand(userId, request);
                var result = await _mediator.Send(command);
                return Ok(new { message = "Profile updated successfully", profile = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{farmerId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FarmerProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFarmerById(Guid farmerId)
        {
            try
            {
                var query = new GetCurrentUserProfileQuery(farmerId);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // FIXED: Now actually implemented
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<FarmerListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllFarmers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);
            var query = new GetAllFarmersQuery(pageNumber, pageSize, searchTerm);
            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}