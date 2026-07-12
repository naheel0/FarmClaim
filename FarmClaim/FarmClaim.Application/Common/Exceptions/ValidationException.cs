using System.Collections.Generic;
using System.Linq;

namespace FarmClaim.Application.Common.Exceptions
{
    public class ValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }

        public ValidationException(IReadOnlyList<string> errors)
            : base("One or more validation failures occurred.")
        {
            Errors = errors;
        }

        public ValidationException(FluentValidation.Results.ValidationResult result)
            : this(result.Errors.Select(e => e.ErrorMessage).ToList()) { }

        public override string ToString() => $"{Message}\n{string.Join("\n", Errors)}";
    }
}