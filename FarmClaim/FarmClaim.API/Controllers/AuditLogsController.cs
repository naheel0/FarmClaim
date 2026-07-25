using FarmClaim.Application.Features.AuditLogs.Queries.GetAuditLogs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/Admin/AuditLogs")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(IMediator mediator, ILogger<AuditLogsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // GET /api/v1/Admin/AuditLogs
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var result = await _mediator.Send(new GetAuditLogsQuery(
                    pageNumber, pageSize, userId, entityType, action, fromDate, toDate, searchTerm));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load audit logs");
                return StatusCode(500, new { error = "Failed to load audit logs" });
            }
        }
    }
}