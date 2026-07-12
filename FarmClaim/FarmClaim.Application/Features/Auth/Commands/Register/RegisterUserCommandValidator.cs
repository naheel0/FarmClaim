using FluentValidation;

namespace FarmClaim.Application.Features.Auth.Commands.Register
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x = > x.Request.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Invalid email");
            RuleFor(x = > x.Request.Password).NotEmpty().WithMessage("Password required").MinimumLength(6).WithMessage("Min 6 chars")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$").WithMessage("Must have uppercase, lowercase, number");
            RuleFor(x = > x.Request.FirstName).NotEmpty().WithMessage("First name required").MaximumLength(100);
            RuleFor(x = > x.Request.LastName).NotEmpty().WithMessage("Last name required").MaximumLength(100);
            RuleFor(x = > x.Request.Role).IsInEnum().WithMessage("Role must be Farmer or Admin");
        }
    }
}