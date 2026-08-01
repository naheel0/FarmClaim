using System.Security.Cryptography;
using System.Text;
using FarmClaim.Application.Common.Interfaces;
using FarmClaim.Application.Features.Payments.DTOs;
using FarmClaim.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Razorpay.Api;

namespace FarmClaim.Infrastructure.Services
{
    /// <summary>
    /// Razorpay API implementation of IPaymentService.
    /// Docs: https://razorpay.com/docs/api/orders/
    /// </summary>
    public class RazorpayPaymentService : IPaymentService
    {
        private readonly RazorpaySettings _settings;
        private readonly ILogger<RazorpayPaymentService> _logger;
        private readonly bool _isProduction;

        public RazorpayPaymentService(
            IOptions<RazorpaySettings> settings,
            ILogger<RazorpayPaymentService> logger,
            IHostEnvironment env)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger;
            _isProduction = env.IsProduction();

            // C4 FIX: Hard fail at startup if DummyMode is accidentally enabled in Production.
            // This prevents accepting fake payments when the env var is misconfigured.
            if (_settings.DummyMode && _isProduction)
            {
                throw new InvalidOperationException(
                    "Razorpay DummyMode is enabled in Production. " +
                    "Set DummyMode=false in production config or set ASPNETCORE_ENVIRONMENT=Production.");
            }
        }

