using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Data;
using Emma.Data.Enums;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace Emma.Api.Services;

/// <summary>
/// Service for managing NBA (Next Best Action) context data
/// Aggregates contact information, interaction history, and state for AI decision-making
/// </summary>
public class NbaContextService : INbaContextService
{
    private readonly AppDbContext _context;
    private readonly IAzureOpenAIService _azureOpenAIService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ISqlContextExtractor _sqlContextExtractor;
    private readonly ILogger<NbaContextService> _logger;

    public NbaContextService(
        AppDbContext context, 
        IAzureOpenAIService azureOpenAIService,
        IVectorSearchService vectorSearchService,
        ISqlContextExtractor sqlContextExtractor,
        ILogger<NbaContextService> logger)
    {
        _context = context;
        _azureOpenAIService = azureOpenAIService;
        _vectorSearchService = vectorSearchService;
        _sqlContextExtractor = sqlContextExtractor;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves complete NBA context for a contact
    /// </summary>
    public async Task<NbaContext> GetNbaContextAsync(
        Guid contactId, 
        Guid organizationId, 
        Guid requestingAgentId,
        int maxRecentInteractions = 5, 
        int maxRelevantInteractions = 10,
        bool includeSqlContext = true)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Retrieving NBA context for contact {ContactId} by agent {AgentId}", 
                contactId, requestingAgentId);

            // Get rolling summary
            var rollingSummary = await GetContactSummaryAsync(contactId, organizationId);
            
            // Get current contact state
            var contactState = await GetContactStateAsync(contactId, organizationId);
            
            // Get recent interactions
            var recentInteractions = await GetRecentInteractionsAsync(
                contactId, organizationId, maxRecentInteractions);
            
            // Get relevant interactions via vector search (fallback to recent for now)
            var relevantInteractions = await GetRelevantInteractionsAsync(
                contactId, organizationId, maxRelevantInteractions);
            
            // Get active contact assignments
            var activeContactAssignments = await GetActiveContactAssignmentsAsync(
                contactId, organizationId);

            // Extract SQL context data for comprehensive business intelligence
            var sqlContextIncluded = false;
            var sqlContextSecurityLevel = "";
            var sqlContextFilters = new List<string>();
            
            if (includeSqlContext)
            {
                try
                {
                    var sqlContext = await _sqlContextExtractor.ExtractContextAsync(
                        contactId, requestingAgentId, UserRole.Agent, CancellationToken.None);
                    
                    sqlContextIncluded = true;
                    sqlContextSecurityLevel = sqlContext.Security.DataClassification;
                    sqlContextFilters = sqlContext.Security.AppliedFilters;
                    
                    _logger.LogInformation("SQL context extracted for contact {ContactId} with data classification {DataClassification}", 
                        contactId, sqlContext.Security.DataClassification);
                        
                    // TODO: Use SQL context to enrich the NBA context
                    // For now, we're extracting it but not directly storing it in NbaContext
                    // This maintains the separation of concerns while enabling future integration
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract SQL context for contact {ContactId}, continuing without it", contactId);
                }
            }

            // Get total interaction count
            var totalInteractionCount = await _context.Interactions
                .CountAsync(i => i.ContactId == contactId && i.OrganizationId == organizationId);

            var context = new NbaContext
            {
                ContactId = contactId,
                OrganizationId = organizationId,
                RollingSummary = rollingSummary,
                CurrentState = contactState,
                RecentInteractions = recentInteractions,
                RelevantInteractions = relevantInteractions.Select(ri => new RelevantInteraction
                {
                    Interaction = ri.Interaction,
                    Embedding = ri.Embedding,
                    SimilarityScore = ri.SimilarityScore,
                    RelevanceReason = ri.RelevanceReason
                }).ToList(),
                ActiveContactAssignments = activeContactAssignments,
                Metadata = new NbaContextMetadata
                {
                    GeneratedAt = DateTime.UtcNow,
                    TotalInteractionCount = totalInteractionCount,
                    RelevantInteractionCount = relevantInteractions.Count,
                    ActiveAssignmentCount = activeContactAssignments.Count,
                    ContextVersion = "1.1",
                    IncludesSqlContext = sqlContextIncluded
                }
            };

