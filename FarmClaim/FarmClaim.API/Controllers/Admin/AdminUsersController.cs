using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Admin.Commands.ActivateUser;
using FarmClaim.Application.Features.Admin.Commands.BlockUser;
using FarmClaim.Application.Features.Admin.Commands.SuspendUser;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Application.Features.Admin.Queries.GetAllUsers;
using FarmClaim.Application.Features.Admin.Queries.GetUserById;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
    [Route("api/v1/Admin/Users")]
    public class AdminUsersController : AdminBaseController
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(IMediator mediator, ILogger<AdminUsersController> logger)
            : base(logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/v1/Admin/Users?pageNumber=1&pageSize=20&searchTerm=&role=Farmer&status=Active&sortBy=CreatedAt&sortOrder=desc
        [HttpGet]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] UserStatus? status = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                pageNumber = Math.Max(1, pageNumber);

                var result = await _mediator.Send(new GetAllUsersQuery(
                    pageNumber, pageSize, searchTerm, role, status, sortBy, sortOrder));
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list users");
                return HandleError(ex);
            }
        }

        // GET /api/v1/Admin/Users/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            try
            {
                var result = await _mediator.Send(new GetUserByIdQuery(userId));
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user {UserId}", userId);
                return HandleError(ex);
            }
        }

        // PATCH /api/v1/Admin/Users/{userId}/suspend
        [HttpPatch("{userId}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid userId, [FromBody] UserActionRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new SuspendUserCommand(userId, adminId, request));
                return Ok(new { message = "User suspended successfully", action = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to suspend user {UserId}", userId);
                return HandleError(ex);
            }
        }

        // PATCH /api/v1/Admin/Users/{userId}/activate
        [HttpPatch("{userId}/activate")]
        public async Task<IActionResult> ActivateUser(Guid userId, [FromBody] UserActionRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new ActivateUserCommand(userId, adminId, request));
                return Ok(new { message = "User activated successfully", action = result });
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
                _logger.LogError(ex, "Failed to activate user {UserId}", userId);
                return HandleError(ex);
            }
        }

        // PATCH /api/v1/Admin/Users/{userId}/block
        [HttpPatch("{userId}/block")]
        public async Task<IActionResult> BlockUser(Guid userId, [FromBody] UserActionRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new BlockUserCommand(userId, adminId, request));
                return Ok(new { message = "User blocked permanently", action = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to block user {UserId}", userId);
                return HandleError(ex);
            }
        }

    }
}