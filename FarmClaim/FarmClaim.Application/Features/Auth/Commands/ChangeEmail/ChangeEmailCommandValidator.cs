using FluentValidation;

namespace FarmClaim.Application.Features.Auth.Commands.ChangeEmail
{
    public class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
    {
        public ChangeEmailCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();

            RuleFor(x => x.Request.NewEmail)
                .NotEmpty().WithMessage("New email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(256);

            RuleFor(x => x.Request.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required");
        }
    }
}