            _logger.LogInformation("Retrieved NBA context for contact {ContactId} in {ElapsedMs}ms with SQL context: {SqlIncluded}", 
                contactId, stopwatch.ElapsedMilliseconds, sqlContextIncluded);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving NBA context for contact {ContactId}", contactId);
            throw;
        }
    }

    /// <summary>
    /// Gets the contact summary for a contact
    /// </summary>
    public async Task<ContactSummary?> GetContactSummaryAsync(Guid contactId, Guid organizationId)
    {
        return await _context.ContactSummaries
            .FirstOrDefaultAsync(cs => cs.ContactId == contactId && cs.OrganizationId == organizationId);
    }

    /// <summary>
    /// Gets the current contact state for a contact
    /// </summary>
    public async Task<ContactState?> GetContactStateAsync(Guid contactId, Guid organizationId)
    {
        return await _context.ContactStates
            .FirstOrDefaultAsync(cs => cs.ContactId == contactId && cs.OrganizationId == organizationId);
    }

    /// <summary>
    /// Updates the rolling summary for a contact after a new interaction
    /// </summary>
    public async Task<ContactSummary> UpdateRollingSummaryAsync(Guid contactId, Guid organizationId, Interaction interaction)
    {
        // Get existing summary or create new one
        var existingSummary = await GetContactSummaryAsync(contactId, organizationId);
        
        // Get recent interactions for context
        var recentInteractions = await _context.Interactions
            .Where(i => i.ContactId == contactId && i.OrganizationId == organizationId)
            .OrderByDescending(i => i.Timestamp)
            .Take(5)
            .ToListAsync();

        if (existingSummary == null)
        {
            // Create initial summary using AI
            var initialSummaryText = await _azureOpenAIService.UpdateRollingSummaryAsync(
                null, interaction, recentInteractions);

            existingSummary = new ContactSummary
            {
                Id = $"summary-{contactId}",
                ContactId = contactId,
                OrganizationId = organizationId,
                SummaryType = "rolling",
                SummaryText = initialSummaryText,
                InteractionCount = 1,
                EarliestInteraction = interaction.Timestamp,
                LatestInteraction = interaction.Timestamp,
                KeyMilestones = new List<string>(),
                ImportantPreferences = new Dictionary<string, object>(),
                CustomFields = new Dictionary<string, object>()
            };
        }
        else
        {
            // Update existing summary using AI
            var updatedSummaryText = await _azureOpenAIService.UpdateRollingSummaryAsync(
                existingSummary.SummaryText, interaction, recentInteractions);

            existingSummary.SummaryText = updatedSummaryText;
            existingSummary.InteractionCount++;
            existingSummary.LatestInteraction = interaction.Timestamp;
            existingSummary.LastUpdated = DateTime.UtcNow;
        }

        _context.ContactSummaries.Update(existingSummary);
        await _context.SaveChangesAsync();

        return existingSummary;
    }

    /// <summary>
    /// Updates the contact state after a new interaction
    /// </summary>
    public async Task<ContactState> UpdateContactStateAsync(
        Guid contactId, 
        Guid organizationId, 
        Interaction newInteraction)
    {
        // Get existing state or create new one
        var existingState = await GetContactStateAsync(contactId, organizationId);
        
        if (existingState == null)
        {
            existingState = new ContactState
            {
                Id = $"state-{contactId}",
                ContactId = contactId,
                OrganizationId = organizationId,
                CurrentStage = "Initial Contact",
                AssignedAgentId = newInteraction.AgentId,
                Priority = "Medium",
                PendingTasks = new List<string>(),
                OpenObjections = new List<string>(),
                ImportantDates = new Dictionary<string, DateTime>(),
                PropertyInfo = new Dictionary<string, object>(),
                FinancialInfo = new Dictionary<string, object>(),
                CustomFields = new Dictionary<string, object>()
            };
        }
        else
        {
            existingState.LastUpdated = DateTime.UtcNow;
            
            // Check if state transition should occur using AI
            var (newState, reason) = await _azureOpenAIService.SuggestStateTransitionAsync(
                existingState, newInteraction);
            
            if (!string.IsNullOrEmpty(newState))
            {
                _logger.LogInformation(
                    "State transition suggested for contact {ContactId}: {OldState} -> {NewState}. Reason: {Reason}",
                    contactId, existingState.CurrentStage, newState, reason);
                
                existingState.CurrentStage = newState;
                
                // Add transition to custom fields for audit trail
                var transitions = existingState.CustomFields.ContainsKey("stateTransitions") 
                    ? (List<object>)existingState.CustomFields["stateTransitions"]
                    : new List<object>();
                
                transitions.Add(new
                {
                    timestamp = DateTime.UtcNow,
                    fromState = existingState.CurrentStage,
                    toState = newState,
                    reason = reason,
                    interactionId = newInteraction.Id
                });
                
                existingState.CustomFields["stateTransitions"] = transitions;
            }
        }

        _context.ContactStates.Update(existingState);
        await _context.SaveChangesAsync();

        return existingState;
    }

    /// <summary>
    /// Generates and stores vector embedding for an interaction
    /// </summary>
    public async Task<InteractionEmbedding> GenerateInteractionEmbeddingAsync(Interaction interaction)
    {
        // Generate embedding using Azure OpenAI
        var embeddingVector = await _azureOpenAIService.GenerateEmbeddingAsync(
            interaction.Content ?? $"{interaction.Type} interaction");

        // Extract entities and analyze sentiment
        var extractedEntities = await _azureOpenAIService.ExtractEntitiesAsync(
            interaction.Content ?? "");
        var sentimentScore = await _azureOpenAIService.AnalyzeSentimentAsync(
            interaction.Content ?? "");

        var embedding = new InteractionEmbedding
        {
            Id = $"interaction-{interaction.Id}",
            ContactId = interaction.ContactId,
            InteractionId = interaction.Id,
            OrganizationId = interaction.OrganizationId,
            Timestamp = interaction.Timestamp,
            Type = interaction.Type,
            Summary = interaction.Content?.Substring(0, Math.Min(200, interaction.Content.Length)) ?? "",
            RawContent = interaction.Content,
            Embedding = embeddingVector,
            EmbeddingModel = "text-embedding-ada-002",
            ModelVersion = "2",
            PrivacyTags = interaction.Tags,
            ExtractedEntities = extractedEntities,
            Topics = extractedEntities.ContainsKey("topics") 
                ? (List<string>)extractedEntities["topics"] 
                : new List<string>(),
            SentimentScore = sentimentScore,
            CustomFields = new Dictionary<string, object>()
        };

        _context.InteractionEmbeddings.Add(embedding);
        await _context.SaveChangesAsync();

        return embedding;
    }

    /// <summary>
    /// Performs vector search for relevant interactions using provided query embedding
    /// </summary>
    public async Task<List<RelevantInteraction>> FindRelevantInteractionsAsync(
        float[] queryEmbedding, 
        Guid contactId, 
        Guid organizationId, 
        int maxResults = 10)
    {
        try
        {
            _logger.LogDebug("Performing vector search for contact {ContactId} with embedding of dimension {Dimension}", 
                contactId, queryEmbedding?.Length ?? 0);

            if (queryEmbedding == null || queryEmbedding.Length == 0)
            {
                _logger.LogWarning("Invalid query embedding provided for contact {ContactId}", contactId);
                return new List<RelevantInteraction>();
            }

            // Use the vector search service to find similar interactions
            var relevantInteractions = await _vectorSearchService.FindSimilarInteractionsAsync(
                queryEmbedding, contactId, organizationId, maxResults, minSimilarity: 0.6);

            _logger.LogInformation("Vector search returned {Count} relevant interactions for contact {ContactId}", 
                relevantInteractions.Count, contactId);

            return relevantInteractions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in vector search for contact {ContactId}, falling back to recent interactions", contactId);
            
            // Fallback to recent interactions on error
            var recentInteractions = await GetRecentInteractionsAsync(contactId, organizationId, maxResults);
            
            return recentInteractions.Select(i => new RelevantInteraction
            {
                Interaction = i,
                Embedding = new InteractionEmbedding(), // Placeholder for fallback
                SimilarityScore = 0.3, // Lower score for fallback
                RelevanceReason = "Recent interaction (vector search error fallback)"
            }).ToList();
        }
    }

    /// <summary>
    /// Processes a new interaction end-to-end
    /// </summary>
    public async Task ProcessNewInteractionAsync(Interaction interaction)
    {
        try
        {
            _logger.LogInformation("Processing new interaction {InteractionId} for contact {ContactId}", 
                interaction.Id, interaction.ContactId);

            // Generate and store embedding
            var embedding = await GenerateInteractionEmbeddingAsync(interaction);
            
            // Index the embedding in vector search service
            var indexSuccess = await _vectorSearchService.IndexInteractionAsync(embedding);
            if (!indexSuccess)
            {
                _logger.LogWarning("Failed to index interaction embedding {InteractionId} in vector search", interaction.Id);
            }
            
            // Update rolling summary
            await UpdateRollingSummaryAsync(
                interaction.ContactId, interaction.OrganizationId, interaction);
            
            // Update contact state
            await UpdateContactStateAsync(
                interaction.ContactId, interaction.OrganizationId, interaction);

            _logger.LogInformation("Successfully processed interaction {InteractionId}", interaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interaction {InteractionId}", interaction.Id);
            throw;
        }
    }

    private async Task<List<Interaction>> GetRecentInteractionsAsync(
        Guid contactId, 
        Guid organizationId, 
        int maxResults)
    {
        return await _context.Interactions
            .Where(i => i.ContactId == contactId && i.OrganizationId == organizationId)
            .OrderByDescending(i => i.Timestamp)
            .Take(maxResults)
            .ToListAsync();
    }

    private async Task<List<RelevantInteraction>> GetRelevantInteractionsAsync(
        Guid contactId, 
        Guid organizationId, 
        int maxResults)
    {
        try
        {
            // Get the latest contact summary to use as search context
            var contactSummary = await GetContactSummaryAsync(contactId, organizationId);
            var searchQuery = contactSummary?.SummaryText ?? "contact interactions";
            
            // Use vector search to find semantically relevant interactions
            var relevantInteractions = await _vectorSearchService.SearchInteractionsAsync(
                searchQuery, contactId, organizationId, maxResults, minSimilarity: 0.6);

            if (relevantInteractions.Any())
            {
                _logger.LogDebug("Found {Count} relevant interactions via vector search for contact {ContactId}", 
                    relevantInteractions.Count, contactId);
                return relevantInteractions;
            }
            
            // Fallback to recent interactions if no vector matches found
            _logger.LogInformation("No vector search results found, falling back to recent interactions for contact {ContactId}", contactId);
            var recentInteractions = await GetRecentInteractionsAsync(contactId, organizationId, maxResults);
            
            return recentInteractions.Select(i => new RelevantInteraction
            {
                Interaction = i,
                Embedding = new InteractionEmbedding(), // Placeholder for fallback
                SimilarityScore = 0.5, // Lower score for fallback
                RelevanceReason = "Recent interaction (fallback - no embeddings found)"
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting relevant interactions for contact {ContactId}, falling back to recent", contactId);
            
            // Final fallback to recent interactions
            var recentInteractions = await GetRecentInteractionsAsync(contactId, organizationId, maxResults);
            return recentInteractions.Select(i => new RelevantInteraction
            {
                Interaction = i,
                Embedding = new InteractionEmbedding(),
                SimilarityScore = 0.3,
                RelevanceReason = "Recent interaction (error fallback)"
            }).ToList();
        }
    }

    private async Task<List<ContactAssignment>> GetActiveContactAssignmentsAsync(
        Guid contactId, 
        Guid organizationId)
    {
        return await _context.ContactAssignments
            .Where(ca => ca.ClientContactId == contactId && 
                        ca.OrganizationId == organizationId &&
                        ca.Status == ResourceAssignmentStatus.Active)
            .ToListAsync();
    }
}
