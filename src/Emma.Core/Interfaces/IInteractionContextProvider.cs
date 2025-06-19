using System;
using System.Threading.Tasks;
using Emma.Core.Models.InteractionContext;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Provides access to interaction-related context and state management.
    /// </summary>
    public interface IInteractionContextProvider
    {
        // Core Interaction Context
        
        /// <summary>
        /// Gets the current context for an interaction.
        /// </summary>
        /// <param name="interactionId">The ID of the interaction.</param>
        /// <param name="traceId">A unique identifier for tracing the request.</param>
        /// <returns>The interaction context.</returns>
        Task<InteractionContext> GetInteractionContextAsync(Guid interactionId, string traceId);
        
        /// <summary>
        /// Updates the state of an interaction.
        /// </summary>
        /// <param name="interactionId">The ID of the interaction.</param>
        /// <param name="state">The new state.</param>
        /// <param name="reason">The reason for the state change.</param>
        /// <param name="traceId">A unique identifier for tracing the request.</param>
        /// <returns>The updated interaction context.</returns>
        Task<InteractionContext> UpdateInteractionStateAsync(
            Guid interactionId, 
            string state, 
            string? reason = null, 
            string? traceId = null);
            
        /// <summary>
        /// Adds a message to an interaction.
        /// </summary>
        Task<InteractionContext> AddInteractionMessageAsync(
            Guid interactionId, 
            InteractionMessage message, 
            string? traceId = null);
        
        // Agent Context
        
        /// <summary>
        /// Gets the context for an agent within an interaction.
        /// </summary>
        Task<AgentContext> GetAgentContextAsync(
            string agentType, 
            Guid interactionId, 
            string? traceId = null);
            
        /// <summary>
        /// Updates the context for an agent.
        /// </summary>
        Task<AgentContext> UpdateAgentContextAsync(
            AgentContext context, 
            string? traceId = null);
        
        // Tenant/Organization Context
        
        /// <summary>
        /// Gets the tenant/organization context.
        /// </summary>
        Task<TenantContext> GetTenantContextAsync(string? traceId = null);
        
        // Intelligence
        
        /// <summary>
        /// Gets cached intelligence for an interaction.
        /// </summary>
        Task<ContextIntelligence> GetCachedIntelligenceAsync(
            Guid interactionId, 
            string? traceId = null);
            
        // NBA Integration
        
        /// <summary>
        /// Gets NBA-specific context for a contact.
        /// </summary>
        Task<object> GetNbaContextAsync(
            Guid contactId, 
            Guid organizationId, 
            string? traceId = null);
    }
}
