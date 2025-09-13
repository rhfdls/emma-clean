using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Emma.Core.Models
{
    public class EmmaAction : IValidatableObject
    {
        [Required(ErrorMessage = "Action type is required")]
        [JsonPropertyName("action")]
        public EmmaActionType Action { get; set; } = EmmaActionType.Unknown;

        [JsonPropertyName("payload")]
        public string Payload { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Action != EmmaActionType.None && Action != EmmaActionType.Unknown && string.IsNullOrWhiteSpace(Payload))
            {
                results.Add(new ValidationResult(
                    $"Payload is required for action type: {Action}",
                    new[] { nameof(Payload) }));
            }

            if (Action == EmmaActionType.Unknown)
            {
                results.Add(new ValidationResult(
                    $"Unknown action type. Valid values are: {string.Join(", ", Enum.GetNames(typeof(EmmaActionType)).Where(n => n != nameof(EmmaActionType.Unknown)))}",
                    new[] { nameof(Action) }));
            }

            return results;
        }

        public static EmmaAction FromJson(string json)
        {
            if (json is null) throw new JsonException("JSON input cannot be null");
            if (string.IsNullOrWhiteSpace(json)) throw new JsonException("JSON input cannot be empty");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("action", out _))
            {
                throw new JsonException("Required property 'action' not found in JSON");
            }

            var action = document.RootElement.Deserialize<EmmaAction>(options)
                         ?? throw new JsonException("Failed to deserialize JSON to EmmaAction");

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(action);
            if (!Validator.TryValidateObject(action, validationContext, validationResults, true))
            {
                var errorMessages = validationResults.Select(r => r.ErrorMessage);
                throw new ValidationException($"Validation failed: {string.Join("; ", errorMessages)}");
            }

            return action;
        }
    }
}
