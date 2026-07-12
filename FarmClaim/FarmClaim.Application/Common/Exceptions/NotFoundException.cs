using System;

namespace FarmClaim.Application.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception inner) : base(message, inner) { }
        public NotFoundException(object key, object value)
            : base($"Entity \"{key}\" ({value}) was not found.") { }
    }
}