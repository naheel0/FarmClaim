using FluentValidation;

namespace FarmClaim.Application.Features.Farmers.Commands.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.Request.FirstName)
                .MaximumLength(100).WithMessage("First name too long")
                .Matches(@"^[a-zA-Z\s'-]*$").WithMessage("Invalid characters in first name")
                .When(x => !string.IsNullOrEmpty(x.Request.FirstName));

            RuleFor(x => x.Request.LastName)
                .MaximumLength(100).WithMessage("Last name too long")
                .Matches(@"^[a-zA-Z\s'-]*$").WithMessage("Invalid characters in last name")
                .When(x => !string.IsNullOrEmpty(x.Request.LastName));

            RuleFor(x => x.Request.PhoneNumber)
                .Matches(@"^[\+]?[\d\s\-\(\)]{7,20}$").WithMessage("Invalid phone format")
                .When(x => !string.IsNullOrEmpty(x.Request.PhoneNumber));

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}