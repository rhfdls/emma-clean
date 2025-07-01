using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Options;
using Emma.Models.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Emma.Api.Dtos;
using Emma.Api.Models;

namespace Emma.Core.Services
{
    /// <summary>
    /// Service for managing interactions with AI-powered features.
    /// </summary>
    public class InteractionService : IInteractionService
    {
        private readonly ILogger<InteractionService> _logger;
        private readonly IInteractionRepository _repository;
        private readonly IContactRepository _contactRepository;
        private readonly IUserRepository _userRepository;
        private readonly AiOptions _aiOptions;
        private readonly IOptions<InteractionOptions> _interactionOptions;

        public InteractionService(
            ILogger<InteractionService> logger,
            IInteractionRepository repository,
            IContactRepository contactRepository,
            IUserRepository userRepository,
            IOptions<AiOptions> aiOptions,
            IOptions<InteractionOptions> interactionOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _aiOptions = aiOptions?.Value ?? throw new ArgumentNullException(nameof(aiOptions));
            _interactionOptions = interactionOptions ?? throw new ArgumentNullException(nameof(interactionOptions));
        }

        public async Task CreateInteractionAsync(Interaction interaction)
        {
            if (interaction == null)
                throw new ArgumentNullException(nameof(interaction));

            // Set default values if not provided
            interaction.CreatedAt = interaction.CreatedAt == default ? DateTime.UtcNow : interaction.CreatedAt;
            interaction.UpdatedAt = DateTime.UtcNow;
            interaction.Version = 1;

            // Vector embedding for semantic search is currently disabled due to missing dependencies.
            // TODO: Re-implement using new embedding infrastructure if needed.

            // Save to database
            await _repository.AddAsync(interaction);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Created interaction {InteractionId} of type {Type} for contact {ContactId}", 
                interaction.Id, interaction.Type, interaction.ContactId);
        }

        public async Task<Interaction?> GetInteractionByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task UpdateInteractionAsync(Interaction interaction)
        {
            if (interaction == null)
                throw new ArgumentNullException(nameof(interaction));

            var existingInteraction = await _repository.GetByIdAsync(interaction.Id);
            if (existingInteraction == null)
                throw new KeyNotFoundException($"Interaction with ID {interaction.Id} not found");

            // Update vector embedding if content has changed
            if (existingInteraction.Content != interaction.Content && !string.IsNullOrWhiteSpace(interaction.Content))
            {
                await UpdateVectorEmbeddingAsync(interaction);
            }

            // Update tracking fields
            interaction.UpdatedAt = DateTime.UtcNow;
            interaction.Version++;

            _repository.Update(interaction);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Updated interaction {InteractionId}", interaction.Id);
        }

        public async Task<bool> DeleteInteractionAsync(Guid id)
        {
            var interaction = await _repository.GetByIdAsync(id);
            if (interaction == null)
                return false;

            // Soft delete by default, or hard delete based on configuration
            if (_interactionOptions.Value.SoftDelete)
            {
                interaction.IsDeleted = true;
                interaction.DeletedAt = DateTime.UtcNow;
                _repository.Update(interaction);
            }
            else
            {
                _repository.Remove(interaction);
            }

            await _repository.SaveChangesAsync();
            _logger.LogInformation("Deleted interaction {InteractionId}", id);
            return true;
        }

