using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    public abstract class AdminBaseController : ControllerBase
    {
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
            return StatusCode(500, new { error = "An unexpected error occurred", detail = ex.Message });
        }
    }
}