using FarmClaim.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    public abstract class AdminBaseController : ControllerBase
    {
        private readonly ILogger<AdminBaseController> _logger;

        protected AdminBaseController(ILogger<AdminBaseController> logger)
        {
            _logger = logger;
        }

        protected Guid GetAdminId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid admin identity.");
            return id;
        }

        protected string GetAdminEmail()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return string.IsNullOrWhiteSpace(email) ? "admin" : email;
        }

        protected IActionResult HandleError(Exception ex)
        {
            switch (ex)
            {
                case NotFoundException _:
                    return StatusCode(StatusCodes.Status404NotFound, new { error = "Resource not found" });
                case ForbiddenException _:
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "Access denied" });
                default:
                    _logger.LogError(ex, "Unexpected error in admin operation");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred" });
            }
        }
    }
}