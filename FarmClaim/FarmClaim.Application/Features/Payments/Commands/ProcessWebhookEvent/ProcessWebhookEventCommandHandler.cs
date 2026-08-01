using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Payments.DTOs;
using FarmClaim.Domain.Entities;
using FarmClaim.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FarmClaim.Application.Features.Payments.Commands.ProcessWebhookEvent
{
    public class ProcessWebhookEventCommandHandler : IRequestHandler<ProcessWebhookEventCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ProcessWebhookEventCommandHandler> _logger;

        public ProcessWebhookEventCommandHandler(
            IApplicationDbContext context,
            ILogger<ProcessWebhookEventCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> Handle(ProcessWebhookEventCommand cmd, CancellationToken ct)
        {
            var evt = cmd.Event;

            // Generate a unique event ID from the payload (Razorpay doesn't always provide event.id)
            // Use payment/refund ID for idempotency — avoid DateTime.UtcNow.Ticks which differs per retry
            var eventId = $"{evt.Event}_{evt.Payload.Payment?.Id ?? evt.Payload.Refund?.Id ?? evt.Payload.Order?.Id ?? evt.CreatedAt.ToString()}";

            _logger.LogInformation("Processing Razorpay webhook: Event={Event}, EventId={EventId}",
                evt.Event, eventId);

            // IDEMPOTENCY: Check if this event was already processed
            var existingEvent = await _context.WebhookEvents
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.ProcessedAt != null, ct);

            if (existingEvent != null)
            {
                _logger.LogInformation("Duplicate webhook event {EventId} — skipping", eventId);
                return true;
            }

            // Record the event
            var webhookEvent = new WebhookEvent
            {
                EventId = eventId,
                EventType = evt.Event,
                Payload = cmd.RawPayload,
                OrderId = evt.Payload.Payment?.OrderId ?? evt.Payload.Order?.Id,
                PaymentId = evt.Payload.Payment?.Id ?? evt.Payload.Refund?.PaymentId
            };

            try
            {
                switch (evt.Event)
                {
                    case "payment.captured":
                        await HandlePaymentCapturedAsync(evt, ct);
                        break;

                    case "payment.authorized":
                        _logger.LogInformation("Payment authorized (no action needed for auto-capture)");
                        break;

                    case "payment.failed":
                        await HandlePaymentFailedAsync(evt, ct);
                        break;

                    case "refund.processed":
                        await HandleRefundProcessedAsync(evt, ct);
                        break;

                    case "refund.created":
                        _logger.LogInformation("Refund created for payment {PaymentId}", evt.Payload.Refund?.PaymentId);
                        break;

                    case "order.paid":
                        _logger.LogInformation("Order {OrderId} marked as paid", evt.Payload.Order?.Id);
                        break;

                    case "payment.downtime":
                        _logger.LogWarning("Razorpay payment downtime notification received");
                        break;

                    default:
                        _logger.LogInformation("Unhandled event type: {Event}", evt.Event);
                        break;
                }

                webhookEvent.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                webhookEvent.ProcessingError = ex.Message;
                _logger.LogError(ex, "Failed to process webhook event {EventId}", eventId);
                throw;
            }
            finally
            {
                _context.WebhookEvents.Add(webhookEvent);
                await _context.SaveChangesAsync(ct);
            }

            return true;
        }

        private async Task HandlePaymentCapturedAsync(RazorpayWebhookEventDto evt, CancellationToken ct)
        {
            var paymentDto = evt.Payload.Payment;
            if (paymentDto == null) return;

            var payment = await _context.Payments
                .Include(p => p.Policy).ThenInclude(p => p!.PremiumSchedules)
                .FirstOrDefaultAsync(p => p.OrderId == paymentDto.OrderId, ct);

            if (payment == null)
            {
                _logger.LogWarning("Webhook: Payment record not found for OrderId {OrderId}", paymentDto.OrderId);
                return;
            }

            // IDEMPOTENCY: Already processed
            if (payment.Status == PaymentStatus.Captured && payment.PaymentId == paymentDto.Id)
            {
                _logger.LogInformation("Webhook: Payment {PaymentId} already captured — skipping", payment.Id);
                return;
            }

            payment.PaymentId = paymentDto.Id;
            payment.Status = PaymentStatus.Captured;
            payment.CapturedAt = DateTime.UtcNow;
            payment.Method = paymentDto.Method;
            payment.Fee = paymentDto.Fee.HasValue ? paymentDto.Fee.Value / 100m : null;
            payment.Tax = paymentDto.Tax.HasValue ? paymentDto.Tax.Value / 100m : null;

            // Handle installment payments — mirror logic from VerifyPaymentCommandHandler
            if (payment.PremiumScheduleId.HasValue && payment.Policy != null)
            {
                var schedule = await _context.PremiumSchedules
                    .FirstOrDefaultAsync(s => s.Id == payment.PremiumScheduleId.Value && !s.IsDeleted, ct);

                if (schedule != null && schedule.Status != PremiumScheduleStatus.Paid)
                {
                    schedule.Status = PremiumScheduleStatus.Paid;
                    schedule.PaidAt = DateTime.UtcNow;
                    schedule.PaymentId = payment.Id;

                    var policy = payment.Policy;
                    var totalInstallments = policy.PremiumSchedules.Count(s => !s.IsDeleted);
                    var paidCount = policy.PremiumSchedules.Count(s => !s.IsDeleted && s.Status == PremiumScheduleStatus.Paid);

                    policy.CurrentInstallmentNumber = paidCount;

                    var nextSchedule = policy.PremiumSchedules
                        .Where(s => !s.IsDeleted && s.Status == PremiumScheduleStatus.Pending)
                        .OrderBy(s => s.InstallmentNumber)
                        .FirstOrDefault();

                    policy.NextInstallmentDueDate = nextSchedule?.DueDate;

                    if (paidCount >= totalInstallments && totalInstallments > 0)
                    {
                        _logger.LogInformation(
                            "Webhook: All {Count} installments paid for policy {PolicyId}. Transitioning to PaymentReceived.",
                            totalInstallments, policy.Id);
                        policy.Status = PolicyStatus.PaymentReceived;
                    }
                }
            }
            // Single full payment — transition from Pending to PaymentReceived
            else if (payment.Policy != null && payment.Policy.Status == PolicyStatus.Pending)
            {
                payment.Policy.Status = PolicyStatus.PaymentReceived;
                _logger.LogInformation("Webhook: Policy {PolicyId} transitioned to PaymentReceived after payment", payment.PolicyId);
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Webhook: Payment {PaymentId} marked as Captured", payment.Id);
        }

        private async Task HandlePaymentFailedAsync(RazorpayWebhookEventDto evt, CancellationToken ct)
        {
            var paymentDto = evt.Payload.Payment;
            if (paymentDto == null) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == paymentDto.OrderId, ct);

            if (payment == null)
            {
                _logger.LogWarning("Webhook: Payment record not found for OrderId {OrderId}", paymentDto.OrderId);
                return;
            }

            if (payment.Status == PaymentStatus.Failed)
            {
                _logger.LogInformation("Webhook: Payment {PaymentId} already marked Failed — skipping", payment.Id);
                return;
            }

            payment.Status = PaymentStatus.Failed;
            payment.FailedAt = DateTime.UtcNow;
            payment.FailureReason = paymentDto.ErrorDescription ?? "Payment failed at bank/gateway";
            payment.PaymentId = paymentDto.Id;

            await _context.SaveChangesAsync(ct);
            _logger.LogWarning("Webhook: Payment {PaymentId} marked as Failed. Reason: {Reason}",
                payment.Id, payment.FailureReason);
        }

        private async Task HandleRefundProcessedAsync(RazorpayWebhookEventDto evt, CancellationToken ct)
        {
            var refundDto = evt.Payload.Refund;
            if (refundDto == null) return;

            var payment = await _context.Payments
                .Include(p => p.PremiumSchedule)
                .FirstOrDefaultAsync(p => p.PaymentId == refundDto.PaymentId, ct);

            if (payment == null)
            {
                _logger.LogWarning("Webhook: Payment not found for refund. PaymentId={PaymentId}", refundDto.PaymentId);
                return;
            }

            if (payment.Status == PaymentStatus.Refunded)
            {
                _logger.LogInformation("Webhook: Payment {PaymentId} already marked Refunded — skipping", payment.Id);
                return;
            }

            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAt = DateTime.UtcNow;
            payment.Notes = $"Refunded. Refund ID: {refundDto.Id}, Amount: ₹{refundDto.Amount / 100m:N2}, Speed: {refundDto.Speed}";

            // Reset PremiumSchedule status if this was an installment payment
            if (payment.PremiumSchedule != null && payment.PremiumSchedule.Status == PremiumScheduleStatus.Paid)
            {
                payment.PremiumSchedule.Status = PremiumScheduleStatus.Pending;
                payment.PremiumSchedule.PaidAt = null;
                _logger.LogInformation("Webhook: Reset PremiumSchedule {ScheduleId} to Pending after refund",
                    payment.PremiumSchedule.Id);
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Webhook: Payment {PaymentId} marked as Refunded. RefundId={RefundId}",
                payment.Id, refundDto.Id);
        }
    }
}