        public async Task<PaginatedResult<Interaction>> SearchInteractionsAsync(InteractionSearchDto searchDto)
        {
            var query = _repository.GetAll();

            // Apply filters
            if (searchDto.ContactId.HasValue)
                query = query.Where(i => i.ContactId == searchDto.ContactId);

            if (searchDto.AssignedToId.HasValue)
                query = query.Where(i => i.AssignedToId == searchDto.AssignedToId);

            if (searchDto.Types?.Count > 0)
                query = query.Where(i => searchDto.Types.Contains(i.Type));

            if (searchDto.Statuses?.Count > 0)
                query = query.Where(i => searchDto.Statuses.Contains(i.Status));

            if (searchDto.Priorities?.Count > 0)
                query = query.Where(i => searchDto.Priorities.Contains(i.Priority));

            if (searchDto.Channels?.Count > 0)
                query = query.Where(i => searchDto.Channels.Contains(i.Channel));

            if (searchDto.PrivacyLevels?.Count > 0)
                query = query.Where(i => searchDto.PrivacyLevels.Contains(i.PrivacyLevel));

            if (searchDto.ConfidentialityLevels?.Count > 0)
                query = query.Where(i => searchDto.ConfidentialityLevels.Contains(i.Confidentiality));

            if (searchDto.Tags?.Count > 0)
                query = query.Where(i => i.Tags.Any(tag => searchDto.Tags.Contains(tag)));

            if (searchDto.FollowUpRequired.HasValue)
                query = query.Where(i => i.FollowUpRequired == searchDto.FollowUpRequired);

            if (searchDto.IsStarred.HasValue)
                query = query.Where(i => i.IsStarred == searchDto.IsStarred);

            if (searchDto.IsRead.HasValue)
                query = query.Where(i => i.IsRead == searchDto.IsRead);

            if (searchDto.StartDate.HasValue)
                query = query.Where(i => i.CreatedAt >= searchDto.StartDate.Value);

            if (searchDto.EndDate.HasValue)
                query = query.Where(i => i.CreatedAt <= searchDto.EndDate.Value);

            if (searchDto.FollowUpByStart.HasValue)
                query = query.Where(i => i.FollowUpBy >= searchDto.FollowUpByStart.Value);

            if (searchDto.FollowUpByEnd.HasValue)
                query = query.Where(i => i.FollowUpBy <= searchDto.FollowUpByEnd.Value);

            // Apply search term if provided
            if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
            {
                var searchTerm = searchDto.SearchTerm.ToLower();
                query = query.Where(i =>
                    i.Subject != null && i.Subject.ToLower().Contains(searchTerm) ||
                    i.Content != null && i.Content.ToLower().Contains(searchTerm) ||
                    i.Summary != null && i.Summary.ToLower().Contains(searchTerm) ||
                    i.Tags != null && i.Tags.Any(t => t.ToLower().Contains(searchTerm)));
            }

            // Get total count for pagination
            var totalCount = await _repository.CountAsync(query);

            // Apply sorting
            query = ApplySorting(query, searchDto.SortBy, searchDto.SortDirection);

            // Apply pagination
            query = query
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize);

            var items = await _repository.ToListAsync(query);

