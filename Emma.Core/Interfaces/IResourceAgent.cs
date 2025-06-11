using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for Resource Agent that provides intelligent resource management and recommendations
    /// </summary>
    public interface IResourceAgent
    {
        /// <summary>
        /// Process agent request for resource management
        /// </summary>
        /// <param name="request">Agent request containing resource parameters</param>
        /// <param name="traceId">Optional trace ID for logging</param>
        /// <returns>Agent response with resource management results</returns>
        Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Get agent capability information
        /// </summary>
        /// <returns>Agent capability details</returns>
        Task<AgentCapability> GetCapabilityAsync();

        /// <summary>
        /// Find and recommend resources based on criteria
        /// </summary>
        /// <param name="organizationId">Organization ID for scoping</param>
        /// <param name="resourceCriteria">Criteria for resource matching</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <param name="traceId">Optional trace ID for logging</param>
        /// <param name="userOverrides">Optional user overrides for validation framework</param>
        /// <returns>Resource recommendations and analysis</returns>
        Task<AgentResponse> RecommendResourcesAsync(Guid organizationId, Dictionary<string, object> resourceCriteria, int maxResults = 10, string? traceId = null, Dictionary<string, object>? userOverrides = null);
    }
}
