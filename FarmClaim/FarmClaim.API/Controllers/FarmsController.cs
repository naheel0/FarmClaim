using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Farms.Commands.CreateFarm;
using FarmClaim.Application.Features.Farms.Commands.DeleteFarm;
using FarmClaim.Application.Features.Farms.Commands.UpdateFarm;
using FarmClaim.Application.Features.Farms.DTOs;
using FarmClaim.Application.Features.Farms.Queries.GetFarmById;
using FarmClaim.Application.Features.Farms.Queries.GetMyFarms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class FarmsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FarmsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        // ==========================================
        // CREATE FARM
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(FarmResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateFarm([FromBody] CreateFarmRequestDto request)
        {
            var userId = GetUserId();  // Helper method
            try
            {
                var command = new CreateFarmCommand(userId, request);
                var result = await _mediator.Send(command);

                return CreatedAtAction(nameof(GetById), new { farmId = result.Id }, result);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // ==========================================
        // LIST FARMS (PAGINATED)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(PagedResult<FarmListDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyFarms(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null)
        {
            var query = new GetMyFarmsQuery(GetUserId(), pageNumber, pageSize, searchTerm);

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        // ==========================================
        // GET BY ID
        // ==========================================
        [HttpGet("{farmId}")]
        [Authorize(Roles = "Farmer,Admin")]
        [ProducesResponseType(typeof(FarmResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid farmId)
        {
            try
            {
                var query = new GetFarmByIdQuery(farmId, GetUserId());
                var result = await _mediator.Send(query);

                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = $"Farm not found. Details: {ex.Message}" });
            }
        }

        // ==========================================
        // UPDATE FARM
        // ==========================================
        [HttpPut("{farmId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateFarm(Guid farmId, [FromBody] UpdateFarmRequestDto request)
        {
            try
            {
                var command = new UpdateFarmCommand(farmId, GetUserId(), request);
                var result = await _mediator.Send(command);

                return Ok(new { message = "Farm updated successfully", farm = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ==========================================
        // DELETE FARM
        // ==========================================
        [HttpDelete("{farmId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteFarm(Guid farmId)
        {
            try
            {
                var command = new DeleteFarmCommand(farmId, GetUserId());
                await _mediator.Send(command);

                return Ok(new { message = "Farm deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ==========================================
        // SET LOCATION (GPS)
        // ==========================================
        [HttpPost("{farmId}/location")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(FarmResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetLocation(Guid farmId, [FromBody] LocationRequestDto request)
        {
            var updateReq = new UpdateFarmRequestDto
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LocationGeoJson = request.GeoJson
            };

            try
            {
                var command = new UpdateFarmCommand(farmId, GetUserId(), updateReq);
                var result = await _mediator.Send(command);

                return Ok(new { message = "Location updated successfully", farm = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

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
                    return StatusCode(StatusCodes.Status404NotFound,
                        new { error = ex.Message });

                case UnauthorizedAccessException _:
                    return StatusCode(StatusCodes.Status401Unauthorized,
                        new { error = "Access denied" });

                default:
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        new { error = "An unexpected error occurred" });
            }
        }
    }
}