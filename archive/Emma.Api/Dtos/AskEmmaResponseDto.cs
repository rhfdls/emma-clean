using System;

namespace Emma.Api.Dtos
{
    public class AskEmmaResponseDto
    {
        public Guid RequestId { get; set; }
        public string Response { get; set; } = string.Empty;
        public long ProcessingTimeMs { get; set; }
    }
}
