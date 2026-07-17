using System;

namespace FarmClaim.Application.Common.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("Access denied") { }
        public ForbiddenException(string message) : base(message) { }
        public ForbiddenException(string message, Exception inner) : base(message, inner) { }
    }
}