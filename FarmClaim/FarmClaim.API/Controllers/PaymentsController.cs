using FarmClaim.Application.Common.Exceptions;
using FarmClaim.Application.Features.Payments.Commands.CreateOrder;
using FarmClaim.Application.Features.Payments.Commands.ProcessWebhookEvent;
using FarmClaim.Application.Features.Payments.Commands.VerifyPayment;
using FarmClaim.Application.Features.Payments.DTOs;
using FarmClaim.Application.Features.Payments.Queries.GetPaymentByPolicyId;
using FarmClaim.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace FarmClaim.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // POST /api/v1/Payments/create-order/{policyId}
        [HttpPost("create-order/{policyId}")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(CreateOrderResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateOrder(Guid policyId, [FromBody] CreateOrderRequestDto? request)
        {
            try
            {
                var userId = GetUserId();
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers.UserAgent.ToString();

                var command = new CreateOrderCommand(policyId, userId, request ?? new CreateOrderRequestDto(), clientIp, userAgent);
                var result = await _mediator.Send(command);

                return Ok(result);
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
                _logger.LogError(ex, "Failed to create order for policy {PolicyId}", policyId);
                return StatusCode(500, new { error = "Failed to initiate payment. Please try again." });
            }
        }

        // POST /api/v1/Payments/verify
        [HttpPost("verify")]
        [Authorize(Roles = "Farmer")]
        [ProducesResponseType(typeof(VerifyPaymentResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestDto request)
        {
            try
            {
                var userId = GetUserId();
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                var command = new VerifyPaymentCommand(request, userId, clientIp);
                var result = await _mediator.Send(command);

                return Ok(result);
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
                _logger.LogError(ex, "Payment verification failed for Order {OrderId}", request.RazorpayOrderId);
                return StatusCode(500, new { error = "Payment verification failed." });
            }
        }

        // GET /api/v1/Payments/policy/{policyId}
        [HttpGet("policy/{policyId}")]
        [ProducesResponseType(typeof(PaymentResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaymentByPolicyId(Guid policyId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _mediator.Send(new GetPaymentByPolicyIdQuery(policyId, userId));
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get payment for policy {PolicyId}", policyId);
                return StatusCode(500, new { error = "Failed to load payment details." });
            }
        }

        // POST /api/v1/Payments/webhook
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> RazorpayWebhook(
            [FromServices] RazorpayPaymentService razorpayService)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(payload))
                    return BadRequest(new { error = "Empty payload" });

                var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Webhook called without signature header");
                    return Unauthorized(new { error = "Missing signature" });
                }

                if (!razorpayService.VerifyWebhookSignature(payload, signature))
                {
                    _logger.LogError("❌ Webhook signature verification failed");
                    return Unauthorized(new { error = "Invalid signature" });
                }

                RazorpayWebhookEventDto? webhookEvent;
                try
                {
                    webhookEvent = JsonSerializer.Deserialize<RazorpayWebhookEventDto>(payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse webhook payload");
                    return BadRequest(new { error = "Invalid JSON" });
                }

                if (webhookEvent == null || string.IsNullOrEmpty(webhookEvent.Event))
                {
                    return BadRequest(new { error = "Invalid webhook event" });
                }

                var command = new ProcessWebhookEventCommand(webhookEvent, payload, signature);
                await _mediator.Send(command);

                return Ok(new { received = true, @event = webhookEvent.Event });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Razorpay webhook processing failed");
                return Ok(new { received = true, error = "Processing failed but acknowledged" });
            }
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(claim, out var id))
                throw new UnauthorizedAccessException("Invalid user identity.");
            return id;
        }
    }
}