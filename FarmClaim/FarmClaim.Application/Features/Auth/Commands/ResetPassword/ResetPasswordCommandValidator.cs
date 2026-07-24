using FluentValidation;

namespace FarmClaim.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Request.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.Request.Token)
                .NotEmpty().WithMessage("Token is required");

            RuleFor(x => x.Request.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters")
                .NotEqual("password").WithMessage("Password cannot be 'password'")
                .NotEqual("123456").WithMessage("Password cannot be '123456'");

            RuleFor(x => x.Request.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required")
                .Equal(x => x.Request.NewPassword).WithMessage("Passwords do not match");
        }
    }
}