        // ============================================
        // CREATE ORDER
        // ============================================
        public async Task<CreateOrderResponseDto> CreateOrderAsync(
            decimal amountInRupees,
            string currency,
            string receipt,
            Guid policyId,
            Guid userId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_settings.KeyId) || string.IsNullOrEmpty(_settings.KeySecret))
                throw new InvalidOperationException("Razorpay KeyId/KeySecret not configured.");

            // C4 FIX: Use injected _isProduction instead of fragile env-var string comparison
            if (_settings.DummyMode && !_isProduction)
            {
                _logger.LogInformation("DUMMY: Creating Razorpay order for amount {Amount}", amountInRupees);
                return new CreateOrderResponseDto
                {
                    OrderId = $"order_dummy_{Guid.NewGuid():N}",
                    AmountInPaise = (long)(amountInRupees * 100),
                    AmountInRupees = amountInRupees,
                    Currency = currency,
                    RazorpayKeyId = _settings.KeyId,
                    ReceiptNumber = receipt,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };
            }

            // Real Razorpay API call
            RazorpayClient client = new(_settings.KeyId, _settings.KeySecret);

            var orderDict = new Dictionary<string, object>
            {
                { "amount", (long)(amountInRupees * 100) },  // Convert rupees to paise
                { "currency", currency },
                { "receipt", receipt },
                { "payment_capture", 1 },  // Auto-capture
                { "notes", new Dictionary<string, string>
                    {
                        { "policy_id", policyId.ToString() },
                        { "user_id", userId.ToString() },
                        { "source", "FarmClaim API" }
                    }
                }
            };

            _logger.LogInformation("Creating Razorpay order: Amount={Amount}, Receipt={Receipt}",
                amountInRupees, receipt);

            try
            {
                // Razorpay SDK doesn't have async methods, so use Task.Run
                var order = await Task.Run(() => client.Order.Create(orderDict), ct);

                // Safely extract values with local variables to satisfy nullable analysis
                var idObj = order["id"];
                var statusObj = order["status"];
                var amountDueObj = order["amount_due"];

                string orderId = idObj != null ? (string)idObj.ToString()! : string.Empty;
                string orderStatus = statusObj != null ? (string)statusObj.ToString()! : "unknown";
                long amountDue = amountDueObj != null
                    ? long.TryParse((string)amountDueObj.ToString()!, out var due) ? due : 0
                    : 0;

                _logger.LogInformation("Razorpay order created: Id={OrderId}, Status={Status}, Due={Due}",
                    orderId, orderStatus, amountDue);

                return new CreateOrderResponseDto
                {
                    OrderId = orderId,
                    AmountInPaise = amountDue,
                    AmountInRupees = amountInRupees,
                    Currency = currency,
                    RazorpayKeyId = _settings.KeyId,
                    ReceiptNumber = receipt,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Razorpay order creation failed for PolicyId: {PolicyId}, Amount: {Amount}",
                    policyId, amountInRupees);
                throw new InvalidOperationException(
                    $"Payment gateway error: {ex.Message}. Please try again or contact support.");
            }
        }

        // ============================================
        // VERIFY SIGNATURE
        // ============================================
        public Task<bool> VerifySignatureAsync(string orderId, string paymentId, string signature)
        {
            if (_settings.DummyMode && !_isProduction)
            {
                _logger.LogInformation("DUMMY: Skipping signature verification");
                return Task.FromResult(true);
            }

            // M8 FIX: Null-check signature to prevent NRE on .ToLowerInvariant()
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Signature is null or empty for Order {OrderId}", orderId);
                return Task.FromResult(false);
            }

            // Razorpay signature = HMAC-SHA256(key_secret, orderId + "|" + paymentId)
            var payload = $"{orderId}|{paymentId}";
            var secret = _settings.KeySecret;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
            var receivedBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());
            var isValid = CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);

            if (!isValid)
            {
                _logger.LogWarning("Signature mismatch for Order {OrderId}", orderId);
            }

            return Task.FromResult(isValid);
        }

        // ============================================
        // FETCH PAYMENT DETAILS
        // ============================================
        public async Task<PaymentDetailsDto> FetchPaymentDetailsAsync(string paymentId)
        {
            if (_settings.DummyMode && !_isProduction)
            {
                return new PaymentDetailsDto
                {
                    PaymentId = paymentId,
                    Method = "upi",
                    Vpa = "test@upi",
                    Status = "captured",
                    Fee = 0,
                    Tax = 0
                };
            }

            RazorpayClient client = new(_settings.KeyId, _settings.KeySecret);

            var payment = await Task.Run(() => client.Payment.Fetch(paymentId));

            // Safely extract scalar fields
            var methodObj = payment["method"];
            var statusObj = payment["status"];

            string method = methodObj != null ? (string)methodObj.ToString()! : "";
            string status = statusObj != null ? (string)statusObj.ToString()! : "unknown";

            var details = new PaymentDetailsDto
            {
                PaymentId = paymentId,
                Method = method,
                Status = status
            };

            // Fee + tax
            decimal fee = 0, tax = 0;
            var feeObj = payment["fee"];
            if (feeObj != null)
            {
                var feeStr = (string)feeObj.ToString()!;
                decimal.TryParse(feeStr, out fee);
            }
            var taxObj = payment["tax"];
            if (taxObj != null)
            {
                var taxStr = (string)taxObj.ToString()!;
                decimal.TryParse(taxStr, out tax);
            }
            details.Fee = fee;
            details.Tax = tax;

            // Method-specific fields
            switch (details.Method)
            {
                case "card":
                    var card = payment["card"] as Dictionary<string, object>;
                    if (card != null)
                    {
                        if (card.ContainsKey("last4") && card["last4"] != null)
                            details.CardLast4 = (string)card["last4"]!.ToString()!;
                        if (card.ContainsKey("network") && card["network"] != null)
                            details.CardNetwork = (string)card["network"]!.ToString()!;
                    }
                    break;

                case "upi":
                    var vpaObj = payment["vpa"];
                    if (vpaObj != null)
                    {
                        details.Vpa = (string)vpaObj.ToString()!;
                    }
                    break;

                case "netbanking":
                    var bankObj = payment["bank"];
                    if (bankObj != null)
                    {
                        details.Bank = (string)bankObj.ToString()!;
                    }
                    break;

                case "wallet":
                    var walletObj = payment["wallet"];
                    if (walletObj != null)
                    {
                        details.Wallet = (string)walletObj.ToString()!;
                    }
                    break;
            }

            // Bank reference (acquirer_data)
            var bankRef = payment["acquirer_data"] as Dictionary<string, object>;
            if (bankRef != null && bankRef.ContainsKey("bank_transaction_id") && bankRef["bank_transaction_id"] != null)
            {
                details.BankReference = (string)bankRef["bank_transaction_id"]!.ToString()!;
            }

            _logger.LogInformation("Fetched payment details: Method={Method}, Status={Status}",
                details.Method, details.Status);

            return details;
        }

        // ============================================
        // VERIFY WEBHOOK SIGNATURE
        // ============================================
        public bool VerifyWebhookSignature(string payload, string signature)
        {
            if (_settings.DummyMode && !_isProduction)
            {
                _logger.LogWarning("DUMMY: Skipping webhook signature verification");
                return true;
            }

            if (string.IsNullOrEmpty(_settings.WebhookSecret))
            {
                _logger.LogWarning("WebhookSecret not configured - skipping webhook verification");
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLower();

            var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
            var receivedBytes = Encoding.UTF8.GetBytes(signature.ToLower());
            var isValid = CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);

            if (!isValid)
            {
                _logger.LogWarning("Webhook signature verification failed");
            }

            return isValid;
        }

        // ============================================
        // REFUND PAYMENT
        // ============================================
        public async Task<RefundResultDto> RefundPaymentAsync(
            string razorpayPaymentId,
            decimal amountInRupees,
            string reason,
            CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_settings.KeyId) || string.IsNullOrEmpty(_settings.KeySecret))
                throw new InvalidOperationException("Razorpay KeyId/KeySecret not configured.");

            if (_settings.DummyMode && !_isProduction)
            {
                _logger.LogInformation("DUMMY: Refunding payment {PaymentId}, Amount={Amount}, Reason={Reason}",
                    razorpayPaymentId, amountInRupees, reason);
                return new RefundResultDto
                {
                    Success = true,
                    RefundId = $"refund_dummy_{Guid.NewGuid():N}",
                    AmountRefunded = amountInRupees,
                    Status = "processed"
                };
            }

            try
            {
                RazorpayClient client = new(_settings.KeyId, _settings.KeySecret);

                var refundDict = new Dictionary<string, object>
                {
                    { "amount", (long)(amountInRupees * 100) },
                    { "speed", "optimum" },
                    { "notes", new Dictionary<string, string>
                        {
                            { "reason", reason },
                            { "source", "FarmClaim API" }
                        }
                    }
                };

                _logger.LogInformation("Initiating refund: PaymentId={PaymentId}, Amount={Amount}, Reason={Reason}",
                    razorpayPaymentId, amountInRupees, reason);

                var refund = await Task.Run(() =>
                    client.Payment.Fetch(razorpayPaymentId).Refund(refundDict), ct);

                string refundId = refund["id"]?.ToString() ?? "";
                string refundStatus = refund["status"]?.ToString() ?? "unknown";
                decimal refundAmount = 0m;
                if (refund["amount"] != null)
                {
                    string amtStr = refund["amount"].ToString() ?? "";
                    if (decimal.TryParse(amtStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedAmt))
                        refundAmount = parsedAmt / 100m;
                }

                string rid = refundId;
                string rstatus = refundStatus;
                decimal ramount = refundAmount;
                _logger.LogInformation("Refund initiated: RefundId={RefundId}, Status={Status}, Amount={Amount}",
                    rid, rstatus, ramount);

                return new RefundResultDto
                {
                    Success = true,
                    RefundId = refundId,
                    AmountRefunded = refundAmount > 0 ? refundAmount : amountInRupees,
                    Status = refundStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refund failed for PaymentId={PaymentId}", razorpayPaymentId);
                return new RefundResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}