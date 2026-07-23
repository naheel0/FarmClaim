using FluentValidation;

namespace FarmClaim.Application.Features.Admin.Commands.SuspendUser
{
    public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
    {
        public SuspendUserCommandValidator()
        {
            RuleFor(x => x.TargetUserId).NotEmpty();
            RuleFor(x => x.AdminUserId).NotEmpty();

            RuleFor(x => x.Request.Reason)
                .NotEmpty().WithMessage("Reason is required")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }
}