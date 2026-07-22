using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.InsurancePlans.Queries.GetAllPlans;
using FarmClaim.Application.Features.InsurancePlans.Queries.GetPlanById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
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
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? cropType = null)
        {
            var result = await _mediator.Send(new GetAllPlansQuery(
                pageNumber, pageSize, searchTerm, cropType, null, AdminContext: false));
            return Ok(result);
        }

        [HttpGet("{planId}")]
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