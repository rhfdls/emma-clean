namespace Emma.Core.Models.Guardrails;

/// <summary>
/// Context information for guardrail validation including industry, user, and interaction details.
/// </summary>
public class GuardrailContext
{
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string InteractionId { get; set; } = string.Empty;
    public IndustryType Industry { get; set; } = IndustryType.General;
    public ContentType ContentType { get; set; } = ContentType.Text;
    public bool HasSourceDocuments { get; set; }
    public List<string> SourceDocuments { get; set; } = new();
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of guardrail validation with detailed findings and recommendations.
/// </summary>
public class GuardrailResult
{
    public bool IsAllowed { get; set; }
    public GuardrailAction RecommendedAction { get; set; }
    public List<GuardrailCheck> ValidationResults { get; set; } = new();
    public string? ProcessedContent { get; set; }
    public string? FallbackResponse { get; set; }
    public GuardrailSeverity MaxSeverity { get; set; } = GuardrailSeverity.None;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
    public string ValidationId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Individual guardrail check result.
/// </summary>
public class GuardrailCheck
{
    public string CheckType { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public GuardrailSeverity Severity { get; set; } = GuardrailSeverity.None;
    public string Details { get; set; } = string.Empty;
    public string? RedactedContent { get; set; }
    public double ConfidenceScore { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Guardrail violation details for audit logging.
/// </summary>
public class GuardrailViolation
{
    public string ViolationId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public GuardrailSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public GuardrailAction ActionTaken { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Azure AI Content Safety harm analysis result.
/// </summary>
public class HarmAnalysisResult
{
    public bool HasViolations { get; set; }
    public GuardrailSeverity MaxSeverity { get; set; } = GuardrailSeverity.None;
    public string Details { get; set; } = string.Empty;
    public List<HarmCategory> DetectedCategories { get; set; } = new();
}

/// <summary>
/// Detected harm category with severity score.
/// </summary>
public class HarmCategory
{
    public string Category { get; set; } = string.Empty;
    public GuardrailSeverity Severity { get; set; }
    public double Score { get; set; }
}

/// <summary>
/// PII detection and redaction result.
/// </summary>
public class PIIDetectionResult
{
    public bool ContainsPII { get; set; }
    public string RedactedContent { get; set; } = string.Empty;
    public List<string> DetectedEntities { get; set; } = new();
    public Dictionary<string, List<string>> EntityTypes { get; set; } = new();
}

/// <summary>
/// Prompt injection detection result.
/// </summary>
public class PromptInjectionResult
{
    public bool IsInjectionAttempt { get; set; }
    public double ConfidenceScore { get; set; }
    public string InjectionType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Groundedness validation result for RAG responses.
/// </summary>
public class GroundednessResult
{
    public bool IsGrounded { get; set; }
    public double GroundednessScore { get; set; }
    public List<string> UngroundedClaims { get; set; } = new();
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Content sanitization options.
/// </summary>
public class SanitizationOptions
{
    public bool RedactPII { get; set; } = true;
    public bool RemoveHarmfulContent { get; set; } = true;
    public bool PreserveStructure { get; set; } = true;
    public string RedactionPlaceholder { get; set; } = "[REDACTED]";
    public List<string> AllowedEntities { get; set; } = new();
}

/// <summary>
/// Audit record for guardrail validation.
/// </summary>
public class GuardrailAudit
{
    public string AuditId { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public GuardrailResult ValidationResult { get; set; } = new();
    public GuardrailContext Context { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Supported industry types for compliance validation.
/// </summary>
public enum IndustryType
{
    General,
    RealEstate,
    Healthcare,
    Finance,
    Legal,
    Education,
    Government
}

/// <summary>
/// Content types for specialized validation.
/// </summary>
public enum ContentType
{
    Text,
    Email,
    Document,
    Code,
    Query,
    Response
}

/// <summary>
/// Guardrail severity levels.
/// </summary>
public enum GuardrailSeverity
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Recommended actions for guardrail violations.
/// </summary>
public enum GuardrailAction
{
    Allow,
    Flag,
    Redact,
    Block,
    Escalate
}

/// <summary>
/// Content safety levels for Azure AI Content Safety.
/// </summary>
public enum ContentSafetyLevel
{
    Low,
    Medium,
    High,
    Strict
}
