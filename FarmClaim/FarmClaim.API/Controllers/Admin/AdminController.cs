using FarmClaim.Application.Features.Admin.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
    [Route("api/v1/Admin")]
    public class AdminController : AdminBaseController
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IMediator mediator, ILogger<AdminController> logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/v1/Admin/Dashboard
        [HttpGet("Dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var result = await _mediator.Send(new GetDashboardStatsQuery());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard stats");
                return StatusCode(500, new { error = "Failed to load dashboard" });
            }
        }
    }
}