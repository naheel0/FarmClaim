using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Application.Features.Admin.Queries.GetAllClaims;
using FarmClaim.Application.Features.Admin.Queries.GetClaimDetail;
using FarmClaim.Application.Features.Admin.Queries.GetDashboardStats;
using FarmClaim.Application.Features.Notifications.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IApplicationDbContext context,
            IMediator mediator,
            INotificationService notificationService,
            ILogger<AdminController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================
        // GET /api/v1/Admin/Dashboard
        // ============================================
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

        // ============================================
        // GET /api/v1/Admin/Claims
        // ============================================
        [HttpGet("Claims")]
        public async Task<IActionResult> GetAllClaims(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? incidentType = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var query = new GetAllClaimsQuery(pageNumber, pageSize, status, incidentType, searchTerm, sortBy, sortOrder);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list claims for admin");
                return StatusCode(500, new { error = "Failed to load claims" });
            }
        }

        // ============================================
        // GET /api/v1/Admin/Claims/{claimId}
        // ============================================
        [HttpGet("Claims/{claimId}")]
        public async Task<IActionResult> GetClaimDetail(Guid claimId)
        {
            try
            {
                var result = await _mediator.Send(new GetClaimDetailQuery(claimId));
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get claim detail {ClaimId}", claimId);
                return StatusCode(500, new { error = "Failed to load claim details" });
            }
        }

        // ============================================
        // PUT /api/v1/Admin/Claims/{claimId}/approve
        // ============================================
        [HttpPut("Claims/{claimId}/approve")]
        public async Task<IActionResult> ApproveClaim(Guid claimId, [FromBody] ApproveClaimRequestDto request)
        {
            try
            {
                var adminEmail = GetAdminEmail();

                var claim = await _context.Claims
                    .Include(c => c.Policy)
                    .FirstOrDefaultAsync(c => c.Id == claimId && !c.IsDeleted);

                if (claim == null)
                    return NotFound(new { error = "Claim not found" });

                if (claim.Status == ClaimStatus.Approved)
                    return BadRequest(new { error = "Claim is already approved" });

                if (claim.Status == ClaimStatus.Rejected)
                    return BadRequest(new { error = "Cannot approve a rejected claim" });

                if (request.ApprovedAmount > (claim.Policy?.SumInsured ?? 0))
                    return BadRequest(new { error = $"Approved amount cannot exceed policy sum insured ({claim.Policy?.SumInsured})" });

                claim.Status = ClaimStatus.Approved;
                claim.ApprovedAmount = request.ApprovedAmount;
                claim.ReviewedBy = adminEmail;
                claim.ReviewedAt = DateTime.UtcNow;
                claim.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim {ClaimId} approved by {Admin} for amount {Amount}",
                    claimId, adminEmail, request.ApprovedAmount);

                // Notify farmer via SignalR
                await _notificationService.SendClaimUpdateAsync(claim.UserId, new ClaimNotificationDto
                {
                    ClaimId = claimId,
                    Status = ClaimStatus.Approved,
                    Title = "Claim Approved",
                    Message = $"Your claim has been approved. Payout: {request.ApprovedAmount:C}",
                    NotificationType = "StatusChanged",
                    ApprovedAmount = request.ApprovedAmount
                });

                return Ok(new
                {
                    message = "Claim approved successfully",
                    claimId = claim.Id,
                    approvedAmount = claim.ApprovedAmount,
                    reviewedBy = claim.ReviewedBy,
                    reviewedAt = claim.ReviewedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve claim {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        // ============================================
        // PUT /api/v1/Admin/Claims/{claimId}/reject
        // ============================================
        [HttpPut("Claims/{claimId}/reject")]
        public async Task<IActionResult> RejectClaim(Guid claimId, [FromBody] RejectClaimRequestDto request)
        {
            try
            {
                var adminEmail = GetAdminEmail();

                var claim = await _context.Claims
                    .FirstOrDefaultAsync(c => c.Id == claimId && !c.IsDeleted);

                if (claim == null)
                    return NotFound(new { error = "Claim not found" });

                if (claim.Status == ClaimStatus.Approved)
                    return BadRequest(new { error = "Cannot reject an approved claim" });

                if (claim.Status == ClaimStatus.Rejected)
                    return BadRequest(new { error = "Claim is already rejected" });

                claim.Status = ClaimStatus.Rejected;
                claim.RejectionReason = request.RejectionReason?.Trim();
                claim.ReviewedBy = adminEmail;
                claim.ReviewedAt = DateTime.UtcNow;
                claim.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Claim {ClaimId} rejected by {Admin}. Reason: {Reason}",
                    claimId, adminEmail, request.RejectionReason);

                // Notify farmer via SignalR
                await _notificationService.SendClaimUpdateAsync(claim.UserId, new ClaimNotificationDto
                {
                    ClaimId = claimId,
                    Status = ClaimStatus.Rejected,
                    Title = "Claim Rejected",
                    Message = $"Your claim has been rejected. Reason: {request.RejectionReason}",
                    NotificationType = "StatusChanged",
                    RejectionReason = request.RejectionReason
                });

                return Ok(new
                {
                    message = "Claim rejected successfully",
                    claimId = claim.Id,
                    status = claim.Status,
                    rejectionReason = claim.RejectionReason,
                    reviewedBy = claim.ReviewedBy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject claim {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        // ============================================
        // PUT /api/v1/Admin/Claims/{claimId}/review
        // ============================================
        [HttpPut("Claims/{claimId}/review")]
        public async Task<IActionResult> SetUnderReview(Guid claimId)
        {
            try
            {
                var adminEmail = GetAdminEmail();

                var claim = await _context.Claims
                    .FirstOrDefaultAsync(c => c.Id == claimId && !c.IsDeleted);

                if (claim == null)
                    return NotFound(new { error = "Claim not found" });

                if (claim.Status != ClaimStatus.Pending)
                    return BadRequest(new { error = $"Only pending claims can be set to review. Current: {claim.Status}" });

                claim.Status = ClaimStatus.UnderReview;
                claim.ReviewedBy = adminEmail;
                claim.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Claim set to under review", claimId = claim.Id });
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // ============================================
        // HELPERS
        // ============================================
        private string GetAdminEmail()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return string.IsNullOrWhiteSpace(email) ? "admin" : email;
        }

        private IActionResult HandleError(Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred", detail = ex.Message });
        }
    }
}