using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;

namespace Emma.Core.Models;

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

        // Validate Payload is provided for actions that require it
        if (Action != EmmaActionType.None && Action != EmmaActionType.Unknown && string.IsNullOrWhiteSpace(Payload))
        {
            results.Add(new ValidationResult(
                $"Payload is required for action type: {Action}",
                new[] { nameof(Payload) }));
        }

        // Validate that the action is a known type
        if (Action == EmmaActionType.Unknown)
        {
            results.Add(new ValidationResult(
                $"Unknown action type. Valid values are: {string.Join(", ", Enum.GetNames(typeof(EmmaActionType)).Where(n => n != nameof(EmmaActionType.Unknown)))}",
                new[] { nameof(Action) }));
        }

        return results;
    }

    /// <summary>
    /// Creates a new EmmaAction from JSON string with validation
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>Deserialized EmmaAction</returns>
    /// <exception cref="JsonException">Thrown when JSON deserialization fails</exception>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    public static EmmaAction FromJson(string json)
    {
        if (json == null)
            throw new JsonException("JSON input cannot be null");
            
        if (string.IsNullOrWhiteSpace(json))
            throw new JsonException("JSON input cannot be empty");

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // First, deserialize to JsonDocument to handle unknown enum values
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new JsonException("Invalid JSON format", ex);
            }

            using (document)
            {
                // Handle invalid JSON structure
                if (!document.RootElement.TryGetProperty("action", out _))
                {
                    throw new JsonException("Required property 'action' not found in JSON");
                }

                var action = document.RootElement.Deserialize<EmmaAction>(options) ??
                            throw new JsonException("Failed to deserialize JSON to EmmaAction");

                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(action);
                
                if (!Validator.TryValidateObject(action, validationContext, validationResults, true))
                {
                    var errorMessages = validationResults.Select(r => r.ErrorMessage);
                    throw new ValidationException(
                        $"Validation failed: {string.Join("; ", errorMessages)}");
                }

                return action;
            }
        }
        catch (JsonException ex) when (ex.Message.Contains("could not be converted to"))
        {
            // Handle invalid enum values with a more specific error
            if (ex.Message.Contains(nameof(EmmaActionType)))
            {
                var validValues = string.Join(", ", Enum.GetNames<EmmaActionType>()
                    .Where(n => n != nameof(EmmaActionType.Unknown)));
                throw new ValidationException(
                    $"Invalid action type. Valid values are: {validValues}");
            }
            throw;
        }
    }
}
