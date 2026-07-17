using System;

namespace FarmClaim.Application.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public string? EntityName { get; }
        public object? KeyValue { get; }

        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception inner) : base(message, inner) { }
        public NotFoundException(string entityName, object key)
            : base($"Entity \"{entityName}\" ({key}) was not found.")
        {
            EntityName = entityName;
            KeyValue = key;
        }
    }
}