            return new PaginatedResult<Interaction>(
                items,
                searchDto.PageNumber,
                searchDto.PageSize,
                totalCount);
        }

        public async Task<IEnumerable<Interaction>> SemanticSearchAsync(SemanticSearchDto searchDto)
        {
            if (string.IsNullOrWhiteSpace(searchDto.Query))
                throw new ArgumentException("Search query is required", nameof(searchDto.Query));

            // Generate embedding for the search query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchDto.Query);
            
            // Search in the vector store
            var searchResults = await _memoryStore.GetNearestMatchesAsync(
                collectionName: _aiOptions.InteractionsCollection,
                embedding: queryEmbedding,
                limit: searchDto.MaxResults,
                minRelevanceScore: (float)searchDto.MinRelevanceScore);

            // Get the interaction IDs from search results
            var interactionIds = searchResults.Select(r => Guid.Parse(r.Metadata.Id)).ToList();
            
            // Retrieve the full interaction objects
            var interactions = await _repository.GetByIdsAsync(interactionIds);

            // Apply additional filters if specified
            if (searchDto.ContactId.HasValue)
                interactions = interactions.Where(i => i.ContactId == searchDto.ContactId);

            if (searchDto.AssignedToId.HasValue)
                interactions = interactions.Where(i => i.AssignedToId == searchDto.AssignedToId);

            if (searchDto.Types?.Count > 0)
                interactions = interactions.Where(i => searchDto.Types.Contains(i.Type));

            if (searchDto.Tags?.Count > 0)
                interactions = interactions.Where(i => i.Tags?.Any(t => searchDto.Tags.Contains(t)) == true);

            if (searchDto.StartDate.HasValue)
                interactions = interactions.Where(i => i.CreatedAt >= searchDto.StartDate.Value);

            if (searchDto.EndDate.HasValue)
                interactions = interactions.Where(i => i.CreatedAt <= searchDto.EndDate.Value);

            if (!string.IsNullOrEmpty(searchDto.PrivacyLevel))
                interactions = interactions.Where(i => i.PrivacyLevel == searchDto.PrivacyLevel);

            if (!string.IsNullOrEmpty(searchDto.Confidentiality))
                interactions = interactions.Where(i => i.Confidentiality == searchDto.Confidentiality);

            return interactions.Take(searchDto.MaxResults);
        }

        public async Task<Interaction> AnalyzeSentimentAsync(Guid interactionId)
        {
            var interaction = await _repository.GetByIdAsync(interactionId);
            if (interaction == null)
                throw new KeyNotFoundException($"Interaction with ID {interactionId} not found");

            if (string.IsNullOrWhiteSpace(interaction.Content))
                return interaction;

            try
            {
                // Use Semantic Kernel to analyze sentiment
                var sentimentFunction = _kernel.Plugins.GetFunction("SentimentAnalysis", "AnalyzeSentiment");
                var result = await _kernel.InvokeAsync<SentimentAnalysisResult>(sentimentFunction, new()
                {
                    ["text"] = interaction.Content
                });

                // Update interaction with sentiment analysis results
                interaction.SentimentScore = result.Score;
                interaction.SentimentLabel = result.Label;
                interaction.AiMetadata ??= new Dictionary<string, object>();
                interaction.AiMetadata["sentimentAnalysis"] = new
                {
                    model = result.Model,
                    timestamp = DateTime.UtcNow,
                    confidence = result.Confidence
                };

                await UpdateInteractionAsync(interaction);
                return interaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment for interaction {InteractionId}", interactionId);
                throw new ApplicationException("Failed to analyze sentiment", ex);
            }
        }

        public async Task<Interaction> ExtractActionItemsAsync(Guid interactionId)
        {
            var interaction = await _repository.GetByIdAsync(interactionId);
            if (interaction == null)
                throw new KeyNotFoundException($"Interaction with ID {interactionId} not found");

            if (string.IsNullOrWhiteSpace(interaction.Content))
                return interaction;

            try
            {
                // Use Semantic Kernel to extract action items
                var actionItemFunction = _kernel.Plugins.GetFunction("ActionItemExtraction", "ExtractActionItems");
                var result = await _kernel.InvokeAsync<ActionItemExtractionResult>(actionItemFunction, new()
                {
                    ["text"] = interaction.Content,
                    ["context"] = $"Interaction Type: {interaction.Type}, Contact ID: {interaction.ContactId}"
                });

                // Update interaction with extracted action items
                interaction.ActionItems = result.Items.Select(ai => new ActionItem
                {
                    Id = Guid.NewGuid(),
                    Description = ai.Description,
                    Status = ai.Status ?? "pending",
                    Priority = ai.Priority ?? "normal",
                    DueDate = ai.DueDate,
                    AssignedToId = ai.AssignedToId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = interaction.CreatedById,
                    InteractionId = interaction.Id
                }).ToList();

                interaction.AiMetadata ??= new Dictionary<string, object>();
                interaction.AiMetadata["actionItemExtraction"] = new
                {
                    model = result.Model,
                    timestamp = DateTime.UtcNow,
                    itemsExtracted = interaction.ActionItems.Count
                };

                await UpdateInteractionAsync(interaction);
                return interaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting action items from interaction {InteractionId}", interactionId);
                throw new ApplicationException("Failed to extract action items", ex);
            }
        }

        public async Task UpdateVectorEmbeddingAsync(Guid interactionId)
        {
            var interaction = await _repository.GetByIdAsync(interactionId);
            if (interaction == null)
                throw new KeyNotFoundException($"Interaction with ID {interactionId} not found");

            await UpdateVectorEmbeddingAsync(interaction);
        }

        public async Task<IEnumerable<Interaction>> FindSimilarInteractionsAsync(Guid interactionId, int maxResults = 5)
        {
            var interaction = await _repository.GetByIdAsync(interactionId);
            if (interaction == null)
                throw new KeyNotFoundException($"Interaction with ID {interactionId} not found");

            if (interaction.VectorEmbedding == null || !interaction.VectorEmbedding.Any())
            {
                if (string.IsNullOrWhiteSpace(interaction.Content))
                    return Enumerable.Empty<Interaction>();

                await UpdateVectorEmbeddingAsync(interaction);
            }


            // Search for similar interactions in the vector store
            var searchResults = await _memoryStore.GetNearestMatchesAsync(
                collectionName: _aiOptions.InteractionsCollection,
                embedding: interaction.VectorEmbedding!,
                limit: maxResults + 1, // +1 to exclude self
                minRelevanceScore: 0.7f);

            // Filter out the current interaction and get the top N results
            var similarIds = searchResults
                .Where(r => r.Metadata.Id != interactionId.ToString())
                .Take(maxResults)
                .Select(r => Guid.Parse(r.Metadata.Id))
                .ToList();

            return await _repository.GetByIdsAsync(similarIds);
        }

        public async Task<IEnumerable<Interaction>> GetContactHistoryAsync(Guid contactId, int maxResults = 50)
        {
            return await _repository.GetByContactIdAsync(contactId, maxResults);
        }

        #region Private Methods

        private async Task UpdateVectorEmbeddingAsync(Interaction interaction)
        {
            if (string.IsNullOrWhiteSpace(interaction.Content))
                return;

            try
            {
                // Generate embedding for the interaction content
                var textToEmbed = GenerateEmbeddingText(interaction);
                var embedding = await _embeddingService.GenerateEmbeddingAsync(textToEmbed);
                
                interaction.VectorEmbedding = embedding;
                interaction.EmbeddingUpdatedAt = DateTime.UtcNow;

                // Update the interaction in the database
                _repository.Update(interaction);
                await _repository.SaveChangesAsync();

                // Update the vector store
                await _memoryStore.UpsertAsync(
                    collectionName: _aiOptions.InteractionsCollection,
                    record: new MemoryRecord(
                        new MemoryRecordMetadata(
                            isReference: false,
                            id: interaction.Id.ToString(),
                            text: interaction.Content,
                            description: interaction.Summary ?? string.Empty,
                            externalSourceName: "EMMA",
                            additionalMetadata: null),
                        embedding,
                        key: null,
                        timestamp: null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vector embedding for interaction {InteractionId}", interaction.Id);
                // Don't fail the operation if embedding update fails
            }
        }

        private string GenerateEmbeddingText(Interaction interaction)
        {
            // Create a comprehensive text representation for embedding
            var sb = new System.Text.StringBuilder();
            
            if (!string.IsNullOrEmpty(interaction.Subject))
                sb.AppendLine(interaction.Subject);
                
            if (!string.IsNullOrEmpty(interaction.Content))
                sb.AppendLine(interaction.Content);
                
            if (!string.IsNullOrEmpty(interaction.Summary))
                sb.AppendLine(interaction.Summary);
                
            if (interaction.Tags?.Count > 0)
                sb.AppendLine("Tags: " + string.Join(", ", interaction.Tags));
                
            if (interaction.Participants?.Count > 0)
            {
                sb.AppendLine("Participants: ");
                foreach (var participant in interaction.Participants)
                {
                    sb.AppendLine($"- {participant.Name} ({participant.Role}): {participant.Email} {participant.Phone}");
                }
            }

            if (interaction.RelatedEntities?.Count > 0)
            {
                sb.AppendLine("Related Entities: ");
                foreach (var entity in interaction.RelatedEntities)
                {
                    sb.AppendLine($"- {entity.Type}: {entity.Name} ({entity.Role})");
                }
            }

            return sb.ToString();
        }

        private IQueryable<Interaction> ApplySorting(IQueryable<Interaction> query, string? sortBy, string? sortDirection)
        {
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortBy?.ToLower() ?? "createdat") switch
            {
                "updatedat" => isDescending 
                    ? query.OrderByDescending(i => i.UpdatedAt) 
                    : query.OrderBy(i => i.UpdatedAt),
                "priority" => isDescending
                    ? query.OrderByDescending(i => i.Priority)
                    : query.OrderBy(i => i.Priority),
                "status" => isDescending
                    ? query.OrderByDescending(i => i.Status)
                    : query.OrderBy(i => i.Status),
                "type" => isDescending
                    ? query.OrderByDescending(i => i.Type)
                    : query.OrderBy(i => i.Type),
                "channel" => isDescending
                    ? query.OrderByDescending(i => i.Channel)
                    : query.OrderBy(i => i.Channel),
                _ => isDescending
                    ? query.OrderByDescending(i => i.CreatedAt)
                    : query.OrderBy(i => i.CreatedAt)
            };
        }


        #endregion
    }

    // Helper classes for AI operations
    public class SentimentAnalysisResult
    {
        public string Label { get; set; } = string.Empty;
        public float Score { get; set; }
        public float Confidence { get; set; }
        public string Model { get; set; } = string.Empty;
    }

    public class ActionItemExtractionResult
    {
        public List<ActionItemDto> Items { get; set; } = new();
        public string Model { get; set; } = string.Empty;
    }

    public class ActionItemDto
    {
        public string Description { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssignedToId { get; set; }
    }
}
