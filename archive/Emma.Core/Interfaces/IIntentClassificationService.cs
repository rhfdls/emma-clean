using Emma.Core.Models;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for intent classification service
    /// Abstracted for easy swapping when Microsoft's semantic routing matures
    /// </summary>
    public interface IIntentClassificationService
    {
        /// <summary>
        /// Classify user input to determine appropriate agent routing
        /// </summary>
        /// <param name="userInput">The user's natural language input</param>
        /// <param name="context">Optional context for better classification</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>Intent classification result with confidence and reasoning</returns>
        Task<IntentClassificationResult> ClassifyIntentAsync(
            string userInput, 
            Dictionary<string, object>? context = null,
            string? traceId = null);
        
        /// <summary>
        /// Get confidence threshold for intent routing decisions
        /// </summary>
        double GetConfidenceThreshold();
        
        /// <summary>
        /// Update classification model with feedback for continuous improvement
        /// </summary>
        Task UpdateWithFeedbackAsync(string userInput, AgentIntent actualIntent, string traceId);
    }
}
