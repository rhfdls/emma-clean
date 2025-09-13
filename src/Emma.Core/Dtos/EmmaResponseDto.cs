using Emma.Core.Models;
using System.Text.Json.Serialization;

namespace Emma.Core.Dtos
{
    public class EmmaResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("action")]
        public EmmaAction? Action { get; set; }

        [JsonPropertyName("raw")]
        public string? Raw { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        public static EmmaResponseDto SuccessResponse(EmmaAction action, string raw, string correlationId)
        {
            return new EmmaResponseDto
            {
                Success = true,
                Action = action,
                Raw = raw,
                CorrelationId = correlationId
            };
        }

        public static EmmaResponseDto ErrorResponse(string error, string correlationId, string? raw)
        {
            return new EmmaResponseDto
            {
                Success = false,
                Error = error,
                Raw = raw,
                CorrelationId = correlationId
            };
        }
    }
}
