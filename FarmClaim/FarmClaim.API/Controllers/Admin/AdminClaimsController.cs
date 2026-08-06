using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.Commands.ApproveClaim;
using FarmClaim.Application.Features.Admin.Commands.PayClaim;
using FarmClaim.Application.Features.Admin.Commands.RejectClaim;
using FarmClaim.Application.Features.Admin.Commands.SetUnderReview;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Application.Features.Admin.Queries.GetAllClaims;
using FarmClaim.Application.Features.Admin.Queries.GetClaimDetail;
using FarmClaim.Application.Features.Notifications.DTOs;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FarmClaim.API.Controllers
{
    [Route("api/v1/Admin/Claims")]
    public class AdminClaimsController : AdminBaseController
    {
        private readonly IMediator _mediator;
        private readonly INotificationService _notificationService;
        private readonly IClaimBackgroundJobService _backgroundJobService;
        private readonly ILogger<AdminClaimsController> _logger;

        public AdminClaimsController(
            IMediator mediator,
            IApplicationDbContext context,
            INotificationService notificationService,
            IClaimBackgroundJobService backgroundJobService,
            ILogger<AdminClaimsController> logger)
            : base(logger)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/v1/Admin/Claims
        [HttpGet]
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
                pageSize = Math.Clamp(pageSize, 1, 100);
                pageNumber = Math.Max(1, pageNumber);
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

        // GET /api/v1/Admin/Claims/{claimId}
        [HttpGet("{claimId}")]
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

        // PUT /api/v1/Admin/Claims/{claimId}/review
        [HttpPut("{claimId}/review")]
        public async Task<IActionResult> SetUnderReview(Guid claimId)
        {
            try
            {
                var adminId = GetAdminId();
                var adminEmail = GetAdminEmail();

                await _mediator.Send(new SetUnderReviewCommand(claimId, adminId, adminEmail));

                var userId = await GetClaimUserId(claimId);
                if (userId.HasValue)
                {
                    await _notificationService.SendClaimUpdateAsync(userId.Value, new ClaimNotificationDto
                    {
                        ClaimId = claimId,
                        Status = ClaimStatus.UnderReview,
                        Title = "Claim Under Review",
                        Message = "Your claim is now being reviewed by our team.",
                        NotificationType = "StatusChanged"
                    });
                }

                return Ok(new { message = "Claim set to under review", claimId });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Errors.FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        // PUT /api/v1/Admin/Claims/{claimId}/approve
        [HttpPut("{claimId}/approve")]
        public async Task<IActionResult> ApproveClaim(Guid claimId, [FromBody] ApproveClaimRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var adminEmail = GetAdminEmail();

                await _mediator.Send(new ApproveClaimCommand(claimId, adminId, adminEmail, request));

                var userId = await GetClaimUserId(claimId);
                if (userId.HasValue)
                {
                    await _notificationService.SendClaimUpdateAsync(userId.Value, new ClaimNotificationDto
                    {
                        ClaimId = claimId,
                        Status = ClaimStatus.Approved,
                        Title = "Claim Approved",
                        Message = $"Your claim has been approved. Payout: {request.ApprovedAmount:C}",
                        NotificationType = "StatusChanged",
                        ApprovedAmount = request.ApprovedAmount
                    });
                }

                return Ok(new
                {
                    message = "Claim approved successfully",
                    claimId,
                    approvedAmount = request.ApprovedAmount
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Errors.FirstOrDefault() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve claim {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        // PUT /api/v1/Admin/Claims/{claimId}/reject
        [HttpPut("{claimId}/reject")]
        public async Task<IActionResult> RejectClaim(Guid claimId, [FromBody] RejectClaimRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var adminEmail = GetAdminEmail();

                await _mediator.Send(new RejectClaimCommand(claimId, adminId, adminEmail, request));

                var userId = await GetClaimUserId(claimId);
                if (userId.HasValue)
                {
                    await _notificationService.SendClaimUpdateAsync(userId.Value, new ClaimNotificationDto
                    {
                        ClaimId = claimId,
                        Status = ClaimStatus.Rejected,
                        Title = "Claim Rejected",
                        Message = $"Your claim has been rejected. Reason: {request.RejectionReason}",
                        NotificationType = "StatusChanged",
                        RejectionReason = request.RejectionReason
                    });
                }

                return Ok(new
                {
                    message = "Claim rejected successfully",
                    claimId,
                    rejectionReason = request.RejectionReason
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Errors.FirstOrDefault() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject claim {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        // PUT /api/v1/Admin/Claims/{claimId}/pay
        [HttpPut("{claimId}/pay")]
        public async Task<IActionResult> PayClaim(Guid claimId, [FromBody] PayClaimRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new PayClaimCommand(claimId, adminId, request));

                var userId = await GetClaimUserId(claimId);
                if (userId.HasValue)
                {
                    var claimResult = result as dynamic;
                    await _notificationService.SendClaimUpdateAsync(userId.Value, new ClaimNotificationDto
                    {
                        ClaimId = claimId,
                        Status = ClaimStatus.Paid,
                        Title = "Claim Paid",
                        Message = $"Your claim payout has been processed.",
                        NotificationType = "StatusChanged"
                    });
                }

                return Ok(new { message = "Claim marked as paid successfully.", claim = result });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pay claim {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        // POST /api/v1/Admin/Claims/{claimId}/reprocess
        // PROD: Re-run weather + AI verification for a claim (e.g. after a farm was
        // geo-tagged or to retry a previously failed external call).
        [HttpPost("{claimId}/reprocess")]
        public IActionResult ReprocessVerification(Guid claimId)
        {
            try
            {
                _backgroundJobService.ReprocessVerification(claimId);
                return Ok(new { message = "Verification reprocessing started", claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reprocess claim verification {ClaimId}", claimId);
                return HandleError(ex);
            }
        }

        private async Task<Guid?> GetClaimUserId(Guid claimId)
        {
            var claim = await _mediator.Send(new GetClaimDetailQuery(claimId));
            return claim?.UserId;
        }
    }
}
