using System;

namespace Emma.Core.Models
{
    public class PromptFormatException : Exception
    {
        public PromptFormatException(string message) : base(message) { }
        public PromptFormatException(string message, Exception innerException) : base(message, innerException) { }
    }
}
