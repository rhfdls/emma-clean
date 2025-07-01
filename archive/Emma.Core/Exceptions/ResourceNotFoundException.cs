using System;

namespace Emma.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when a requested resource is not found.
    /// </summary>
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException()
        {
        }

        public ResourceNotFoundException(string message)
            : base(message)
        {
        }

        public ResourceNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
