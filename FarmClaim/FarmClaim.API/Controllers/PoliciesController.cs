using FarmClaim.Application.Common.DTOs;
using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.InsurancePolicies.Commands.CreatePolicy;
using FarmClaim.Application.Features.InsurancePolicies.Commands.DeletePolicy;
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
    public class PoliciesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PoliciesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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

            var statusFilter = !string.IsNullOrWhiteSpace(status)

                ? Enum.Parse<FarmClaim.Domain.Enums.PolicyStatus>(status, true)

                : (FarmClaim.Domain.Enums.PolicyStatus?)null;



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
                var query = new GetPolicyByIdQuery(policyId, GetUserId());
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = $"Policy not found. Details: {ex.Message}" });
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
                return Ok(new { message = "Policy updated successfully", policy = result });
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
                        new { error = ex.Message, type = ex.GetType().Name });
            }
        }
    }
}