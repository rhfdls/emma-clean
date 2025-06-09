using Emma.Core.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// NBA (Next Best Action) Agent interface - provides intelligent recommendations
/// based on contact context and business rules
/// </summary>
public interface INbaAgent
{
    /// <summary>
    /// Analyzes contact context and recommends next best actions
    /// </summary>
    /// <param name="contactId">Contact to analyze</param>
    /// <param name="organizationId">Organization context</param>
    /// <param name="requestingAgentId">Agent making the request</param>
    /// <param name="maxRecommendations">Maximum number of recommendations to return</param>
    /// <param name="traceId">Optional trace ID for logging</param>
    /// <returns>Prioritized list of recommended actions</returns>
    Task<AgentResponse> RecommendNextBestActionsAsync(
        Guid contactId,
        Guid organizationId,
        Guid requestingAgentId,
        int maxRecommendations = 3,
        string? traceId = null);

    /// <summary>
    /// Processes a general NBA request (implements standard agent interface)
    /// </summary>
    /// <param name="request">Agent request containing context and parameters</param>
    /// <param name="traceId">Optional trace ID for logging</param>
    /// <returns>Agent response with recommendations</returns>
    Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null);

    /// <summary>
    /// Gets NBA agent capabilities and supported tasks
    /// </summary>
    /// <returns>Agent capability information</returns>
    Task<AgentCapability> GetCapabilityAsync();
}
