using Azure.AI.ContentSafety;
using Emma.Core.Interfaces;
using Emma.Core.Models.Guardrails;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Emma.Api.Services;

/// <summary>
/// Enterprise-grade AI output guardrail service implementing Azure AI Content Safety
/// and multi-layered validation for responsible AI governance.
/// </summary>
public class AIOutputGuardrailService : IAIOutputGuardrailService
{
    private readonly ContentSafetyClient _contentSafetyClient;
    private readonly ILogger<AIOutputGuardrailService> _logger;
    private readonly TelemetryClient _telemetryClient;
    private readonly IConfiguration _configuration;

    // Industry-specific compliance patterns
    private static readonly Dictionary<IndustryType, List<string>> CompliancePatterns = new()
    {
        [IndustryType.RealEstate] = new()
        {
            @"\b(discriminat|bias|prefer|avoid)\b.*\b(race|color|religion|sex|familial|national origin|disability)\b",
            @"\b(no\s+)?(kids|children|families|pregnant)\b",
            @"\b(adults?\s+only|mature\s+adults?)\b"
        },
        [IndustryType.Finance] = new()
        {
            @"\b(guaranteed\s+returns?|risk-free|no\s+risk)\b",
            @"\b(insider\s+information|sure\s+thing)\b",
            @"\b(get\s+rich\s+quick|easy\s+money)\b"
        },
        [IndustryType.Healthcare] = new()
        {
            @"\b(cure|guaranteed\s+treatment|miracle)\b",
            @"\b(diagnos|prescrib|treat)\b.*\b(without\s+doctor|self-medicate)\b"
        }
    };

