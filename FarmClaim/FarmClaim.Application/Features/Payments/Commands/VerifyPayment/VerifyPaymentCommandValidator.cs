using FluentValidation;

namespace FarmClaim.Application.Features.Payments.Commands.VerifyPayment
{
    public class VerifyPaymentCommandValidator : AbstractValidator<VerifyPaymentCommand>
    {
        public VerifyPaymentCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();

            RuleFor(x => x.Request.RazorpayOrderId)
                .NotEmpty().WithMessage("Razorpay Order ID is required")
                .MaximumLength(100);

            RuleFor(x => x.Request.RazorpayPaymentId)
                .NotEmpty().WithMessage("Razorpay Payment ID is required")
                .MaximumLength(100);

            RuleFor(x => x.Request.RazorpaySignature)
                .NotEmpty().WithMessage("Razorpay Signature is required")
                .MaximumLength(500);
        }
    }
}