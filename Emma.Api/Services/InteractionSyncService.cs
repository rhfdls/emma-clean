using System;
using Emma.Api.Models;
using Emma.Data;
using System.Threading.Tasks;
using Emma.Data.Models; 
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services
{
    /// <summary>
    /// Service for synchronizing shared fields between PostgreSQL and CosmosDB for Interactions.
    /// </summary>
    public class InteractionSyncService
    {
        private readonly CosmosAgentRepository _cosmosRepo;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<InteractionSyncService> _logger;

        public InteractionSyncService(CosmosAgentRepository cosmosRepo, AppDbContext dbContext, ILogger<InteractionSyncService> logger)
        {
            _cosmosRepo = cosmosRepo;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Syncs shared fields from a PostgreSQL Interaction to CosmosDB.
        /// Call this after a PostgreSQL update/create.
        /// </summary>
        public async Task SyncToCosmosAsync(Interaction pgInteraction)
        {
            // Map shared fields
            var doc = new FulltextInteractionDocument
            {
                Id = pgInteraction.Id.ToString(),
                AgentId = pgInteraction.AgentId,
                ContactId = pgInteraction.ContactId,
                OrganizationId = pgInteraction.OrganizationId,
                Type = pgInteraction.Type,
                Content = pgInteraction.Content,
                Timestamp = pgInteraction.Timestamp,
                CustomFields = pgInteraction.CustomFields
            };
            // Upsert to CosmosDB
            await _cosmosRepo.UpsertItemAsync(doc, pgInteraction.Id.ToString());
            _logger.LogInformation("Synced Interaction {Id} to CosmosDB", pgInteraction.Id);
        }

        /// <summary>
        /// Syncs shared fields from a CosmosDB Interaction to PostgreSQL.
        /// Call this after a CosmosDB update/create if needed.
        /// </summary>
        public async Task SyncToPostgresAsync(FulltextInteractionDocument doc)
        {
            // Find or create the PostgreSQL entity
            var pg = await _dbContext.Interactions.FindAsync(Guid.Parse(doc.Id));
            if (pg == null)
            {
                pg = new Interaction { Id = Guid.Parse(doc.Id) };
                _dbContext.Interactions.Add(pg);
            }
            // Map shared fields
            pg.AgentId = doc.AgentId != Guid.Empty ? doc.AgentId : Guid.NewGuid();
            pg.ContactId = doc.ContactId != Guid.Empty ? doc.ContactId : Guid.NewGuid();
            pg.OrganizationId = doc.OrganizationId ?? Guid.Empty;
            pg.Type = doc.Type;
            pg.Content = doc.Content;
            pg.Timestamp = doc.Timestamp;
            pg.CustomFields = doc.CustomFields;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Synced Interaction {Id} to PostgreSQL", doc.Id);
        }
    }
}
