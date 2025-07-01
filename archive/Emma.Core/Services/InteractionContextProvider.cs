using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Emma.Core.Interfaces;
using Emma.Core.Models.InteractionContext;
using Emma.Core.Options;

namespace Emma.Core.Services
{
    /// <summary>
    /// Provides access to and manages interaction context data with caching and orchestration integration.
    /// </summary>
    public class InteractionContextProvider : IInteractionContextProvider, IDisposable
    {
        private readonly ILogger<InteractionContextProvider> _logger;
        private readonly IMemoryCache _cache;
        private readonly IAgentRegistry _agentRegistry;
        private readonly INbaContextService _nbaContextService;
        private readonly ITenantContextService _tenantContextService;
        private readonly IOrchestrationLogger _orchestrationLogger;
        private readonly InteractionContextOptions _options;
        private bool _disposed;

        public InteractionContextProvider(
            ILogger<InteractionContextProvider> logger,
            IMemoryCache cache,
            IAgentRegistry agentRegistry,
            INbaContextService nbaContextService,
            ITenantContextService tenantContextService,
            IOrchestrationLogger orchestrationLogger,
            IOptions<InteractionContextOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _nbaContextService = nbaContextService ?? throw new ArgumentNullException(nameof(nbaContextService));
            _tenantContextService = tenantContextService ?? throw new ArgumentNullException(nameof(tenantContextService));
            _orchestrationLogger = orchestrationLogger ?? throw new ArgumentNullException(nameof(orchestrationLogger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        // Core Interaction Context Methods

        public async Task<InteractionContext> GetInteractionContextAsync(Guid interactionId, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogDebug("[{TraceId}] Getting interaction context for {InteractionId}", traceId, interactionId);

            return await _cache.GetOrCreateAsync(
                GetCacheKey(nameof(InteractionContext), interactionId.ToString()),
                async entry =>
                {
                    entry.SlidingExpiration = _options.CacheExpiration;
                    
                    // In a real implementation, this would load from a persistent store
                    var context = new InteractionContext
                    {
                        InteractionId = interactionId,
                        State = "Active",
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    await _orchestrationLogger.LogActivityAsync(
                        "InteractionContextRetrieved",
                        interactionId.ToString(),
                        $"Retrieved interaction context for {interactionId}",
                        context);

                    return context;
                }) ?? new InteractionContext { InteractionId = interactionId };
        }

        public async Task<InteractionContext> UpdateInteractionStateAsync(
            Guid interactionId,
            string state,
            string? reason = null,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogInformation(
                "[{TraceId}] Updating interaction {InteractionId} state to {State}",
                traceId, interactionId, state);

            var context = await GetInteractionContextAsync(interactionId, traceId);
            context.State = state;
            context.UpdatedAt = DateTimeOffset.UtcNow;
            context.Reason = reason ?? context.Reason;

            await _orchestrationLogger.LogActivityAsync(
                "InteractionStateUpdated",
                interactionId.ToString(),
                $"Updated interaction state to {state}",
                new { PreviousState = context.State, NewState = state, Reason = reason });

            // Update cache
            _cache.Set(GetCacheKey(nameof(InteractionContext), interactionId.ToString()), context, new MemoryCacheEntryOptions
            {
                SlidingExpiration = _options.CacheExpiration
            });

            return context;
        }

        public async Task<InteractionContext> AddInteractionMessageAsync(
            Guid interactionId,
            InteractionMessage message,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogDebug(
                "[{TraceId}] Adding message to interaction {InteractionId} from {SenderType} {SenderId}",
                traceId, interactionId, message.SenderType, message.SenderId);

            var context = await GetInteractionContextAsync(interactionId, traceId);
            message.Timestamp = DateTimeOffset.UtcNow;
            context.Messages.Add(message);
            context.UpdatedAt = DateTimeOffset.UtcNow;

            // Update cache
            _cache.Set(GetCacheKey(nameof(InteractionContext), interactionId.ToString()), context, new MemoryCacheEntryOptions
            {
                SlidingExpiration = _options.CacheExpiration
            });

            return context;
        }

        // Agent Context Methods

        public async Task<AgentContext> GetAgentContextAsync(
            string agentType,
            Guid interactionId,
            string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogDebug(
                "[{TraceId}] Getting agent context for {AgentType} in interaction {InteractionId}",
                traceId, agentType, interactionId);

            var cacheKey = GetCacheKey($"AgentContext_{agentType}", interactionId.ToString());
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = _options.CacheExpiration;
                
                var agent = await _agentRegistry.GetAgentAsync(agentType);
                if (agent == null)
                {
                    throw new InvalidOperationException($"Agent of type {agentType} not found");
                }


                var context = new AgentContext
                {
                    AgentId = agent.Id,
                    AgentType = agentType,
                    DisplayName = agent.DisplayName,
                    State = "Active",
                    LastActiveAt = DateTimeOffset.UtcNow,
                    InteractionId = interactionId,
                    OrganizationId = (await _tenantContextService.GetTenantContextAsync())?.Id ?? Guid.Empty
                };

                await _orchestrationLogger.LogActivityAsync(
                    "AgentContextRetrieved",
                    interactionId.ToString(),
                    $"Retrieved agent context for {agentType}",
                    context);

                return context;
            }) ?? throw new InvalidOperationException("Failed to create agent context");
        }

        public async Task<AgentContext> UpdateAgentContextAsync(AgentContext context, string? traceId = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            traceId ??= Guid.NewGuid().ToString();

            _logger.LogInformation(
                "[{TraceId}] Updating agent context for {AgentType} in interaction {InteractionId}",
                traceId, context.AgentType, context.InteractionId);

            context.LastActiveAt = DateTimeOffset.UtcNow;
            
            var cacheKey = GetCacheKey($"AgentContext_{context.AgentType}", context.InteractionId.ToString());
            _cache.Set(cacheKey, context, new MemoryCacheEntryOptions
            {
                SlidingExpiration = _options.CacheExpiration
            });

            await _orchestrationLogger.LogActivityAsync(
                "AgentContextUpdated",
                context.InteractionId.ToString(),
                $"Updated agent context for {context.AgentType}",
                context);

            return context;
        }

        // Tenant Context Methods

        public async Task<TenantContext> GetTenantContextAsync(string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogDebug("[{TraceId}] Getting tenant context", traceId);

            var tenantId = _tenantContextService.GetCurrentTenantId();
            var cacheKey = GetCacheKey("TenantContext", tenantId);
            
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = _options.CacheExpiration;
                
                var tenantInfo = await _tenantContextService.GetTenantInfoAsync();
                if (tenantInfo == null)
                {
                    throw new InvalidOperationException("Tenant information not available");
                }

                var context = new TenantContext
                {
                    TenantId = tenantInfo.Id,
                    Name = tenantInfo.Name,
                    TimeZone = tenantInfo.TimeZone ?? "UTC",
                    DefaultLanguage = tenantInfo.DefaultLanguage ?? "en-US",
                    EnabledFeatures = tenantInfo.EnabledFeatures ?? new HashSet<string>()
                };

                // Add industry profile if available
                if (tenantInfo.Industry != null)
                {
                    context.IndustryProfile = new IndustryProfile
                    {
                        Name = tenantInfo.Industry,
                        Settings = tenantInfo.IndustrySettings ?? new()
                    };
                }

                await _orchestrationLogger.LogActivityAsync(
                    "TenantContextRetrieved",
                    tenantInfo.Id,
                    "Retrieved tenant context",
                    new { TenantId = tenantInfo.Id, tenantInfo.Name });

                return context;
            }) ?? throw new InvalidOperationException("Failed to create tenant context");
        }

