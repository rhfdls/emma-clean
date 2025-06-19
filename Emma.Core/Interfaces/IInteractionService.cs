using Emma.Api.Dtos;
using Emma.Models.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for interaction-related business logic.
    /// </summary>
    public interface IInteractionService
    {
        /// <summary>
        /// Creates a new interaction.
        /// </summary>
        /// <param name="interaction">The interaction to create.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateInteractionAsync(Interaction interaction);

        /// <summary>
        /// Retrieves an interaction by its ID.
        /// </summary>
        /// <param name="id">The ID of the interaction to retrieve.</param>
        /// <returns>The interaction if found; otherwise, null.</returns>
        Task<Interaction?> GetInteractionByIdAsync(Guid id);

        /// <summary>
        /// Updates an existing interaction.
        /// </summary>
        /// <param name="interaction">The interaction to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateInteractionAsync(Interaction interaction);

        /// <summary>
        /// Deletes an interaction by its ID.
        /// </summary>
        /// <param name="id">The ID of the interaction to delete.</param>
        /// <returns>True if the interaction was deleted; otherwise, false.</returns>
        Task<bool> DeleteInteractionAsync(Guid id);

        /// <summary>
        /// Searches interactions based on the provided criteria.
        /// </summary>
        /// <param name="searchDto">The search criteria.</param>
        /// <returns>A paginated result of interactions matching the search criteria.</returns>
        Task<PaginatedResult<Interaction>> SearchInteractionsAsync(InteractionSearchDto searchDto);

        /// <summary>
        /// Performs a semantic search on interactions using natural language.
        /// </summary>
        /// <param name="searchDto">The semantic search criteria.</param>
        /// <returns>A list of interactions matching the semantic query.</returns>
        Task<IEnumerable<Interaction>> SemanticSearchAsync(SemanticSearchDto searchDto);

        /// <summary>
        /// Analyzes the sentiment of an interaction's content.
        /// </summary>
        /// <param name="interactionId">The ID of the interaction to analyze.</param>
        /// <returns>The updated interaction with sentiment analysis results.</returns>
        Task<Interaction> AnalyzeSentimentAsync(Guid interactionId);

        /// <summary>
        /// Extracts action items from an interaction.
        /// </summary>
        /// <param name="interactionId">The ID of the interaction to process.</param>
        /// <returns>The updated interaction with extracted action items.</returns>
        Task<Interaction> ExtractActionItemsAsync(Guid interactionId);

        /// <summary>
        /// Updates the vector embedding for an interaction.
        /// </summary>
        /// <param name="interactionId">The ID of the interaction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateVectorEmbeddingAsync(Guid interactionId);

        /// <summary>
        /// Gets similar interactions based on vector similarity.
        /// </summary>
        /// <param name="interactionId">The ID of the interaction to find similar ones for.</param>
        /// <param name="maxResults">The maximum number of similar interactions to return.</param>
        /// <returns>A list of similar interactions.</returns>
        Task<IEnumerable<Interaction>> FindSimilarInteractionsAsync(Guid interactionId, int maxResults = 5);

        /// <summary>
        /// Gets the interaction history for a contact.
        /// </summary>
        /// <param name="contactId">The ID of the contact.</param>
        /// <param name="maxResults">The maximum number of interactions to return.</param>
        /// <returns>A list of interactions for the contact, ordered by most recent.</returns>
        Task<IEnumerable<Interaction>> GetContactHistoryAsync(Guid contactId, int maxResults = 50);
    }
}
