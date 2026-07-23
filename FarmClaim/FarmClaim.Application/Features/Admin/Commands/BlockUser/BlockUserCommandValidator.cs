using FluentValidation;

namespace FarmClaim.Application.Features.Admin.Commands.BlockUser
{
    public class BlockUserCommandValidator : AbstractValidator<BlockUserCommand>
    {
        public BlockUserCommandValidator()
        {
            RuleFor(x => x.TargetUserId).NotEmpty();
            RuleFor(x => x.AdminUserId).NotEmpty();

            RuleFor(x => x.Request.Reason)
                .NotEmpty().WithMessage("Reason is required for blocking a user")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
        }
    }
}