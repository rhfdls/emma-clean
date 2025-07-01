using Emma.Core.Models.Guardrails;

namespace Emma.Core.Interfaces;

/// <summary>
/// Service for validating AI input and output content against enterprise safety and compliance standards.
/// Implements Azure AI Content Safety integration with multi-layered guardrail validation.
/// </summary>
public interface IAIOutputGuardrailService
{
    /// <summary>
    /// Validates AI-generated output content for safety, compliance, and business logic violations.
    /// </summary>
    /// <param name="content">The AI output content to validate</param>
    /// <param name="context">Context information for validation including industry, user, and interaction details</param>
    /// <returns>Comprehensive validation result with recommendations</returns>
    Task<GuardrailResult> ValidateOutputAsync(string content, GuardrailContext context);

    /// <summary>
    /// Validates user input content before processing by AI services.
    /// </summary>
    /// <param name="content">The user input content to validate</param>
    /// <param name="context">Context information for validation</param>
    /// <returns>Validation result indicating if input is safe to process</returns>
    Task<GuardrailResult> ValidateInputAsync(string content, GuardrailContext context);

    /// <summary>
    /// Performs basic content safety check using Azure AI Content Safety.
    /// </summary>
    /// <param name="content">Content to check</param>
    /// <param name="level">Safety level threshold</param>
    /// <returns>True if content is safe, false otherwise</returns>
    Task<bool> IsContentSafeAsync(string content, ContentSafetyLevel level);

    /// <summary>
    /// Sanitizes content by removing or redacting unsafe elements.
    /// </summary>
    /// <param name="content">Content to sanitize</param>
    /// <param name="options">Sanitization options</param>
    /// <returns>Sanitized content</returns>
    Task<string> SanitizeContentAsync(string content, SanitizationOptions options);

    /// <summary>
    /// Logs guardrail violations for audit and monitoring purposes.
    /// </summary>
    /// <param name="violation">Details of the guardrail violation</param>
    Task LogGuardrailViolationAsync(GuardrailViolation violation);

    /// <summary>
    /// Generates a safe fallback response when content is blocked.
    /// </summary>
    /// <param name="result">The guardrail result that caused the block</param>
    /// <param name="context">Context for generating appropriate fallback</param>
    /// <returns>Safe fallback response</returns>
    Task<string> GenerateSafeFallbackResponseAsync(GuardrailResult result, GuardrailContext context);
}
