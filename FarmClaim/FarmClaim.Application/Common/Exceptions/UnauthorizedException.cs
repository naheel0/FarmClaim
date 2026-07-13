using System;

namespace FarmClaim.Application.Common.Exceptions
{
    public class UnauthorizedException : UnauthorizedAccessException
    {
        public UnauthorizedException() : base("Unauthorized access") { }
        public UnauthorizedException(string message) : base(message) { }
        public UnauthorizedException(string message, Exception inner) : base(message, inner) { }
    }
}