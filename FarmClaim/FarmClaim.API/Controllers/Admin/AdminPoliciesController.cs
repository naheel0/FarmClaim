using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Admin.Commands.ApprovePolicy;
using FarmClaim.Application.Features.Admin.Commands.RejectPolicy;
using FarmClaim.Application.Features.Admin.DTOs;
using FarmClaim.Application.Features.Notifications.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmClaim.API.Controllers
{
    [Route("api/v1/Admin/Policies")]
    public class AdminPoliciesController : AdminBaseController
    {
        private readonly IApplicationDbContext _context;
        private readonly IMediator _mediator;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AdminPoliciesController> _logger;

        public AdminPoliciesController(
            IApplicationDbContext context,
            IMediator mediator,
            INotificationService notificationService,
            ILogger<AdminPoliciesController> logger)
            : base(logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // PUT /api/v1/Admin/Policies/{policyId}/approve
        [HttpPut("{policyId}/approve")]
        public async Task<IActionResult> ApprovePolicy(Guid policyId)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new ApprovePolicyCommand(policyId, adminId));

                var policy = await _context.InsurancePolicies
                    .Include(p => p.Farm)
                    .FirstOrDefaultAsync(p => p.Id == policyId);
                if (policy?.Farm != null)
                {
                    await _notificationService.SendClaimUpdateAsync(policy.Farm.UserId, new ClaimNotificationDto
                    {
                        Title = "Policy Approved",
                        Message = $"Your policy {policy.PolicyNumber} has been approved and is now active.",
                        NotificationType = "PolicyStatusChanged"
                    });
                }

                return Ok(new { message = "Policy approved successfully.", policy = result });
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
                _logger.LogError(ex, "Failed to approve policy {PolicyId}", policyId);
                return HandleError(ex);
            }
        }

        // PUT /api/v1/Admin/Policies/{policyId}/reject
        [HttpPut("{policyId}/reject")]
        public async Task<IActionResult> RejectPolicy(
            Guid policyId, [FromBody] RejectPolicyRequestDto request)
        {
            try
            {
                var adminId = GetAdminId();
                var result = await _mediator.Send(new RejectPolicyCommand(policyId, adminId, request));

                var policy = await _context.InsurancePolicies
                    .Include(p => p.Farm)
                    .FirstOrDefaultAsync(p => p.Id == policyId);
                if (policy?.Farm != null)
                {
                    await _notificationService.SendClaimUpdateAsync(policy.Farm.UserId, new ClaimNotificationDto
                    {
                        Title = "Policy Rejected",
                        Message = $"Your policy {policy.PolicyNumber} has been rejected. Reason: {request.Reason}",
                        NotificationType = "PolicyStatusChanged"
                    });
                }

                return Ok(new { message = "Policy rejected.", policy = result });
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
                _logger.LogError(ex, "Failed to reject policy {PolicyId}", policyId);
                return HandleError(ex);
            }
        }
    }
}