    // PII detection patterns
    private static readonly List<Regex> PIIPatterns = new()
    {
        new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled), // SSN
        new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled), // Credit Card
        new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled), // Email
        new(@"\b\(\d{3}\)\s?\d{3}-\d{4}\b", RegexOptions.Compiled), // Phone
        new(@"\b\d{1,5}\s+\w+\s+(Street|St|Avenue|Ave|Road|Rd|Drive|Dr|Lane|Ln|Boulevard|Blvd)\b", RegexOptions.Compiled) // Address
    };

    public AIOutputGuardrailService(
        ContentSafetyClient contentSafetyClient,
        ILogger<AIOutputGuardrailService> logger,
        TelemetryClient telemetryClient,
        IConfiguration configuration)
    {
        _contentSafetyClient = contentSafetyClient;
        _logger = logger;
        _telemetryClient = telemetryClient;
        _configuration = configuration;
    }

    public async Task<GuardrailResult> ValidateOutputAsync(string content, GuardrailContext context)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var validationId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("Starting guardrail validation for output. ValidationId: {ValidationId}, Industry: {Industry}", 
            validationId, context.Industry);

        try
        {
            var validationResults = new List<GuardrailCheck>();

            // 1. Azure Content Safety - Harm Categories
            var harmAnalysis = await AnalyzeHarmCategoriesAsync(content);
            validationResults.Add(new GuardrailCheck
            {
                CheckType = "HarmCategories",
                Passed = !harmAnalysis.HasViolations,
                Details = harmAnalysis.Details,
                Severity = harmAnalysis.MaxSeverity,
                ConfidenceScore = 0.95
            });

            // 2. Prompt Injection/Jailbreak Detection
            var injectionResult = await DetectPromptInjectionAsync(content);
            validationResults.Add(new GuardrailCheck
            {
                CheckType = "PromptInjection",
                Passed = !injectionResult.IsInjectionAttempt,
                Details = injectionResult.Details,
                Severity = injectionResult.IsInjectionAttempt ? GuardrailSeverity.High : GuardrailSeverity.None,
                ConfidenceScore = injectionResult.ConfidenceScore
            });

            // 3. PII Detection and Redaction
            var piiResult = await DetectAndRedactPIIAsync(content);
            validationResults.Add(new GuardrailCheck
            {
                CheckType = "PIIDetection",
                Passed = !piiResult.ContainsPII,
                Details = string.Join(", ", piiResult.DetectedEntities),
                RedactedContent = piiResult.RedactedContent,
                Severity = piiResult.ContainsPII ? GuardrailSeverity.Medium : GuardrailSeverity.None,
                ConfidenceScore = 0.90
            });

            // 4. Groundedness Detection (for RAG responses)
            if (context.HasSourceDocuments)
            {
                var groundednessResult = await ValidateGroundednessAsync(content, context.SourceDocuments);
                validationResults.Add(new GuardrailCheck
                {
                    CheckType = "Groundedness",
                    Passed = groundednessResult.IsGrounded,
                    Details = groundednessResult.Details,
                    Severity = !groundednessResult.IsGrounded ? GuardrailSeverity.Medium : GuardrailSeverity.None,
                    ConfidenceScore = groundednessResult.GroundednessScore
                });
            }

            // 5. Industry-Specific Compliance
            var complianceResult = await ValidateIndustryComplianceAsync(content, context.Industry);
            validationResults.Add(complianceResult);

            // 6. Business Logic Validation
            var businessLogicResult = await ValidateBusinessLogicAsync(content, context);
            validationResults.Add(businessLogicResult);

            stopwatch.Stop();

            // Determine overall result
            var maxSeverity = validationResults.Max(r => r.Severity);
            var hasFailures = validationResults.Any(r => !r.Passed);
            var recommendedAction = DetermineRecommendedAction(validationResults);

            var overallResult = new GuardrailResult
            {
                IsAllowed = recommendedAction == GuardrailAction.Allow || recommendedAction == GuardrailAction.Flag,
                RecommendedAction = recommendedAction,
                ValidationResults = validationResults,
                ProcessedContent = piiResult.RedactedContent,
                MaxSeverity = maxSeverity,
                ProcessingTime = stopwatch.Elapsed,
                ValidationId = validationId,
                Metadata = new Dictionary<string, object>
                {
                    ["Industry"] = context.Industry.ToString(),
                    ["ContentType"] = context.ContentType.ToString(),
                    ["HasSourceDocuments"] = context.HasSourceDocuments,
                    ["ValidationTimestamp"] = DateTime.UtcNow
                }
            };

            // Generate fallback response if needed
            if (!overallResult.IsAllowed)
            {
                overallResult.FallbackResponse = await GenerateSafeFallbackResponseAsync(overallResult, context);
            }

            // Audit all guardrail checks
            await LogGuardrailAuditAsync(new GuardrailAudit
            {
                UserId = context.UserId,
                SessionId = context.SessionId,
                ContentHash = ComputeContentHash(content),
                ValidationResult = overallResult,
                Context = context,
                ProcessingTime = stopwatch.Elapsed
            });

            // Log telemetry
            LogGuardrailTelemetry(overallResult, context);

            _logger.LogInformation("Guardrail validation completed. ValidationId: {ValidationId}, IsAllowed: {IsAllowed}, Action: {Action}, ProcessingTime: {ProcessingTime}ms",
                validationId, overallResult.IsAllowed, overallResult.RecommendedAction, stopwatch.ElapsedMilliseconds);

            return overallResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Guardrail validation failed for content hash: {ContentHash}, ValidationId: {ValidationId}", 
                ComputeContentHash(content), validationId);

            // Return safe default - block content on validation failure
            return new GuardrailResult
            {
                IsAllowed = false,
                RecommendedAction = GuardrailAction.Block,
                MaxSeverity = GuardrailSeverity.Critical,
                ProcessingTime = stopwatch.Elapsed,
                ValidationId = validationId,
                FallbackResponse = "I apologize, but I'm unable to process your request at this time due to a technical issue. Please try again later or contact support if the problem persists."
            };
        }
    }

    public async Task<GuardrailResult> ValidateInputAsync(string content, GuardrailContext context)
    {
        // For input validation, focus on injection detection and basic safety
        var validationResults = new List<GuardrailCheck>();

        // Check for prompt injection attempts
        var injectionResult = await DetectPromptInjectionAsync(content);
        validationResults.Add(new GuardrailCheck
        {
            CheckType = "InputInjection",
            Passed = !injectionResult.IsInjectionAttempt,
            Details = injectionResult.Details,
            Severity = injectionResult.IsInjectionAttempt ? GuardrailSeverity.High : GuardrailSeverity.None,
            ConfidenceScore = injectionResult.ConfidenceScore
        });

        // Basic content safety check
        var harmAnalysis = await AnalyzeHarmCategoriesAsync(content);
        validationResults.Add(new GuardrailCheck
        {
            CheckType = "InputHarmCategories",
            Passed = !harmAnalysis.HasViolations,
            Details = harmAnalysis.Details,
            Severity = harmAnalysis.MaxSeverity,
            ConfidenceScore = 0.95
        });

        var recommendedAction = DetermineRecommendedAction(validationResults);
        
        return new GuardrailResult
        {
            IsAllowed = recommendedAction == GuardrailAction.Allow || recommendedAction == GuardrailAction.Flag,
            RecommendedAction = recommendedAction,
            ValidationResults = validationResults,
            MaxSeverity = validationResults.Max(r => r.Severity),
            ValidationId = Guid.NewGuid().ToString()
        };
    }

    public async Task<bool> IsContentSafeAsync(string content, ContentSafetyLevel level)
    {
        try
        {
            var response = await _contentSafetyClient.AnalyzeTextAsync(content);
            
            var threshold = level switch
            {
                ContentSafetyLevel.Low => 6,
                ContentSafetyLevel.Medium => 4,
                ContentSafetyLevel.High => 2,
                ContentSafetyLevel.Strict => 0,
                _ => 4
            };

            return response.Value.CategoriesAnalysis.All(category => category.Severity <= threshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content safety check failed");
            return false; // Fail safe
        }
    }

    public async Task<string> SanitizeContentAsync(string content, SanitizationOptions options)
    {
        var sanitized = content;

        if (options.RedactPII)
        {
            var piiResult = await DetectAndRedactPIIAsync(content);
            sanitized = piiResult.RedactedContent;
        }

        if (options.RemoveHarmfulContent)
        {
            // Remove content flagged by Azure Content Safety
            var harmAnalysis = await AnalyzeHarmCategoriesAsync(sanitized);
            if (harmAnalysis.HasViolations)
            {
                sanitized = options.PreserveStructure 
                    ? "[Content removed for safety]" 
                    : string.Empty;
            }
        }

        return sanitized;
    }

    public async Task LogGuardrailViolationAsync(GuardrailViolation violation)
    {
        try
        {
            _logger.LogWarning("Guardrail violation detected. ViolationId: {ViolationId}, Type: {ViolationType}, Severity: {Severity}, UserId: {UserId}",
                violation.ViolationId, violation.ViolationType, violation.Severity, violation.UserId);

            // Log to Application Insights
            var telemetry = new EventTelemetry("GuardrailViolation");
            telemetry.Properties["ViolationId"] = violation.ViolationId;
            telemetry.Properties["ViolationType"] = violation.ViolationType;
            telemetry.Properties["Severity"] = violation.Severity.ToString();
            telemetry.Properties["UserId"] = violation.UserId;
            telemetry.Properties["ActionTaken"] = violation.ActionTaken.ToString();
            telemetry.Metrics["SeverityLevel"] = (double)violation.Severity;

            _telemetryClient.TrackEvent(telemetry);

            // TODO: Store in audit database for compliance reporting
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log guardrail violation");
        }
    }

    public async Task<string> GenerateSafeFallbackResponseAsync(GuardrailResult result, GuardrailContext context)
    {
        // Generate context-appropriate fallback responses
        var fallbackTemplates = context.Industry switch
        {
            IndustryType.RealEstate => new[]
            {
                "I apologize, but I cannot provide that information as it may not comply with fair housing regulations. Let me help you with property details, market insights, or other real estate questions instead.",
                "I'm unable to assist with that request to ensure compliance with real estate regulations. How can I help you with property information or market analysis?",
                "That request cannot be processed to maintain compliance with fair housing laws. I'd be happy to help with property details, pricing information, or market trends instead."
            },
            IndustryType.Finance => new[]
            {
                "I cannot provide that information as it may not meet financial compliance standards. I can help with general market information, investment education, or other financial topics instead.",
                "That request cannot be processed to ensure regulatory compliance. How can I assist you with financial planning or market insights?",
                "I'm unable to provide that information to maintain compliance with financial regulations. Let me help you with other financial topics instead."
            },
            _ => new[]
            {
                "I apologize, but I cannot process that request to ensure safety and compliance. How else can I assist you today?",
                "I'm unable to help with that particular request. Is there something else I can assist you with?",
                "That request cannot be completed at this time. Please let me know how else I can help you."
            }
        };

        var random = new Random();
        var selectedTemplate = fallbackTemplates[random.Next(fallbackTemplates.Length)];

        await Task.CompletedTask; // Placeholder for any async processing
        return selectedTemplate;
    }

    #region Private Helper Methods

    private async Task<HarmAnalysisResult> AnalyzeHarmCategoriesAsync(string content)
    {
        try
        {
            var response = await _contentSafetyClient.AnalyzeTextAsync(content);

            var detectedCategories = response.Value.CategoriesAnalysis
                .Where(c => c.Severity > 0)
                .Select(c => new HarmCategory
                {
                    Category = c.Category.ToString(),
                    Severity = (GuardrailSeverity)Math.Min((int)c.Severity, 4),
                    Score = (double)(c.Severity ?? 0)
                })
                .ToList();

            return new HarmAnalysisResult
            {
                HasViolations = detectedCategories.Any(),
                MaxSeverity = detectedCategories.Any() ? detectedCategories.Max(c => c.Severity) : GuardrailSeverity.None,
                Details = string.Join(", ", detectedCategories.Select(c => $"{c.Category}: {c.Severity}")),
                DetectedCategories = detectedCategories
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Content Safety analysis failed");
            return new HarmAnalysisResult
            {
                HasViolations = true, // Fail safe
                MaxSeverity = GuardrailSeverity.Critical,
                Details = "Content safety analysis failed"
            };
        }
    }

    private async Task<PromptInjectionResult> DetectPromptInjectionAsync(string content)
    {
        // Simple pattern-based detection for common injection attempts
        var injectionPatterns = new[]
        {
            @"ignore\s+(previous|above|all)\s+(instructions?|prompts?|rules?)",
            @"(forget|disregard)\s+(everything|all|instructions?)",
            @"act\s+as\s+(if\s+you\s+are\s+)?(?:a\s+)?(different|new|another)",
            @"pretend\s+(to\s+be|you\s+are)",
            @"(system|admin|root)\s*(prompt|mode|override)",
            @"jailbreak|break\s+out|escape\s+mode"
        };

        var content_lower = content.ToLowerInvariant();
        var detectedPatterns = new List<string>();

        foreach (var pattern in injectionPatterns)
        {
            if (Regex.IsMatch(content_lower, pattern, RegexOptions.IgnoreCase))
            {
                detectedPatterns.Add(pattern);
            }
        }

        var isInjection = detectedPatterns.Any();
        var confidence = isInjection ? Math.Min(detectedPatterns.Count * 0.3, 1.0) : 0.0;

        await Task.CompletedTask;
        
        return new PromptInjectionResult
        {
            IsInjectionAttempt = isInjection,
            ConfidenceScore = confidence,
            InjectionType = isInjection ? "Pattern-based detection" : "None",
            Details = isInjection ? $"Detected patterns: {string.Join(", ", detectedPatterns)}" : "No injection patterns detected"
        };
    }

    private async Task<PIIDetectionResult> DetectAndRedactPIIAsync(string content)
    {
        var detectedEntities = new List<string>();
        var redactedContent = content;

        foreach (var pattern in PIIPatterns)
        {
            var matches = pattern.Matches(content);
            foreach (Match match in matches)
            {
                detectedEntities.Add(match.Value);
                redactedContent = redactedContent.Replace(match.Value, "[REDACTED]");
            }
        }

        await Task.CompletedTask;

        return new PIIDetectionResult
        {
            ContainsPII = detectedEntities.Any(),
            RedactedContent = redactedContent,
            DetectedEntities = detectedEntities
        };
    }

    private async Task<GroundednessResult> ValidateGroundednessAsync(string content, List<string> sourceDocuments)
    {
        // Simplified groundedness check - in production, use Azure AI services
        var contentWords = content.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sourceWords = sourceDocuments
            .SelectMany(doc => doc.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet();

        var groundedWords = contentWords.Count(word => sourceWords.Contains(word));
        var groundednessScore = contentWords.Length > 0 ? (double)groundedWords / contentWords.Length : 0.0;

        await Task.CompletedTask;

        return new GroundednessResult
        {
            IsGrounded = groundednessScore >= 0.3, // 30% threshold
            GroundednessScore = groundednessScore,
            Details = $"Groundedness score: {groundednessScore:P2}"
        };
    }

    private async Task<GuardrailCheck> ValidateIndustryComplianceAsync(string content, IndustryType industryType)
    {
        if (!CompliancePatterns.TryGetValue(industryType, out var patterns))
        {
            return new GuardrailCheck
            {
                CheckType = "IndustryCompliance",
                Passed = true,
                Details = "No specific compliance patterns for industry",
                Severity = GuardrailSeverity.None,
                ConfidenceScore = 1.0
            };
        }

        var violations = new List<string>();
        foreach (var pattern in patterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                violations.Add(pattern);
            }
        }

        await Task.CompletedTask;

        return new GuardrailCheck
        {
            CheckType = "IndustryCompliance",
            Passed = !violations.Any(),
            Details = violations.Any() ? $"Compliance violations: {violations.Count}" : "No compliance violations detected",
            Severity = violations.Any() ? GuardrailSeverity.High : GuardrailSeverity.None,
            ConfidenceScore = 0.85
        };
    }

    private async Task<GuardrailCheck> ValidateBusinessLogicAsync(string content, GuardrailContext context)
    {
        // Business logic validation - customize based on your requirements
        var violations = new List<string>();

        // Check for inappropriate promises or guarantees
        if (Regex.IsMatch(content, @"\b(guarantee|promise|ensure|certain)\b.*\b(success|profit|results?)\b", RegexOptions.IgnoreCase))
        {
            violations.Add("Inappropriate guarantees detected");
        }

        // Check for unprofessional language
        var unprofessionalPatterns = new[] { @"\b(damn|hell|crap)\b", @"!!!" };
        foreach (var pattern in unprofessionalPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                violations.Add("Unprofessional language detected");
                break;
            }
        }

        await Task.CompletedTask;

        return new GuardrailCheck
        {
            CheckType = "BusinessLogic",
            Passed = !violations.Any(),
            Details = violations.Any() ? string.Join(", ", violations) : "No business logic violations",
            Severity = violations.Any() ? GuardrailSeverity.Medium : GuardrailSeverity.None,
            ConfidenceScore = 0.80
        };
    }

    private GuardrailAction DetermineRecommendedAction(List<GuardrailCheck> validationResults)
    {
        var maxSeverity = validationResults.Max(r => r.Severity);
        var hasFailures = validationResults.Any(r => !r.Passed);

        return maxSeverity switch
        {
            GuardrailSeverity.Critical => GuardrailAction.Block,
            GuardrailSeverity.High => GuardrailAction.Block,
            GuardrailSeverity.Medium => GuardrailAction.Redact,
            GuardrailSeverity.Low => GuardrailAction.Flag,
            _ => GuardrailAction.Allow
        };
    }

    private async Task LogGuardrailAuditAsync(GuardrailAudit audit)
    {
        try
        {
            // Log structured audit data
            _logger.LogInformation("Guardrail audit: {AuditData}", JsonSerializer.Serialize(new
            {
                audit.AuditId,
                audit.UserId,
                audit.SessionId,
                audit.ContentHash,
                ValidationResult = new
                {
                    audit.ValidationResult.IsAllowed,
                    audit.ValidationResult.RecommendedAction,
                    audit.ValidationResult.MaxSeverity,
                    CheckCount = audit.ValidationResult.ValidationResults.Count,
                    FailedChecks = audit.ValidationResult.ValidationResults.Count(r => !r.Passed)
                },
                audit.ProcessingTime.TotalMilliseconds
            }));

            // TODO: Store in dedicated audit database for compliance reporting
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log guardrail audit");
        }
    }

    private void LogGuardrailTelemetry(GuardrailResult result, GuardrailContext context)
    {
        try
        {
            var telemetry = new EventTelemetry("GuardrailValidation");
            telemetry.Properties["ValidationId"] = result.ValidationId;
            telemetry.Properties["IsAllowed"] = result.IsAllowed.ToString();
            telemetry.Properties["RecommendedAction"] = result.RecommendedAction.ToString();
            telemetry.Properties["MaxSeverity"] = result.MaxSeverity.ToString();
            telemetry.Properties["Industry"] = context.Industry.ToString();
            telemetry.Properties["ContentType"] = context.ContentType.ToString();
            
            telemetry.Metrics["ProcessingTimeMs"] = result.ProcessingTime.TotalMilliseconds;
            telemetry.Metrics["CheckCount"] = result.ValidationResults.Count;
            telemetry.Metrics["FailedChecks"] = result.ValidationResults.Count(r => !r.Passed);
            telemetry.Metrics["SeverityLevel"] = (double)result.MaxSeverity;

            _telemetryClient.TrackEvent(telemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log guardrail telemetry");
        }
    }

    private static string ComputeContentHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes)[..16]; // First 16 characters for logging
    }

    #endregion
}
