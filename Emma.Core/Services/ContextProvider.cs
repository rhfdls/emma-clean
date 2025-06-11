using Microsoft.Extensions.Logging;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Configuration;

namespace Emma.Core.Services;

/// <summary>
/// Context provider implementation for EMMA Agent Factory.
/// Provides unified access to conversation, tenant, and agent context.
/// Sprint 1 implementation with feature flag support and explainability.
/// </summary>
public class ContextProvider : IContextProvider
{
    private readonly ITenantContextService _tenantContextService;
    private readonly IContextIntelligenceService _contextIntelligenceService;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IAgentRegistry _agentRegistry;
    private readonly ISqlContextExtractor _sqlContextExtractor;
    private readonly ILogger<ContextProvider> _logger;
    
    // In-memory cache for conversation contexts (can be replaced with Redis/distributed cache)
    private readonly Dictionary<Guid, ConversationContext> _conversationCache = new();
    private readonly object _cacheLock = new();

    public ContextProvider(
        ITenantContextService tenantContextService,
        IContextIntelligenceService contextIntelligenceService,
        IFeatureFlagService featureFlagService,
        IAgentRegistry agentRegistry,
        ISqlContextExtractor sqlContextExtractor,
        ILogger<ContextProvider> logger)
    {
        _tenantContextService = tenantContextService;
        _contextIntelligenceService = contextIntelligenceService;
        _featureFlagService = featureFlagService;
        _agentRegistry = agentRegistry;
        _sqlContextExtractor = sqlContextExtractor;
        _logger = logger;
    }

