using Emma.Core.Models;

namespace Emma.Core.Dtos;

/// <summary>
/// Represents the response from the EMMA agent.
/// </summary>
public class EmmaResponseDto
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the action to be taken.
    /// </summary>
    public EmmaAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the raw response from the AI model.
    /// </summary>
    public string? RawModelOutput { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? Error { get; set; }

    
    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }


    /// <summary>
    /// Creates a success response with the specified action.
    /// </summary>
    public static EmmaResponseDto SuccessResponse(EmmaAction action, string rawModelOutput, string correlationId)
    {
        return new EmmaResponseDto
        {
            Success = true,
            Action = action,
            RawModelOutput = rawModelOutput,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates an error response with the specified error message.
    /// </summary>
    public static EmmaResponseDto ErrorResponse(string error, string correlationId, string? rawModelOutput = null)
    {
        return new EmmaResponseDto
        {
            Success = false,
            Error = error,
            RawModelOutput = rawModelOutput,
            CorrelationId = correlationId
        };
    }
}
