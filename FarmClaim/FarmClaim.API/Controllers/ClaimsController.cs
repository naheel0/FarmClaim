using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Claims.Commands.CreateClaim;
using FarmClaim.Application.Features.Claims.Commands.DeleteClaim;
using FarmClaim.Application.Features.Claims.Commands.UpdateClaim;
using FarmClaim.Application.Features.Claims.DTOs;
using FarmClaim.Application.Features.Claims.Queries.GetClaimById;
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
    public class ClaimsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ClaimsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpPost]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateClaim([FromBody] CreateClaimRequestDto request)
        {
            var userId = GetUserId();
            try
            {
                var command = new CreateClaimCommand(userId, request);
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetById), new { claimId = result.Id }, result);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(PagedResult<ClaimListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyClaims(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null)
        {
            var query = new GetMyClaimsQuery(GetUserId(), pageNumber, pageSize, status, searchTerm);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{claimId}")]
        [Authorize(Roles = "Farmer,Admin")]
        [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid claimId)
        {
            try
            {
                var query = new GetClaimByIdQuery(claimId, GetUserId());
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = $"Claim not found. Details: {ex.Message}" });
            }
        }

        [HttpPut("{claimId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateClaim(Guid claimId, [FromBody] UpdateClaimRequestDto request)
        {
            try
            {
                var command = new UpdateClaimCommand(claimId, GetUserId(), request);
                var result = await _mediator.Send(command);
                return Ok(new { message = "Claim updated successfully", claim = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{claimId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteClaim(Guid claimId)
        {
            try
            {
                var command = new DeleteClaimCommand(claimId, GetUserId());
                await _mediator.Send(command);
                return Ok(new { message = "Claim deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

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
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }
    }
}