        // Intelligence Methods

        public async Task<ContextIntelligence> GetCachedIntelligenceAsync(Guid interactionId, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogDebug(
                "[{TraceId}] Getting cached intelligence for interaction {InteractionId}",
                traceId, interactionId);

            var cacheKey = GetCacheKey("ContextIntelligence", interactionId.ToString());
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = _options.IntelligenceCacheExpiration;
                
                // In a real implementation, this would analyze messages and other context
                var intelligence = new ContextIntelligence
                {
                    InteractionId = interactionId,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    Sentiment = 0.5, // Neutral sentiment
                    Confidence = 0.8,
                    Reason = "Initial analysis"
                };

                await _orchestrationLogger.LogActivityAsync(
                    "IntelligenceGenerated",
                    interactionId.ToString(),
                    "Generated initial intelligence",
                    intelligence);

                return intelligence;
            }) ?? new ContextIntelligence { InteractionId = interactionId };
        }

        // NBA Integration Methods

        public async Task<object> GetNbaContextAsync(Guid contactId, Guid organizationId, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            _logger.LogDebug(
                "[{TraceId}] Getting NBA context for contact {ContactId} in organization {OrganizationId}",
                traceId, contactId, organizationId);

            var cacheKey = GetCacheKey("NbaContext", $"{contactId}_{organizationId}");
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = _options.CacheExpiration;
                
                try
                {
                    var context = await _nbaContextService.GetContextAsync(contactId, organizationId);
                    
                    await _orchestrationLogger.LogActivityAsync(
                        "NbaContextRetrieved",
                        contactId.ToString(),
                        $"Retrieved NBA context for contact {contactId}",
                        new { ContactId = contactId, OrganizationId = organizationId });
                    
                    return context ?? new object();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{TraceId}] Failed to get NBA context", traceId);
                    throw;
                }
            }) ?? new object();
        }

        // Helper Methods

        private static string GetCacheKey(string prefix, string id) => $"{prefix}:{id}";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources if any
                }
                _disposed = true;
            }
        }
    }
}
