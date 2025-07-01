using System;

namespace Emma.Core.Models
{
    public class AgentValidationException : Exception
    {
        public AgentValidationException(string message) : base(message) { }
        public AgentValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
