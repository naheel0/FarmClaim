using FluentValidation;

namespace FarmClaim.Application.Features.Auth.Commands.VerifyEmail
{
    public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
    {
        public VerifyEmailCommandValidator()
        {
            RuleFor(x => x.Request.Email)
                .NotEmpty().EmailAddress().MaximumLength(256);

            RuleFor(x => x.Request.Otp)
                .NotEmpty()
                .Matches(@"^\d{6}$").WithMessage("OTP must be exactly 6 digits");
        }
    }
}