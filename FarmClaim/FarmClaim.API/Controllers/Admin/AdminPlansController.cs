using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.InsurancePlans.Commands.CreatePlan;
using FarmClaim.Application.Features.InsurancePlans.Commands.DeletePlan;
using FarmClaim.Application.Features.InsurancePlans.Commands.TogglePlanStatus;
using FarmClaim.Application.Features.InsurancePlans.Commands.UpdatePlan;
using FarmClaim.Application.Features.InsurancePlans.DTOs;
using FarmClaim.Application.Features.InsurancePlans.Queries.GetAllPlans;
using FarmClaim.Application.Features.InsurancePlans.Queries.GetPlanById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
    [Route("api/v1/Admin/Plans")]
    public class AdminPlansController : AdminBaseController
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminPlansController> _logger;

        public AdminPlansController(IMediator mediator, ILogger<AdminPlansController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/v1/Admin/Plans
        [HttpGet]
        public async Task<IActionResult> GetAllPlans(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? cropType = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var result = await _mediator.Send(new GetAllPlansQuery(
                    pageNumber, pageSize, searchTerm, cropType, isActive, AdminContext: true));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list insurance plans");
                return HandleError(ex);
            }
        }

        // GET /api/v1/Admin/Plans/{planId}
        [HttpGet("{planId}")]
        public async Task<IActionResult> GetPlanById(Guid planId)
        {
            try
            {
                var result = await _mediator.Send(new GetPlanByIdQuery(planId, AdminContext: true));
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get plan {PlanId}", planId);
                return HandleError(ex);
            }
        }

        // POST /api/v1/Admin/Plans
        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new CreatePlanCommand(adminId, request));
                return CreatedAtAction(nameof(GetPlanById), new { planId = result.Id }, result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create insurance plan");
                return HandleError(ex);
            }
        }

        // PUT /api/v1/Admin/Plans/{planId}
        [HttpPut("{planId}")]
        public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new UpdatePlanCommand(planId, adminId, request));
                return Ok(new { message = "Plan updated successfully", plan = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update plan {PlanId}", planId);
                return HandleError(ex);
            }
        }

        // DELETE /api/v1/Admin/Plans/{planId}
        [HttpDelete("{planId}")]
        public async Task<IActionResult> DeletePlan(Guid planId)
        {
            try
            {
                var adminId = GetAdminId();
                await _mediator.Send(new DeletePlanCommand(planId, adminId));
                return Ok(new { message = "Plan deleted successfully" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete plan {PlanId}", planId);
                return HandleError(ex);
            }
        }

        // PATCH /api/v1/Admin/Plans/{planId}/activate
        [HttpPatch("{planId}/activate")]
        public async Task<IActionResult> ActivatePlan(Guid planId)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new TogglePlanStatusCommand(planId, adminId, Activate: true));
                return Ok(new { message = "Plan activated successfully", plan = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate plan {PlanId}", planId);
                return HandleError(ex);
            }
        }

        // PATCH /api/v1/Admin/Plans/{planId}/deactivate
        [HttpPatch("{planId}/deactivate")]
        public async Task<IActionResult> DeactivatePlan(Guid planId)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new TogglePlanStatusCommand(planId, adminId, Activate: false));
                return Ok(new { message = "Plan deactivated successfully", plan = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate plan {PlanId}", planId);
                return HandleError(ex);
            }
        }
    }
}