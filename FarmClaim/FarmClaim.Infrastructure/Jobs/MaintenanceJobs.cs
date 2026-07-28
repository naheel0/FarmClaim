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
                .Where(p => !p.IsDeleted
                            && p.Status == PolicyStatus.Active
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

            var policiesExpiringSoon = await _context.InsurancePolicies
                .Include(p => p.Farm).ThenInclude(f => f!.User)
                .Where(p => !p.IsDeleted
                            && p.Status == PolicyStatus.Active
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
                policy.RejectionReason = "Auto-cancelled: Pending for more than 30 days without admin review";
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("[Maintenance] Cancelled {Count} stale pending policies", stalePolicies.Count);
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