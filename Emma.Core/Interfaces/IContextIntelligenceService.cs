using Emma.Core.Models;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for AI-powered context intelligence and interaction analysis
    /// Extracts insights from agent-to-contact interactions
    /// </summary>
    public interface IContextIntelligenceService
    {
        /// <summary>
        /// Analyze interaction content for sentiment, buying signals, and urgency
        /// </summary>
        /// <param name="interactionContent">Content of the interaction to analyze</param>
        /// <param name="contactContext">Existing contact context for enrichment</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>Enhanced contact context with AI insights</returns>
        Task<ContactContext> AnalyzeInteractionAsync(
            string interactionContent, 
            ContactContext? contactContext = null,
            string? traceId = null);
        
        /// <summary>
        /// Generate recommended next actions based on interaction analysis
        /// </summary>
        /// <param name="contactContext">Contact context with interaction history</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>List of AI-recommended actions</returns>
        Task<List<string>> GenerateRecommendedActionsAsync(
            ContactContext contactContext,
            string? traceId = null);
        
        /// <summary>
        /// Predict likelihood to close based on interaction patterns
        /// </summary>
        /// <param name="contactContext">Contact context with interaction history</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>Close probability score (0.0 - 1.0)</returns>
        Task<double> PredictCloseProbabilityAsync(
            ContactContext contactContext,
            string? traceId = null);
        
        /// <summary>
        /// Extract buying signals from interaction content
        /// </summary>
        /// <param name="interactionContent">Content to analyze for buying signals</param>
        /// <param name="industry">Industry context for specialized signal detection</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>List of detected buying signals</returns>
        Task<List<string>> ExtractBuyingSignalsAsync(
            string interactionContent,
            string? industry = null,
            string? traceId = null);
        
        /// <summary>
        /// Analyze sentiment of interaction content
        /// </summary>
        /// <param name="interactionContent">Content to analyze</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>Sentiment score (-1.0 to 1.0)</returns>
        Task<double> AnalyzeSentimentAsync(
            string interactionContent,
            string? traceId = null);
        
        /// <summary>
        /// Determine urgency level based on interaction content and context
        /// </summary>
        /// <param name="interactionContent">Content to analyze</param>
        /// <param name="contactContext">Contact context for additional signals</param>
        /// <param name="traceId">Trace ID for correlation</param>
        /// <returns>Urgency level classification</returns>
        Task<UrgencyLevel> DetermineUrgencyAsync(
            string interactionContent,
            ContactContext? contactContext = null,
            string? traceId = null);
    }
}
