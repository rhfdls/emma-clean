using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for interacting with Azure AI Foundry services
    /// </summary>
    public interface IAIFoundryService
    {
        /// <summary>
        /// Processes a message using Azure AI Foundry
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <param name="conversationId">Optional conversation ID for context</param>
        /// <returns>The AI's response</returns>
        Task<string> ProcessMessageAsync(string message, string? conversationId = null);

        /// <summary>
        /// Processes a message with additional context
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <param name="context">Additional context as a string</param>
        /// <param name="conversationId">Optional conversation ID for context</param>
        /// <returns>The AI's response</returns>
        Task<string> ProcessMessageWithContextAsync(string message, string context, string? conversationId = null);

        /// <summary>
        /// Processes an agent request with system and user prompts
        /// </summary>
        /// <param name="systemPrompt">The system prompt for agent context</param>
        /// <param name="userPrompt">The user prompt/request</param>
        /// <param name="conversationId">Optional conversation ID for context</param>
        /// <returns>The AI agent's response</returns>
        Task<string> ProcessAgentRequestAsync(string systemPrompt, string userPrompt, string? conversationId = null);

        /// <summary>
        /// Starts a new conversation
        /// </summary>
        /// <returns>A new conversation ID</returns>
        Task<string> StartNewInteractionAsync();
    }
}
