using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.InsurancePolicies.Commands.CreatePolicy;
using FarmClaim.Application.Features.InsurancePolicies.Commands.DeletePolicy;
using FarmClaim.Application.Features.InsurancePolicies.Commands.RenewPolicy;
using FarmClaim.Application.Features.InsurancePolicies.Commands.UpdatePolicy;
using FarmClaim.Application.Features.InsurancePolicies.DTOs;
using FarmClaim.Application.Features.InsurancePolicies.Queries.GetMyPolicies;
using FarmClaim.Application.Features.InsurancePolicies.Queries.GetPolicyById;
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
    public class PoliciesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PoliciesController> _logger;

        public PoliciesController(IMediator mediator, ILogger<PoliciesController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(PolicyResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                var command = new CreatePolicyCommand(userId, request);
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetById), new { policyId = result.Id }, result);
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        [HttpGet]

        [Authorize(Roles = "Farmer")]

        [ProducesResponseType(typeof(PagedResult<PolicyListDto>), StatusCodes.Status200OK)]

        public async Task<IActionResult> GetMyPolicies(

            [FromQuery] int pageNumber = 1,

            [FromQuery] int pageSize = 20,

            [FromQuery] string? searchTerm = null,

            [FromQuery] string? status = null)

        {

            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);

            FarmClaim.Domain.Enums.PolicyStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<FarmClaim.Domain.Enums.PolicyStatus>(status, true, out var parsed))
                    statusFilter = parsed;
                else
                    return BadRequest(new { error = $"Invalid status value: {status}" });
            }



            return Ok(await _mediator.Send(new GetMyPoliciesQuery(

                GetUserId(), pageNumber, pageSize, searchTerm, statusFilter)));

        }

        [HttpGet("{policyId}")]
        [Authorize(Roles = "Farmer,Admin")]
        [ProducesResponseType(typeof(PolicyResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid policyId)
        {
            try
            {
                var userId = GetUserId();
                var role = User.IsInRole("Admin") ? FarmClaim.Domain.Enums.UserRole.Admin : FarmClaim.Domain.Enums.UserRole.Farmer;
                var query = new GetPolicyByIdQuery(policyId, userId, role);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException)
            {
                return NotFound(new { error = "Policy not found" });
            }
        }

        [HttpPut("{policyId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(PolicyResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdatePolicy(Guid policyId, [FromBody] UpdatePolicyRequestDto request)
        {
            try
            {
                var command = new UpdatePolicyCommand(policyId, GetUserId(), request);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{policyId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeletePolicy(Guid policyId)
        {
            try
            {
                var command = new DeletePolicyCommand(policyId, GetUserId());
                await _mediator.Send(command);
                return Ok(new { message = "Policy deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ============================================
        // POST /api/v1/Policies/{policyId}/renew
        // ============================================
        [HttpPost("{policyId}/renew")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(PolicyResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> RenewPolicy(Guid policyId, [FromQuery] DateTime? startDate)
        {
            try
            {
                var userId = GetUserId();
                var command = new RenewPolicyCommand(policyId, userId, startDate);
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetById), new { policyId = result.Id }, result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
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
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "Resource not found" });
                case UnauthorizedException _:
                    return StatusCode(StatusCodes.Status401Unauthorized, new { error = "Access denied" });
                default:
                    _logger.LogError(ex, "Unexpected error in PoliciesController");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }
    }
}