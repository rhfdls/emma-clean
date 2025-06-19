using System;

namespace Emma.Core.Models
{
    public class AgentServiceException : Exception
    {
        public AgentServiceException(string message) : base(message) { }
        public AgentServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
