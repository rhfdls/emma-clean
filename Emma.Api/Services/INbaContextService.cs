using Emma.Data.Models;

namespace Emma.Api.Services;

/// <summary>
/// Interface for NBA (Next Best Action) context management service
/// </summary>
public interface INbaContextService
{
    /// <summary>
    /// Retrieves complete NBA context for a client including summary, state, and relevant interactions
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="maxRecentInteractions">Maximum number of recent interactions to include</param>
    /// <param name="maxRelevantInteractions">Maximum number of relevant interactions to include</param>
    /// <returns>Complete NBA context</returns>
    Task<NbaContext> GetNbaContextAsync(
        Guid contactId, 
        Guid organizationId, 
        int maxRecentInteractions = 5, 
        int maxRelevantInteractions = 10);

    /// <summary>
    /// Gets the client summary for a contact
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Client summary or null if not found</returns>
    Task<ClientSummary?> GetClientSummaryAsync(Guid contactId, Guid organizationId);

    /// <summary>
    /// Gets the current client state for a contact
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Client state or null if not found</returns>
    Task<ClientState?> GetClientStateAsync(Guid contactId, Guid organizationId);

    /// <summary>
    /// Updates the rolling summary for a client after a new interaction
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="newInteraction">New interaction to incorporate</param>
    /// <returns>Updated client summary</returns>
    Task<ClientSummary> UpdateRollingSummaryAsync(
        Guid contactId, 
        Guid organizationId, 
        Interaction newInteraction);

    /// <summary>
    /// Updates the client state after a new interaction
    /// </summary>
    /// <param name="contactId">Contact ID</param>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="newInteraction">New interaction to process</param>
    /// <returns>Updated client state</returns>
    Task<ClientState> UpdateClientStateAsync(
        Guid contactId, 
        Guid organizationId, 
        Interaction newInteraction);

    /// <summary>
    /// Generates and stores vector embedding for an interaction
    /// </summary>
    /// <param name="interaction">Interaction to generate embedding for</param>
    /// <returns>Generated interaction embedding</returns>
    Task<InteractionEmbedding> GenerateInteractionEmbeddingAsync(Interaction interaction);

    /// <summary>
    /// Performs vector search for relevant interactions
    /// </summary>
    /// <param name="queryEmbedding">Query embedding vector</param>
    /// <param name="contactId">Contact ID to filter results</param>
    /// <param name="organizationId">Organization ID to filter results</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of relevant interactions with similarity scores</returns>
    Task<List<RelevantInteraction>> FindRelevantInteractionsAsync(
        float[] queryEmbedding, 
        Guid contactId, 
        Guid organizationId, 
        int maxResults = 10);

    /// <summary>
    /// Processes a new interaction end-to-end (embedding, summary update, state update)
    /// </summary>
    /// <param name="interaction">Interaction to process</param>
    /// <returns>Task representing the async operation</returns>
    Task ProcessNewInteractionAsync(Interaction interaction);
}
