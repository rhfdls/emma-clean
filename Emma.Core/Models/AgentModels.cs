using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Emma.Core.Models
{
    /// <summary>
    /// Represents an intent classification for routing to specialized agents
    /// Following Microsoft AI Foundry best practices for intent-based routing
    /// </summary>
    public enum AgentIntent
    {
        Unknown,
        ContactManagement,
        InteractionAnalysis,
        SchedulingAndTasks,
        Communication,
        MarketIntelligence,
        GeneralInquiry,
        DataAnalysis,
        ReportGeneration,
        WorkflowAutomation,
        BusinessIntelligence,
        IntentClassification,
        ResourceManagement,
        ServiceProviderRecommendation
    }

    /// <summary>
    /// Represents the urgency level of a request
    /// </summary>
    public enum UrgencyLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Defines the scope of an agent action to determine appropriate validation intensity
    /// Essential for the three-tier validation framework performance optimization
    /// </summary>
    public enum ActionScope
    {
        /// <summary>
        /// Internal agent-to-agent coordination actions (minimal validation)
        /// High-frequency, low-risk operations for agent coordination
        /// Examples: Context enrichment, state updates, internal recommendations
        /// Validation: Schema validation, basic logging, auto-approval
        /// </summary>
        InnerWorld,
        
        /// <summary>
        /// Internal actions that significantly impact downstream external actions (moderate validation)
        /// Critical internal decisions that influence real-world outcomes
        /// Examples: Risk assessment, compliance checks, intent classification, orchestration decisions
        /// Validation: Automated risk scoring, conditional approval, structured logging
        /// </summary>
        Hybrid,
        
        /// <summary>
        /// External real-world actions that impact contacts/systems (full validation)
        /// Direct external impact requiring comprehensive validation and approval
        /// Examples: Send emails, schedule appointments, update CRM, process payments
        /// Validation: Full LLM assessment, human approval workflows, comprehensive audit trails
        /// </summary>
        RealWorld
    }

    /// <summary>
    /// Agent-to-Agent communication request following A2A protocol principles
    /// Includes versioning and trace ID for migration readiness
    /// </summary>
    public class AgentRequest
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Trace ID for correlation across entire request lifecycle
        /// Critical for observability and debugging
        /// </summary>
        [Required]
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Event version for backward compatibility during migrations
        /// </summary>
        [Required]
        public string EventVersion { get; set; } = "1.0";
        
        /// <summary>
        /// Workflow definition version for state migration
        /// </summary>
        public string? WorkflowVersion { get; set; }
        
        [Required]
        public AgentIntent Intent { get; set; }
        
        [Required]
        public string OriginalUserInput { get; set; } = string.Empty;
        
        [Required]
        public Guid ConversationId { get; set; }
        
        [Required]
        public Guid InteractionId { get; set; }
        
        /// <summary>
        /// Context data for agent processing
        /// Structured for easy serialization and A2A compatibility
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string? SourceAgentId { get; set; }
        
        public string? TargetAgentId { get; set; }
        
        public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Medium;
        
        /// <summary>
        /// Orchestration method flag for observability
        /// "custom" | "foundry_workflow" | "connected_agent"
        /// </summary>
        public string OrchestrationMethod { get; set; } = "custom";
        
        /// <summary>
        /// User context for personalization and security
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// Industry context for specialized processing
        /// </summary>
        public string? Industry { get; set; }
        
        /// <summary>
        /// Unique identifier for audit trail (Sprint 1 explainability)
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Human-readable reason for this request (Sprint 1 explainability)
        /// </summary>
        public string Reason { get; set; } = "Agent request initiated";
    }

    /// <summary>
    /// Agent-to-Agent communication response
    /// Enhanced with migration and observability features
    /// </summary>
    public class AgentResponse
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string RequestId { get; set; } = string.Empty;
        
        [Required]
        public string TraceId { get; set; } = string.Empty;
        
        [Required]
        public bool Success { get; set; }
        
        public string? Content { get; set; }
        
        /// <summary>
        /// Human-readable message from the agent
        /// </summary>
        public string? Message { get; set; }
        
        /// <summary>
        /// Type of agent that generated this response
        /// </summary>
        public string? AgentType { get; set; }
        
        /// <summary>
        /// Structured data for downstream processing
        /// A2A protocol compatible
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();
        
        /// <summary>
        /// Actions taken or recommended by the agent
        /// </summary>
        public List<string> Actions { get; set; } = new();
        
        public string? ErrorMessage { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string? AgentId { get; set; }
        
        public bool RequiresFollowUp { get; set; }
        
        public AgentIntent? NextIntent { get; set; }
        
        /// <summary>
        /// Confidence score for the response (0.0 - 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double Confidence { get; set; } = 1.0;
        
        /// <summary>
        /// Processing time in milliseconds for performance monitoring
        /// </summary>
        public long ProcessingTimeMs { get; set; }
        
        /// <summary>
        /// Orchestration method used for this response
        /// </summary>
        public string OrchestrationMethod { get; set; } = "custom";
        
        /// <summary>
        /// Unique identifier for audit trail (Sprint 1 explainability)
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Human-readable reason for this response (Sprint 1 explainability)
        /// </summary>
        public string Reason { get; set; } = "Agent response generated";
        
        /// <summary>
        /// Intent of the response for routing and processing
        /// </summary>
        public string? Intent { get; set; }
    }

    /// <summary>
    /// Intent classification result from EMMA Orchestrator
    /// Enhanced with confidence and reasoning for better routing decisions
    /// </summary>
    public class IntentClassificationResult
    {
        [Required]
        public AgentIntent Intent { get; set; }
        
        [Range(0.0, 1.0)]
        public double Confidence { get; set; }
        
        public string Reasoning { get; set; } = string.Empty;
        
        /// <summary>
        /// Extracted entities for context enrichment
        /// </summary>
        public Dictionary<string, object> ExtractedEntities { get; set; } = new();
        
        public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Medium;
        
        /// <summary>
        /// Context required for successful agent execution
        /// </summary>
        public List<string> RequiredContext { get; set; } = new();
        
        /// <summary>
        /// Alternative intents with lower confidence
        /// Useful for fallback routing
        /// </summary>
        public List<(AgentIntent Intent, double Confidence)> AlternativeIntents { get; set; } = new();
        
        public string TraceId { get; set; } = string.Empty;
        
        public DateTime ClassifiedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Unique identifier for audit trail (Sprint 1 explainability)
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Human-readable reason for this classification (Sprint 1 explainability)
        /// </summary>
        public string Reason { get; set; } = "Intent classification completed";
    }

    /// <summary>
    /// Contact context for CRM operations
    /// Enhanced with AI-driven insights and recommendations
    /// </summary>
    public class ContactContext
    {
        public Guid? ContactId { get; set; }
        
        public Guid? OrganizationId { get; set; }
        
        public string? ContactName { get; set; }
        
        public string? ContactEmail { get; set; }
        
        public string? ContactPhone { get; set; }
        
        public string? ContactStatus { get; set; }
        
        /// <summary>
        /// AI-analyzed sentiment score (-1.0 to 1.0)
        /// </summary>
        [Range(-1.0, 1.0)]
        public double? SentimentScore { get; set; }
        
        /// <summary>
        /// AI-detected buying signals from interactions
        /// </summary>
        public List<string> BuyingSignals { get; set; } = new();
        
        public UrgencyLevel UrgencyLevel { get; set; } = UrgencyLevel.Medium;
        
        /// <summary>
        /// AI-recommended next actions
        /// </summary>
        public List<string> RecommendedActions { get; set; } = new();
        
        public DateTime? LastInteraction { get; set; }
        
        /// <summary>
        /// Alias for LastInteraction to maintain compatibility
        /// </summary>
        public DateTime? LastInteractionDate => LastInteraction;
        
        /// <summary>
        /// Custom properties for industry-specific data
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = new();
        
        /// <summary>
        /// Alias for CustomProperties to maintain compatibility
        /// </summary>
        public Dictionary<string, object> AdditionalData => CustomProperties;
        
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Interaction history summary for context
        /// </summary>
        public string? InteractionSummary { get; set; }
        
        /// <summary>
        /// Predicted likelihood to close (0.0 - 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double? CloseProbability { get; set; }
    }

    /// <summary>
    /// Agent capability definition following A2A Agent Card specification
    /// Future-proof for Azure AI Foundry agent catalog integration
    /// </summary>
    public class AgentCapability
    {
        [Required]
        public string AgentId { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Display name for UI purposes
        /// </summary>
        public string? DisplayName { get; set; }
        
        /// <summary>
        /// Type of agent this capability belongs to
        /// </summary>
        public string? AgentType { get; set; }
        
        /// <summary>
        /// Version for capability evolution tracking
        /// </summary>
        [Required]
        public string Version { get; set; } = "1.0.0";
        
        public List<AgentIntent> SupportedIntents { get; set; } = new();
        
        /// <summary>
        /// Supported task types for this capability
        /// </summary>
        public List<string> SupportedTasks { get; set; } = new();
        
        /// <summary>
        /// Industries where this capability is applicable
        /// </summary>
        public List<string> RequiredIndustries { get; set; } = new();
        
        /// <summary>
        /// Whether this capability is currently available
        /// </summary>
        public bool IsAvailable { get; set; } = true;
        
        /// <summary>
        /// Required permissions for security and compliance
        /// </summary>
        public List<string> RequiredPermissions { get; set; } = new();
        
        /// <summary>
        /// Agent configuration parameters
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();
        
        public bool IsActive { get; set; } = true;
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Input/output schema for A2A compatibility
        /// </summary>
        public Dictionary<string, object> InputSchema { get; set; } = new();
        
        public Dictionary<string, object> OutputSchema { get; set; } = new();
        
        /// <summary>
        /// Supported industries for specialized routing
        /// </summary>
        public List<string> SupportedIndustries { get; set; } = new();
        
        /// <summary>
        /// Performance metrics for routing optimization
        /// </summary>
        public AgentPerformanceMetrics? PerformanceMetrics { get; set; }
    }

    /// <summary>
    /// Performance metrics for agent optimization
    /// </summary>
    public class AgentPerformanceMetrics
    {
        public string AgentId { get; set; } = string.Empty;
        
        public double AverageResponseTimeMs { get; set; }
        
        [Range(0.0, 1.0)]
        public double SuccessRate { get; set; }
        
        [Range(0.0, 1.0)]
        public double AverageConfidence { get; set; }
        
        /// <summary>
        /// Alternative name for AverageConfidence to match Services usage
        /// </summary>
        [Range(0.0, 1.0)]
        public double AverageConfidenceScore 
        { 
            get => AverageConfidence; 
            set => AverageConfidence = value; 
        }
        
        public int TotalRequests { get; set; }
        
        /// <summary>
        /// Number of successful requests
        /// </summary>
        public long SuccessfulRequests { get; set; }
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Multi-agent workflow state for complex orchestration
    /// Enhanced with versioning and migration support
    /// </summary>
    public class WorkflowState
    {
        [Required]
        public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string TraceId { get; set; } = string.Empty;
        
        /// <summary>
        /// Workflow definition version for migration compatibility
        /// </summary>
        [Required]
        public string WorkflowVersion { get; set; } = "1.0.0";
        
        [Required]
        public string CurrentState { get; set; } = "Initial";
        
        /// <summary>
        /// Current status of the workflow
        /// </summary>
        public string Status { get; set; } = "Running";
        
        /// <summary>
        /// Workflow variables for state management
        /// Structured for easy migration to Azure workflows
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new();
        
        /// <summary>
        /// Workflow execution history for debugging and replay
        /// </summary>
        public List<WorkflowStep> ExecutionHistory { get; set; } = new();
        
        /// <summary>
        /// Workflow steps for execution tracking
        /// </summary>
        public List<WorkflowStep> Steps { get; set; } = new();
        
        public List<AgentRequest> PendingRequests { get; set; } = new();
        
        public List<AgentResponse> CompletedResponses { get; set; } = new();
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When the workflow was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Error details for failed workflows
        /// </summary>
        public string? Error { get; set; }
        
        /// <summary>
        /// Orchestration method for migration tracking
        /// </summary>
        public string OrchestrationMethod { get; set; } = "custom";
        
        /// <summary>
        /// User context for the workflow
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// Industry context for specialized processing
        /// </summary>
        public string? Industry { get; set; }
    }

    /// <summary>
    /// Individual workflow step for execution tracking
    /// </summary>
    public class WorkflowStep
    {
        public string StepId { get; set; } = Guid.NewGuid().ToString();
        
        public string StepName { get; set; } = string.Empty;
        
        public string AgentId { get; set; } = string.Empty;
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        public bool IsCompleted { get; set; }
        
        public string? Result { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public Dictionary<string, object> StepData { get; set; } = new();
    }

    /// <summary>
    /// Task model for agent execution compatibility
    /// Used by ISpecializedAgent interface for task-based operations
    /// </summary>
    public class AgentTask
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Task type for routing and processing
        /// </summary>
        public string? TaskType { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public Guid ConversationId { get; set; }
        
        /// <summary>
        /// Contact ID associated with this task
        /// </summary>
        public Guid? ContactId { get; set; }
        
        public Dictionary<string, object> Context { get; set; } = new();
        
        /// <summary>
        /// Task parameters for execution
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? UserId { get; set; }
        
        public string? Industry { get; set; }
        
        public Guid? InteractionId { get; set; }
    }

    /// <summary>
    /// Represents a scheduled action that needs relevance verification before execution
    /// Mission-critical safeguard against automation failures
    /// </summary>
    public class ScheduledAction
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of action to be performed (e.g., "email", "sms", "calendar_event")
        /// </summary>
        [Required]
        public string ActionType { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Contact ID this action is associated with
        /// </summary>
        [Required]
        public Guid ContactId { get; set; }
        
        /// <summary>
        /// Organization context for the action
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }
        
        /// <summary>
        /// Agent that scheduled this action
        /// </summary>
        [Required]
        public string ScheduledByAgentId { get; set; } = string.Empty;
        
        /// <summary>
        /// When this action was originally scheduled
        /// </summary>
        public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// When this action should be executed
        /// </summary>
        [Required]
        public DateTime ExecuteAt { get; set; }
        
        /// <summary>
        /// Action parameters and context data
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        /// <summary>
        /// Relevance criteria that must be met for execution
        /// Key-value pairs defining conditions for action validity
        /// Examples: {"dealStatus": "Closed"}, {"contactEngagement": "Active"}
        /// </summary>
        public Dictionary<string, object> RelevanceCriteria { get; set; } = new();
        
        /// <summary>
        /// Current status of the scheduled action
        /// </summary>
        public ScheduledActionStatus Status { get; set; } = ScheduledActionStatus.Pending;
        
        /// <summary>
        /// Reason for suppression if action was deemed irrelevant
        /// </summary>
        public string? SuppressionReason { get; set; }
        
        /// <summary>
        /// Trace ID for correlation and debugging
        /// </summary>
        public string? TraceId { get; set; }
        
        /// <summary>
        /// Priority level for execution ordering
        /// </summary>
        public UrgencyLevel Priority { get; set; } = UrgencyLevel.Medium;
        
        /// <summary>
        /// Maximum number of relevance check attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// Current retry attempt count
        /// </summary>
        public int RetryAttempts { get; set; } = 0;
        
        /// <summary>
        /// When the last relevance check was performed
        /// </summary>
        public DateTime? LastRelevanceCheck { get; set; }
        
        /// <summary>
        /// Result of the last relevance check
        /// </summary>
        public ActionRelevanceResult? LastRelevanceResult { get; set; }

        /// <summary>
        /// Scope of this action for validation intensity determination
        /// Critical for three-tier validation framework performance optimization
        /// </summary>
        [Required]
        public ActionScope Scope { get; set; } = ActionScope.RealWorld; // Default to highest validation for safety

        /// <summary>
        /// Rationale for the scope assignment for audit traceability
        /// Helps future audits understand why this action was classified with this scope
        /// Optional but recommended for compliance and debugging
        /// </summary>
        public string? ReasonForScopeAssignment { get; set; }
        
        /// <summary>
        /// Unique identifier for audit trail (Sprint 1 explainability)
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Human-readable reason for this action (Sprint 1 explainability)
        /// </summary>
        public string Reason { get; set; } = "Scheduled action created";
    }

    /// <summary>
    /// Status of a scheduled action
    /// </summary>
    public enum ScheduledActionStatus
    {
        Pending,
        RelevanceCheckPassed,
        RelevanceCheckFailed,
        Executing,
        Completed,
        Suppressed,
        Failed,
        Expired
    }

    /// <summary>
    /// Result of an action relevance verification check
    /// </summary>
    public class ActionRelevanceResult
    {
        [Required]
        public string ActionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether the action is still relevant and should be executed
        /// </summary>
        [Required]
        public bool IsRelevant { get; set; }
        
        /// <summary>
        /// Confidence score for the relevance determination (0.0 to 1.0)
        /// </summary>
        public double ConfidenceScore { get; set; } = 1.0;
        
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// Context data used in the relevance check
        /// </summary>
        public Dictionary<string, object> ContextData { get; set; } = new();
        
        /// <summary>
        /// Specific criteria that failed (if any)
        /// </summary>
        public List<string> FailedCriteria { get; set; } = new();
        
        /// <summary>
        /// When this relevance check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Agent or service that performed the relevance check
        /// </summary>
        public string CheckedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// Trace ID for correlation
        /// </summary>
        public string? TraceId { get; set; }
        
        /// <summary>
        /// Recommended action if not relevant (reschedule, modify, cancel)
        /// </summary>
        public string? RecommendedAction { get; set; }
        
        /// <summary>
        /// Alternative actions that might be more relevant
        /// </summary>
        public List<string> AlternativeActions { get; set; } = new();
        
        /// <summary>
        /// Unique identifier for audit trail (Sprint 1 explainability)
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Human-readable reason for this relevance assessment (Sprint 1 explainability)
        /// </summary>
        public string ReasonForRelevanceAssessment { get; set; } = "Action relevance assessment completed";
        
        /// <summary>
        /// Method used for validation (LLM, RuleBased, Manual, etc.) for audit trail
        /// </summary>
        public string ValidationMethod { get; set; } = "RuleBased";
    }

    /// <summary>
    /// Request for action relevance verification
    /// </summary>
    public class ActionRelevanceRequest
    {
        [Required]
        public ScheduledAction Action { get; set; } = new();
        
        /// <summary>
        /// Current contact context for verification
        /// </summary>
        public ContactContext? CurrentContext { get; set; }
        
        /// <summary>
        /// Whether to use LLM for semantic relevance checking
        /// </summary>
        public bool UseLLMValidation { get; set; } = false;
        
        /// <summary>
        /// Additional context for relevance checking
        /// </summary>
        public Dictionary<string, object> AdditionalContext { get; set; } = new();
        
        /// <summary>
        /// User override preferences that influence validation decisions
        /// MANDATORY for audit trail, explainability, and regulatory compliance
        /// </summary>
        [Required]
        public Dictionary<string, object> UserOverrides { get; set; } = new();
        
        /// <summary>
        /// Trace ID for correlation
        /// </summary>
        public string? TraceId { get; set; }
    }

    /// <summary>
    /// Configuration for action relevance validation
    /// </summary>
    public class ActionRelevanceConfig
    {
        /// <summary>
        /// Default timeout for relevance checks (in seconds)
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Whether to enable LLM-based semantic validation
        /// </summary>
        public bool EnableLLMValidation { get; set; } = true;
        
        /// <summary>
        /// Minimum confidence score for LLM validation
        /// </summary>
        public double MinimumConfidenceScore { get; set; } = 0.7;
        
        /// <summary>
        /// Maximum age of context data before refresh (in minutes)
        /// </summary>
        public int MaxContextAgeMinutes { get; set; } = 5;
        
        /// <summary>
        /// Whether to log all relevance checks for audit purposes
        /// </summary>
        public bool EnableAuditLogging { get; set; } = true;
        
        /// <summary>
        /// Default action when relevance cannot be determined
        /// </summary>
        public string DefaultActionOnUncertainty { get; set; } = "suppress";

        /// <summary>
        /// User override operating mode for approval workflow
        /// </summary>
        public UserOverrideMode OverrideMode { get; set; } = UserOverrideMode.LLMDecision;

        /// <summary>
        /// Confidence threshold below which user approval is always required
        /// </summary>
        public double UserApprovalThreshold { get; set; } = 0.6;

        /// <summary>
        /// Action types that always require user approval regardless of confidence
        /// </summary>
        public List<string> AlwaysRequireApprovalActions { get; set; } = new()
        {
            "SEND_EMAIL", "MAKE_PHONE_CALL", "SCHEDULE_MEETING", "SEND_CONTRACT"
        };

        /// <summary>
        /// Action types that never require user approval (safe automation)
        /// </summary>
        public List<string> NeverRequireApprovalActions { get; set; } = new()
        {
            "UPDATE_CONTACT_NOTES", "LOG_INTERACTION", "SET_REMINDER"
        };

        /// <summary>
        /// Maximum time to wait for user approval (in minutes)
        /// </summary>
        public int UserApprovalTimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// Whether to allow bulk approval for similar actions
        /// </summary>
        public bool EnableBulkApproval { get; set; } = true;
    }

    /// <summary>
    /// Operating modes for user override and approval workflow
    /// </summary>
    public enum UserOverrideMode
    {
        /// <summary>
        /// Always ask for user approval before executing any action
        /// </summary>
        AlwaysAsk,

        /// <summary>
        /// Never ask for approval, execute all relevant actions automatically
        /// </summary>
        NeverAsk,

        /// <summary>
        /// Use LLM reasoning to determine when user approval is needed
        /// Based on confidence scores, action types, and context sensitivity
        /// </summary>
        LLMDecision,

        /// <summary>
        /// Ask for approval only for high-risk or sensitive actions
        /// Based on predefined action type lists
        /// </summary>
        RiskBased
    }

    /// <summary>
    /// User approval request for scheduled actions
    /// </summary>
    public class UserApprovalRequest
    {
        [Required]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public ScheduledAction Action { get; set; } = new();

        [Required]
        public ActionRelevanceResult RelevanceResult { get; set; } = new();

        /// <summary>
        /// Reason why approval is needed
        /// </summary>
        public string ApprovalReason { get; set; } = string.Empty;

        /// <summary>
        /// Suggested alternatives if user rejects the action
        /// </summary>
        public List<ScheduledAction> AlternativeActions { get; set; } = new();

        /// <summary>
        /// User ID who needs to provide approval
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Original user override preferences that led to this approval request
        /// MANDATORY for audit trail, explainability, and regulatory compliance
        /// </summary>
        public Dictionary<string, object> OriginalUserOverrides { get; set; } = new();

        /// <summary>
        /// When the approval request was created
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the approval expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Current status of the approval request
        /// </summary>
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        /// <summary>
        /// Trace ID for correlation
        /// </summary>
        public string? TraceId { get; set; }
    }

    /// <summary>
    /// User approval response
    /// </summary>
    public class UserApprovalResponse
    {
        [Required]
        public string RequestId { get; set; } = string.Empty;

        [Required]
        public ApprovalDecision Decision { get; set; }

        /// <summary>
        /// User's reason for the decision
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// If rejected, user can suggest modifications
        /// </summary>
        public Dictionary<string, object>? SuggestedModifications { get; set; }

        /// <summary>
        /// If user wants to approve similar actions in bulk
        /// </summary>
        public bool ApplyToSimilarActions { get; set; } = false;

        /// <summary>
        /// User ID who provided the response
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When the response was provided
        /// </summary>
        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Status of an approval request
    /// </summary>
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected,
        Expired,
        Modified
    }

    /// <summary>
    /// User's approval decision
    /// </summary>
    public enum ApprovalDecision
    {
        Approve,
        Reject,
        Modify,
        Defer
    }
}
