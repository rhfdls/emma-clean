using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Emma.Core.Enums;

namespace Emma.Core.Models
{
    /// <summary>
    /// Represents the context in which an agent operation is performed.
    /// Tracks the user, agent, permissions, and trace information for an operation.
    /// </summary>
    public class AgentContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for this context.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the ID of the user who initiated the operation.
        /// Null if the operation was initiated by a system process.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the agent performing the operation.
        /// Required for all agent operations.
        /// </summary>
        [Required]
        public string AgentId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the agent for display purposes.
        /// </summary>
        public string AgentName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type of the agent (e.g., NBA, ContextIntelligence, etc.).
        /// </summary>
        [Required]
        public string AgentType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the organization this operation belongs to.
        /// </summary>
        [Required]
        public string OrganizationId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the contact this operation relates to, if any.
        /// </summary>
        public string? ContactId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the conversation this operation is part of, if any.
        /// </summary>
        public string? ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the trace ID for distributed tracing across services.
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// Gets or sets the session ID that links related operations together.
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this context was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the permissions associated with this context.
        /// These are derived from the user's roles and the agent's capabilities.
        /// </summary>
        public ICollection<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the agent's capabilities for this operation.
        /// </summary>
        public AgentCapability Capabilities { get; set; } = new();

        /// <summary>
        /// Gets or sets additional metadata about the operation.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the IP address where the request originated from, if applicable.
        /// </summary>
        public string? SourceIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the client making the request, if applicable.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the ID of the parent context, if this is a child operation.
        /// </summary>
        public string? ParentContextId { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracking related operations across services.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Creates a new child context with the same base properties as this one.
        /// </summary>
        /// <param name="agentId">The ID of the agent for the child context.</param>
        /// <param name="agentType">The type of the agent for the child context.</param>
        /// <returns>A new AgentContext with the same trace/session IDs and parent context set.</returns>
        public AgentContext CreateChildContext(string agentId, string agentType)
        {
            return new AgentContext
            {
                UserId = this.UserId,
                AgentId = agentId,
                AgentType = agentType,
                OrganizationId = this.OrganizationId,
                ContactId = this.ContactId,
                ConversationId = this.ConversationId,
                TraceId = this.TraceId ?? Guid.NewGuid().ToString(),
                SessionId = this.SessionId ?? Guid.NewGuid().ToString(),
                ParentContextId = this.Id,
                CorrelationId = this.CorrelationId,
                SourceIpAddress = this.SourceIpAddress,
                UserAgent = this.UserAgent,
                // Copy permissions and capabilities by value, not reference
                Permissions = new List<string>(this.Permissions),
                Capabilities = new AgentCapability(this.Capabilities)
            };
        }

        /// <summary>
        /// Creates a new context with the same trace/session but different agent.
        /// </summary>
        public AgentContext ForAgent(string agentId, string agentType)
        {
            return new AgentContext
            {
                UserId = this.UserId,
                AgentId = agentId,
                AgentType = agentType,
                OrganizationId = this.OrganizationId,
                ContactId = this.ContactId,
                ConversationId = this.ConversationId,
                TraceId = this.TraceId,
                SessionId = this.SessionId,
                ParentContextId = this.ParentContextId,
                CorrelationId = this.CorrelationId,
                SourceIpAddress = this.SourceIpAddress,
                UserAgent = this.UserAgent,
                Permissions = new List<string>(this.Permissions),
                Capabilities = new AgentCapability(this.Capabilities)
            };
        }

        /// <summary>
        /// Creates a new context with the same agent but different user context.
        /// </summary>
        public AgentContext ForUser(Guid userId, ICollection<string> permissions)
        {
            return new AgentContext
            {
                UserId = userId,
                AgentId = this.AgentId,
                AgentType = this.AgentType,
                OrganizationId = this.OrganizationId,
                ContactId = this.ContactId,
                ConversationId = this.ConversationId,
                TraceId = this.TraceId,
                SessionId = this.SessionId,
                ParentContextId = this.Id,
                CorrelationId = this.CorrelationId,
                SourceIpAddress = this.SourceIpAddress,
                UserAgent = this.UserAgent,
                Permissions = new List<string>(permissions),
                Capabilities = new AgentCapability(this.Capabilities)
            };
        }

        /// <summary>
        /// Validates that the context has all required fields set.
        /// </summary>
        /// <returns>A tuple indicating if the context is valid and an error message if not.</returns>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (string.IsNullOrEmpty(AgentId))
                return (false, "AgentId is required");
                
            if (string.IsNullOrEmpty(AgentType))
                return (false, "AgentType is required");
                
            if (string.IsNullOrEmpty(OrganizationId))
                return (false, "OrganizationId is required");
                
            // Generate a trace ID if one wasn't provided
            if (string.IsNullOrEmpty(TraceId))
                TraceId = Guid.NewGuid().ToString();
                
            // Generate a session ID if one wasn't provided
            if (string.IsNullOrEmpty(SessionId))
                SessionId = Guid.NewGuid().ToString();
                
            return (true, null);
        }
    }
}
