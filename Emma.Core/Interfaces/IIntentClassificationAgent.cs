using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for Intent Classification Agent that provides intelligent intent recognition and classification
    /// </summary>
    public interface IIntentClassificationAgent
    {
        /// <summary>
        /// Process agent request for intent classification
        /// </summary>
        /// <param name="request">Agent request containing user input and context</param>
        /// <param name="traceId">Optional trace ID for logging</param>
        /// <returns>Agent response with classified intent and confidence</returns>
        Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null);

        /// <summary>
        /// Get agent capability information
        /// </summary>
        /// <returns>Agent capability details</returns>
        Task<AgentCapability> GetCapabilityAsync();

        /// <summary>
        /// Classify user intent from input text
        /// </summary>
        /// <param name="userInput">User input text to classify</param>
        /// <param name="conversationContext">Optional conversation context</param>
        /// <param name="traceId">Optional trace ID for logging</param>
        /// <returns>Classification results with intent and confidence</returns>
        Task<AgentResponse> ClassifyIntentAsync(string userInput, Dictionary<string, object>? conversationContext = null, string? traceId = null);
    }
}
