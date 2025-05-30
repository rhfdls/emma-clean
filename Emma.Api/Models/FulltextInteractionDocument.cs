using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Emma.Api.Models
{
    /// <summary>
    /// CosmosDB Interaction document for AI and agent workflows.
    /// Fields marked [Shared] must be kept in sync with PostgreSQL. Others are CosmosDB-only.
    /// </summary>
    public class FulltextInteractionDocument
    {
        /// <summary>[Shared] Unique interaction/document ID</summary>
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>[Shared] Agent GUID</summary>
        [JsonProperty("agentId")]
        public Guid AgentId { get; set; }

        /// <summary>[Shared] Contact GUID</summary>
        [JsonProperty("contactId")]
        public Guid ContactId { get; set; }

        /// <summary>[Shared] Organization GUID</summary>
        [JsonProperty("organizationId")]
        public Guid? OrganizationId { get; set; }

        /// <summary>[Shared] Interaction type (call, email, sms, note, etc.)</summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>[Shared] Full text content of the interaction</summary>
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>[Shared] UTC timestamp of the interaction</summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>[Shared] Flexible metadata (key-value pairs)</summary>
        [JsonProperty("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>[Shared] Custom fields for integration with relational model</summary>
        [JsonProperty("customFields")]
        public Dictionary<string, string>? CustomFields { get; set; }

        /// <summary>[CosmosDB-only] AI-generated embedding vector</summary>
        [JsonProperty("embedding")]
        public List<float>? Embedding { get; set; }

        /// <summary>[CosmosDB-only] Embedding model name (e.g., openai-ada-002)</summary>
        [JsonProperty("embeddingModel")]
        public string? EmbeddingModel { get; set; }

        /// <summary>[CosmosDB-only] When the embedding was generated</summary>
        [JsonProperty("embeddingDate")]
        public DateTime? EmbeddingDate { get; set; }

        /// <summary>[CosmosDB-only] Tags for AI/RAG/lead classification</summary>
        [JsonProperty("tags")]
        public List<string>? Tags { get; set; }
    }
}
