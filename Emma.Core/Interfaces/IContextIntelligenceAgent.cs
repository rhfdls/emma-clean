using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for Context Intelligence Agent that provides intelligent context analysis and insights
    /// </summary>
    public interface IContextIntelligenceAgent
    {
        /// <summary>
        /// Process agent request for context intelligence analysis
        /// </summary>
        /// <param name="request">Agent request containing context and parameters</param>
        /// <param name="traceId">Optional trace ID for logging</param>
        /// <returns>Agent response with context analysis results</returns>
        Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Get agent capability information
        /// </summary>
        /// <returns>Agent capability details</returns>
        Task<AgentCapability> GetCapabilityAsync();

        /// <summary>
        /// Analyze context patterns and provide intelligent insights
        /// </summary>
        /// <param name="contactId">Contact ID for context analysis</param>
        /// <param name="organizationId">Organization ID for scoping</param>
        /// <param name="analysisType">Type of context analysis to perform</param>
        /// <param name="traceId">Optional trace ID for logging</param>
        /// <returns>Context intelligence analysis results</returns>
        Task<AgentResponse> AnalyzeContextAsync(Guid contactId, Guid organizationId, string analysisType, string? traceId = null);
    }
}