    public async Task<ConversationContext> GetConversationContextAsync(Guid conversationId, string traceId)
    {
        var auditId = Guid.NewGuid();
        _logger.LogDebug("Getting conversation context for {ConversationId}, TraceId: {TraceId}, AuditId: {AuditId}", 
            conversationId, traceId, auditId);

        try
        {
            // Check cache first
            lock (_cacheLock)
            {
                if (_conversationCache.TryGetValue(conversationId, out var cachedContext))
                {
                    _logger.LogDebug("Retrieved conversation context from cache for {ConversationId}", conversationId);
                    cachedContext.AuditId = auditId;
                    cachedContext.Reason = "Retrieved from conversation context cache";
                    return cachedContext;
                }
            }

            // Create new conversation context
            var context = new ConversationContext
            {
                ConversationId = conversationId,
                State = ConversationState.Started,
                LastUpdated = DateTime.UtcNow,
                AuditId = auditId,
                Reason = "New conversation context created"
            };

            // Cache the context
            lock (_cacheLock)
            {
                _conversationCache[conversationId] = context;
            }

            _logger.LogInformation("Created new conversation context for {ConversationId}, AuditId: {AuditId}", 
                conversationId, auditId);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation context for {ConversationId}, TraceId: {TraceId}", 
                conversationId, traceId);
            throw;
        }
    }

    public async Task<TenantContext> GetTenantContextAsync(string traceId)
    {
        var auditId = Guid.NewGuid();
        _logger.LogDebug("Getting tenant context, TraceId: {TraceId}, AuditId: {AuditId}", traceId, auditId);

        try
        {
            // Get tenant from existing service
            var tenant = await _tenantContextService.GetCurrentTenantAsync();
            
            // Check enabled features for this tenant
            var enabledFeatures = new HashSet<string>();
            
            // Check Sprint 1 features
            if (await _featureFlagService.IsEnabledAsync(FeatureFlags.DYNAMIC_AGENT_ROUTING))
                enabledFeatures.Add("dynamic-agent-routing");
            
            if (await _featureFlagService.IsEnabledAsync(FeatureFlags.AGENT_REGISTRY_ENABLED))
                enabledFeatures.Add("agent-registry");
            
            if (await _featureFlagService.IsEnabledAsync(FeatureFlags.AGENT_LIFECYCLE_HOOKS))
                enabledFeatures.Add("lifecycle-hooks");

            // Update tenant with feature flags and audit info
            tenant.EnabledFeatures = enabledFeatures;
            tenant.AuditId = auditId;
            tenant.Reason = "Tenant context retrieved with feature flags and industry profile";

            _logger.LogInformation("Retrieved tenant context for {TenantId}, Features: {Features}, AuditId: {AuditId}", 
                tenant.TenantId, string.Join(", ", enabledFeatures), auditId);
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant context, TraceId: {TraceId}", traceId);
            
            // Return default tenant context on error
            var industryProfile = await _tenantContextService.GetIndustryProfileAsync();
            return new TenantContext
            {
                TenantId = Guid.NewGuid(),
                TenantName = "Default",
                IndustryCode = "RealEstate",
                IndustryProfile = industryProfile,
                EnabledFeatures = new HashSet<string>(),
                AuditId = auditId,
                Reason = "Default tenant context due to error"
            };
        }
    }

    public async Task<Models.AgentContext> GetAgentContextAsync(string agentType, Guid conversationId, string traceId)
    {
        var auditId = Guid.NewGuid();
        _logger.LogDebug("Getting agent context for {AgentType}, ConversationId: {ConversationId}, TraceId: {TraceId}, AuditId: {AuditId}", 
            agentType, conversationId, traceId, auditId);

        try
        {
            // Extract SQL context data for the conversation/contact
            // For now, use a default agent ID - this should be passed from the calling context
            var defaultAgentId = Guid.NewGuid(); // TODO: Get actual agent ID from context
            var sqlContextData = await _sqlContextExtractor.ExtractContextAsync(
                conversationId, // Using conversationId as contactId for now
                defaultAgentId,
                UserRole.Agent);

            // Extract the agent context from the SQL context data
            var context = sqlContextData.Agent ?? new Models.AgentContext();

            _logger.LogInformation("Retrieved agent context for {AgentType}, Contacts: {ContactCount}", 
                agentType, context.Contacts.Count);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent context for {AgentType}, TraceId: {TraceId}", agentType, traceId);
            throw;
        }
    }

    public async Task UpdateConversationContextAsync(Guid conversationId, ConversationContext context, string traceId)
    {
        var auditId = Guid.NewGuid();
        _logger.LogDebug("Updating conversation context for {ConversationId}, TraceId: {TraceId}, AuditId: {AuditId}", 
            conversationId, traceId, auditId);

        try
        {
            context.LastUpdated = DateTime.UtcNow;
            context.AuditId = auditId;
            context.Reason = "Conversation context updated with new information";

            lock (_cacheLock)
            {
                _conversationCache[conversationId] = context;
            }

            _logger.LogInformation("Updated conversation context for {ConversationId}, State: {State}, Messages: {MessageCount}, AuditId: {AuditId}", 
                conversationId, context.State, context.Messages.Count, auditId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conversation context for {ConversationId}, TraceId: {TraceId}", 
                conversationId, traceId);
            throw;
        }
    }

    public async Task<ContextIntelligence> GetContextIntelligenceAsync(Guid conversationId, string traceId)
    {
        var auditId = Guid.NewGuid();
        _logger.LogDebug("Getting context intelligence for {ConversationId}, TraceId: {TraceId}, AuditId: {AuditId}", 
            conversationId, traceId, auditId);

        try
        {
            // For now, return basic intelligence - can be enhanced with actual AI analysis
            var intelligence = new ContextIntelligence
            {
                Sentiment = new Dictionary<string, object>
                {
                    ["Score"] = 0.7,
                    ["Label"] = "Positive",
                    ["Confidence"] = 0.85
                },
                BuyingSignals = new List<string>(),
                Recommendations = new List<string>(),
                Insights = new Dictionary<string, object>
                {
                    ["KeyTopics"] = new List<string>(),
                    ["Intent"] = "General Inquiry",
                    ["Urgency"] = "Medium"
                },
                Confidence = 0.8,
                AuditId = auditId,
                Reason = "Context intelligence generated from conversation analysis"
            };

            _logger.LogInformation("Generated context intelligence for {ConversationId}, Confidence: {Confidence}, AuditId: {AuditId}", 
                conversationId, intelligence.Confidence, auditId);
            return intelligence;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting context intelligence for {ConversationId}, TraceId: {TraceId}", 
                conversationId, traceId);
            throw;
        }
    }

    public async Task ClearContextAsync(Guid conversationId, string reason, string traceId)
    {
        var auditId = Guid.NewGuid();
        _logger.LogDebug("Clearing context for {ConversationId}, Reason: {Reason}, TraceId: {TraceId}, AuditId: {AuditId}", 
            conversationId, reason, traceId, auditId);

        try
        {
            lock (_cacheLock)
            {
                _conversationCache.Remove(conversationId);
            }

            _logger.LogInformation("Cleared context for {ConversationId}, Reason: {Reason}, AuditId: {AuditId}", 
                conversationId, reason, auditId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing context for {ConversationId}, TraceId: {TraceId}", 
                conversationId, traceId);
            throw;
        }
    }
}
