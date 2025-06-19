using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emma.Api.Models;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services
{
    public class FulltextInteractionService
    {
        private readonly CosmosAgentRepository _cosmosRepo;
        private readonly ILogger<FulltextInteractionService> _logger;

        public FulltextInteractionService(CosmosAgentRepository cosmosRepo, ILogger<FulltextInteractionService> logger)
        {
            _cosmosRepo = cosmosRepo ?? throw new ArgumentNullException(nameof(cosmosRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FulltextInteractionDocument> SaveAsync(FulltextInteractionDocument doc)
        {
            if (string.IsNullOrWhiteSpace(doc.Content))
                throw new ArgumentException("Content cannot be empty", nameof(doc.Content));
            if (doc.AgentId == Guid.Empty)
                throw new ArgumentException("AgentId is required", nameof(doc.AgentId));
            if (doc.ContactId == Guid.Empty)
                throw new ArgumentException("ContactId is required", nameof(doc.ContactId));
            if (string.IsNullOrWhiteSpace(doc.Type))
                throw new ArgumentException("Type is required", nameof(doc.Type));

            _logger.LogInformation("Saving fulltext interaction document for agent {AgentId}, contact {ContactId}, type {Type}", doc.AgentId, doc.ContactId, doc.Type);
            return await _cosmosRepo.UpsertItemAsync(doc, doc.AgentId.ToString());
        }

        public async Task<IEnumerable<FulltextInteractionDocument>> QueryAsync(Guid? agentId = null, Guid? contactId = null, string? type = null, DateTime? start = null, DateTime? end = null)
        {
            var query = "SELECT * FROM c WHERE 1=1";
            if (agentId.HasValue) query += $" AND c.agentId = '{agentId}'";
            if (contactId.HasValue) query += $" AND c.contactId = '{contactId}'";
            if (!string.IsNullOrWhiteSpace(type)) query += $" AND c.type = '{type}'";
            if (start.HasValue) query += $" AND c.timestamp >= '{start.Value:O}'";
            if (end.HasValue) query += $" AND c.timestamp <= '{end.Value:O}'";

            _logger.LogInformation("Querying fulltext documents: {Query}", query);
            return await _cosmosRepo.QueryItemsAsync<FulltextInteractionDocument>(query);
        }
    }
}
