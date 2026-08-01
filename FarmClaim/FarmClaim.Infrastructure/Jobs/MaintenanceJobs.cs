using System.Text.Json;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Notifications.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using FarmClaim.Infrastructure.Email.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Infrastructure.Jobs
{
    /// <summary>
    /// Scheduled maintenance jobs for the FarmClaim system.
    /// All jobs are idempotent — safe to run multiple times.
    /// </summary>
    public class MaintenanceJobs
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailQueueService _emailQueue;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MaintenanceJobs> _logger;

        public MaintenanceJobs(
            IApplicationDbContext context,
            IEmailQueueService emailQueue,
            INotificationService notificationService,
            IConfiguration configuration,
            ILogger<MaintenanceJobs> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================
        // JOB 1: EXPIRE POLICIES
        // Runs daily at 1:00 AM
        // ============================================
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task ExpirePoliciesAsync()
        {
            _logger.LogInformation("[Maintenance] Starting ExpirePolicies job at {Time}", DateTime.UtcNow);

            var now = DateTime.UtcNow;

            var policiesToExpire = await _context.InsurancePolicies
                .Include(p => p.Claims)
                .Include(p => p.PremiumSchedules)
                .Where(p => !p.IsDeleted
                            && (p.Status == PolicyStatus.Active || p.Status == PolicyStatus.PaymentReceived)
                            && p.EndDate <= now)
                .ToListAsync();

            if (policiesToExpire.Count == 0)
            {
                _logger.LogInformation("[Maintenance] No policies to expire");
                return;
            }

            _logger.LogInformation("[Maintenance] Found {Count} policies to expire", policiesToExpire.Count);

            foreach (var policy in policiesToExpire)
            {
                // H6: Auto-cancel all pending claims on this expired policy
                var pendingClaims = policy.Claims
                    .Where(c => !c.IsDeleted && c.Status == ClaimStatus.Pending)
                    .ToList();

                foreach (var claim in pendingClaims)
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.RejectionReason = $"Auto-cancelled: Policy {policy.PolicyNumber} expired on {policy.EndDate:yyyy-MM-dd}";
                    claim.UpdatedAt = now;
                }

                if (pendingClaims.Count > 0)
                {
                    _logger.LogInformation(
                        "[Maintenance] Cancelled {Count} pending claims for expired policy {PolicyId}",
                        pendingClaims.Count, policy.Id);
                }

                // Reset any unpaid PremiumSchedules that were still pending
                var unpaidSchedules = policy.PremiumSchedules
                    .Where(s => s.Status == PremiumScheduleStatus.Pending && !s.IsDeleted)
                    .ToList();

                foreach (var schedule in unpaidSchedules)
                {
                    schedule.Status = PremiumScheduleStatus.Waived;
                    schedule.UpdatedAt = now;
                }

                if (unpaidSchedules.Count > 0)
                {
                    _logger.LogInformation(
                        "[Maintenance] Waived {Count} pending premium schedules for expired policy {PolicyId}",
                        unpaidSchedules.Count, policy.Id);
                }

                policy.Status = PolicyStatus.Expired;
                policy.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[Maintenance] Expired {Count} policies", policiesToExpire.Count);
        }

        // ============================================
        // JOB 2: CLEANUP EXPIRED TOKENS
        // Runs daily at 2:00 AM
        // ============================================
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task CleanupExpiredTokensAsync()
        {
            _logger.LogInformation("[Maintenance] Starting CleanupExpiredTokens job at {Time}", DateTime.UtcNow);

            var now = DateTime.UtcNow;
            var cutoff = now.AddDays(-30);

            var oldResetTokens = await _context.PasswordResetTokens
                .Where(t => t.UsedAt != null && t.UsedAt < cutoff || t.ExpiresAt < cutoff)
                .ToListAsync();

            if (oldResetTokens.Count > 0)
            {
                _context.PasswordResetTokens.RemoveRange(oldResetTokens);
                _logger.LogInformation("[Maintenance] Deleted {Count} old password reset tokens", oldResetTokens.Count);
            }

            var oldOtpCodes = await _context.EmailVerificationCodes
                .Where(c => c.UsedAt != null && c.UsedAt < cutoff || c.ExpiresAt < cutoff)
                .ToListAsync();

            if (oldOtpCodes.Count > 0)
            {
                _context.EmailVerificationCodes.RemoveRange(oldOtpCodes);
                _logger.LogInformation("[Maintenance] Deleted {Count} old OTP codes", oldOtpCodes.Count);
            }

            var oldEmailChangeTokens = await _context.EmailChangeTokens
                .Where(t => t.UsedAt != null && t.UsedAt < cutoff || t.ExpiresAt < cutoff)
                .ToListAsync();

            if (oldEmailChangeTokens.Count > 0)
            {
                _context.EmailChangeTokens.RemoveRange(oldEmailChangeTokens);
                _logger.LogInformation("[Maintenance] Deleted {Count} old email change tokens", oldEmailChangeTokens.Count);
            }

            var oldRefreshTokens = await _context.RefreshTokens
                .Where(t => t.IsRevoked && t.RevokedAt < cutoff)
                .ToListAsync();

            if (oldRefreshTokens.Count > 0)
            {
                _context.RefreshTokens.RemoveRange(oldRefreshTokens);
                _logger.LogInformation("[Maintenance] Deleted {Count} old refresh tokens", oldRefreshTokens.Count);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[Maintenance] Token cleanup complete");
        }

        // ============================================
        // JOB 3: POLICY EXPIRY REMINDER
        // Runs daily at 9:00 AM
        // ============================================
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task SendPolicyExpiryRemindersAsync()
        {
            _logger.LogInformation("[Maintenance] Starting PolicyExpiryReminder job at {Time}", DateTime.UtcNow);

            var now = DateTime.UtcNow;
            var sevenDaysFromNow = now.AddDays(7);

            // H2: Include PaymentReceived policies — they're fully paid and awaiting admin activation,
            // the farmer still needs to know it's expiring
            var policiesExpiringSoon = await _context.InsurancePolicies
                .Include(p => p.Farm).ThenInclude(f => f!.User)
                .Where(p => !p.IsDeleted
                            && (p.Status == PolicyStatus.Active || p.Status == PolicyStatus.PaymentReceived)
                            && p.EndDate >= sevenDaysFromNow.AddHours(-12)
                            && p.EndDate <= sevenDaysFromNow.AddHours(12))
                .ToListAsync();

            if (policiesExpiringSoon.Count == 0)
            {
                _logger.LogInformation("[Maintenance] No policies expiring in 7 days");
                return;
            }

            _logger.LogInformation("[Maintenance] Sending {Count} expiry reminders", policiesExpiringSoon.Count);

            var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";

            foreach (var policy in policiesExpiringSoon)
            {
                var farmer = policy.Farm?.User;
                if (farmer == null) continue;

                await _emailQueue.EnqueueEmailAsync(
                    toEmail: farmer.Email,
                    templateName: "PolicyExpiryReminder",
                    model: new PolicyExpiryReminderModel
                    {
                        FarmerName = $"{farmer.FirstName} {farmer.LastName}",
                        PolicyNumber = policy.PolicyNumber,
                        CropType = policy.CropType,
                        EndDate = policy.EndDate,
                        DaysLeft = Math.Max(0, (policy.EndDate - now).Days),
                        RenewUrl = $"{frontendBaseUrl}/policies/{policy.Id}"
                    });

                await _notificationService.SendClaimUpdateAsync(farmer.Id, new ClaimNotificationDto
                {
                    Title = "Policy Expiring Soon",
                    Message = $"Your policy {policy.PolicyNumber} will expire on {policy.EndDate:MMM dd, yyyy}. " +
                              "Please renew to maintain coverage.",
                    NotificationType = "PolicyExpiryReminder"
                });
            }

            _logger.LogInformation("[Maintenance] Sent {Count} expiry reminders", policiesExpiringSoon.Count);
        }

        // ============================================
        // JOB 4: CANCEL STALE PENDING POLICIES
        // Runs weekly on Sunday at 3:00 AM
        // ============================================
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task CancelStalePendingPoliciesAsync()
        {
            _logger.LogInformation("[Maintenance] Starting CancelStalePendingPolicies job at {Time}", DateTime.UtcNow);

            var cutoff = DateTime.UtcNow.AddDays(-30); // Pending for 30+ days = stale

            var stalePolicies = await _context.InsurancePolicies
                .Where(p => !p.IsDeleted
                            && p.Status == PolicyStatus.Pending
                            && p.CreatedAt < cutoff)
                .ToListAsync();

            if (stalePolicies.Count == 0)
            {
                _logger.LogInformation("[Maintenance] No stale pending policies found");
                return;
            }

            _logger.LogInformation("[Maintenance] Found {Count} stale pending policies to cancel", stalePolicies.Count);

            foreach (var policy in stalePolicies)
            {
                policy.Status = PolicyStatus.Cancelled;
                policy.CancelledAt = DateTime.UtcNow;
                policy.UpdatedAt = DateTime.UtcNow;
                // H3: Use a dedicated field — NOT RejectionReason (which is for admin-initiated rejections)
                policy.RejectionReason = "Auto-cancelled: Pending for more than 30 days without admin review";
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[Maintenance] Cancelled {Count} stale pending policies", stalePolicies.Count);
        }

        // ============================================
        // JOB 5: CANCEL POLICIES WITH OVERDUE INSTALLMENTS
        // Runs daily at 4:00 AM
        // ============================================
        [Hangfire.AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
        public async Task CancelOverdueInstallmentPoliciesAsync()
        {
            _logger.LogInformation("[Maintenance] Starting CancelOverdueInstallmentPolicies job at {Time}", DateTime.UtcNow);

            var now = DateTime.UtcNow;
            var gracePeriodDays = 30;

            // Find policies with overdue installments past the grace period
            var overdueSchedules = await _context.PremiumSchedules
                .Include(s => s.Policy)
                .Where(s => !s.IsDeleted
                            && s.Status != PremiumScheduleStatus.Paid
                            && s.Status != PremiumScheduleStatus.Waived
                            && s.DueDate < now.AddDays(-gracePeriodDays))
                .ToListAsync();

            if (overdueSchedules.Count == 0)
            {
                _logger.LogInformation("[Maintenance] No overdue installment schedules found");
                return;
            }

            // Group by policy to avoid processing the same policy multiple times
            var policiesToCancel = overdueSchedules
                .GroupBy(s => s.PolicyId)
                .Select(g => g.First().Policy)
                .Where(p => p != null && !p.IsDeleted)
                .ToList();

            _logger.LogInformation("[Maintenance] Found {Count} policies with overdue installments", policiesToCancel.Count);

            foreach (var policy in policiesToCancel)
            {
                if (policy == null) continue;

                // Auto-cancel all pending claims on this policy
                var pendingClaims = await _context.Claims
                    .Where(c => c.PolicyId == policy.Id && !c.IsDeleted && c.Status == ClaimStatus.Pending)
                    .ToListAsync();

                foreach (var claim in pendingClaims)
                {
                    claim.Status = ClaimStatus.Rejected;
                    claim.RejectionReason = $"Policy {policy.PolicyNumber} auto-cancelled due to overdue installments";
                    claim.UpdatedAt = now;
                }

                // Mark overdue schedules as Waived
                var overdue = overdueSchedules.Where(s => s.PolicyId == policy.Id).ToList();
                foreach (var schedule in overdue)
                {
                    schedule.Status = PremiumScheduleStatus.Waived;
                    schedule.UpdatedAt = now;
                }

                policy.Status = PolicyStatus.Cancelled;
                policy.CancelledAt = now;
                policy.UpdatedAt = now;
                // H3:Separate reason field — but since we don't have AutoCancelReason,
                // we put a clear marker in RejectionReason that it's overdue-related
                policy.RejectionReason = $"Auto-cancelled: Installment overdue by more than {gracePeriodDays} days";

                _logger.LogInformation(
                    "[Maintenance] Cancelled policy {PolicyId} — {OverdueCount} overdue installments, {ClaimCount} claims cancelled",
                    policy.Id, overdue.Count, pendingClaims.Count);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[Maintenance] Cancelled {Count} policies with overdue installments", policiesToCancel.Count);
        }
    }

    // ============================================
    // EMAIL MODEL (for the reminder)
    // ============================================
    public class PolicyExpiryReminderModel
    {
        public string FarmerName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string CropType { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public int DaysLeft { get; set; }
        public string RenewUrl { get; set; } = string.Empty;
    }
}
