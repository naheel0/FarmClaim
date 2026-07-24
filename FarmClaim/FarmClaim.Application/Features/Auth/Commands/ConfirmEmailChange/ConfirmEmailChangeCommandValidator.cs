using FluentValidation;

namespace FarmClaim.Application.Features.Auth.Commands.ConfirmEmailChange
{
    public class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
    {
        public ConfirmEmailChangeCommandValidator()
        {
            RuleFor(x => x.Request.Token).NotEmpty();
            RuleFor(x => x.Request.NewEmail)
                .NotEmpty().EmailAddress().MaximumLength(256);
        }
    }
}