using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.InsurancePlans.Queries.GetAllPlans;
using FarmClaim.Application.Features.InsurancePlans.Queries.GetPlanById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
    /// <summary>
    /// Public (read-only) insurance plan catalogue.
    /// Anonymous farmers must be able to browse plans before signing up,
    /// so the two GET endpoints allow anonymous access. GetAllPlansQuery/
    /// GetPlanByIdQuery receive AdminContext:false and will only return Active plans.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class InsurancePlansController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InsurancePlansController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpGet]
        [AllowAnonymous] // anonymous visitors browse plans before signup — handler only returns Active plans (AdminContext:false)
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? cropType = null)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);
            var result = await _mediator.Send(new GetAllPlansQuery(
                pageNumber, pageSize, searchTerm, cropType, null, AdminContext: false));
            return Ok(result);
        }

        [HttpGet("{planId}")]
        [AllowAnonymous] // anonymous visitors view plan details before signup — handler only returns Active plan (AdminContext:false)
        public async Task<IActionResult> GetById(Guid planId)
        {
            try
            {
                var result = await _mediator.Send(new GetPlanByIdQuery(planId, AdminContext: false));
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}