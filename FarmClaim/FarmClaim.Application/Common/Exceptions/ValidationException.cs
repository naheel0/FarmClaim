using System.Collections.Generic;
using System.Linq;

namespace FarmClaim.Application.Common.Exceptions
{
    public class ValidationException : Exception
    {
        public IReadOnlyList<string> Errors { get; }
        public IReadOnlyDictionary<string, string[]> PropertyErrors { get; }

        public ValidationException(IReadOnlyList<string> errors)
            : base("One or more validation failures occurred.")
        {
            Errors = errors;
            PropertyErrors = new Dictionary<string, string[]>();
        }

        public ValidationException(IDictionary<string, string[]> propertyErrors)
            : base("One or more validation failures occurred.")
        {
            PropertyErrors = propertyErrors as IReadOnlyDictionary<string, string[]>
                ?? new Dictionary<string, string[]>(propertyErrors);
            Errors = propertyErrors.SelectMany(kv => kv.Value).ToList();
        }

        public ValidationException(FluentValidation.Results.ValidationResult result)
            : this(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()))
        { }

        public override string ToString() => $"{Message}\n{string.Join("\n", Errors)}";
    }
}