using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Context provider abstraction for EMMA Agent Factory.
/// Provides unified access to conversation, tenant, and agent context.
/// Sprint 1 implementation for context-aware agent operations.
/// </summary>
public interface IContextProvider
{
    /// <summary>
    /// Get conversation context for the specified conversation.
    /// </summary>
    /// <param name="conversationId">Unique conversation identifier</param>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Conversation context with history and metadata</returns>
    Task<ConversationContext> GetConversationContextAsync(Guid conversationId, string traceId);

    /// <summary>
    /// Get tenant context for the current request.
    /// </summary>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Tenant context with industry profile and settings</returns>
    Task<TenantContext> GetTenantContextAsync(string traceId);

    /// <summary>
    /// Get agent context for the specified agent.
    /// </summary>
    /// <param name="agentType">Type of agent requesting context</param>
    /// <param name="conversationId">Conversation identifier</param>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Agent-specific context and capabilities</returns>
    Task<Models.AgentContext> GetAgentContextAsync(string agentType, Guid conversationId, string traceId);

    /// <summary>
    /// Update conversation context with new information.
    /// </summary>
    /// <param name="conversationId">Conversation identifier</param>
    /// <param name="context">Updated context information</param>
    /// <param name="traceId">Trace ID for correlation</param>
    Task UpdateConversationContextAsync(Guid conversationId, ConversationContext context, string traceId);

    /// <summary>
    /// Get context intelligence for the current conversation.
    /// Includes sentiment analysis, buying signals, and recommendations.
    /// </summary>
    /// <param name="conversationId">Conversation identifier</param>
    /// <param name="traceId">Trace ID for correlation</param>
    /// <returns>Context intelligence with insights and recommendations</returns>
    Task<ContextIntelligence> GetContextIntelligenceAsync(Guid conversationId, string traceId);

    /// <summary>
    /// Clear context cache for the specified conversation.
    /// Used for privacy compliance and context refresh.
    /// </summary>
    /// <param name="conversationId">Conversation identifier</param>
    /// <param name="reason">Reason for clearing context</param>
    /// <param name="traceId">Trace ID for correlation</param>
    Task ClearContextAsync(Guid conversationId, string reason, string traceId);
}

/// <summary>
/// Conversation context containing history and metadata.
/// </summary>
public class ConversationContext
{
    /// <summary>
    /// Unique conversation identifier.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Conversation history and messages.
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Current conversation state and phase.
    /// </summary>
    public ConversationState State { get; set; }

    /// <summary>
    /// Conversation metadata and tags.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Audit ID for traceability.
    /// </summary>
    public Guid AuditId { get; set; }

    /// <summary>
    /// Reason for context creation or update.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Agent-specific context and capabilities.
/// </summary>
public class AgentContext
{
    /// <summary>
    /// Agent type identifier.
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Agent capabilities and supported operations.
    /// </summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>
    /// Agent configuration and settings.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Agent state and status information.
    /// </summary>
    public AgentState State { get; set; }

    /// <summary>
    /// Performance metrics and statistics.
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = new();

    /// <summary>
    /// Audit ID for traceability.
    /// </summary>
    public Guid AuditId { get; set; }

    /// <summary>
    /// Reason for context retrieval.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Context intelligence with insights and recommendations.
/// </summary>
public class ContextIntelligence
{
    /// <summary>
    /// Sentiment analysis results.
    /// </summary>
    public Dictionary<string, object> Sentiment { get; set; } = new();

    /// <summary>
    /// Detected buying signals and intent.
    /// </summary>
    public List<string> BuyingSignals { get; set; } = new();

    /// <summary>
    /// AI-generated recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Conversation insights and patterns.
    /// </summary>
    public Dictionary<string, object> Insights { get; set; } = new();

    /// <summary>
    /// Confidence score for the intelligence.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Audit ID for traceability.
    /// </summary>
    public Guid AuditId { get; set; }

    /// <summary>
    /// Reason for intelligence generation.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Conversation state enumeration.
/// </summary>
public enum ConversationState
{
    Started,
    Active,
    Paused,
    Completed,
    Escalated,
    Archived
}

/// <summary>
/// Agent state enumeration.
/// </summary>
public enum AgentState
{
    Initializing,
    Ready,
    Processing,
    Busy,
    Error,
    Offline
}
