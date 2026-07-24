using FluentValidation;

namespace FarmClaim.Application.Features.Auth.Commands.ResendOtp
{
    public class ResendOtpCommandValidator : AbstractValidator<ResendOtpCommand>
    {
        public ResendOtpCommandValidator()
        {
            RuleFor(x => x.Request.Email)
                .NotEmpty().EmailAddress().MaximumLength(256);
        }
    }
}