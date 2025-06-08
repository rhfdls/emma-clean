using Emma.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using Emma.Api.Models;
using Emma.Api.Services;
using System.Collections.Generic;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// API endpoints for managing Interactions in the Emma AI Platform.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InteractionsController : ControllerBase
    {
        /// <summary>
        /// Gets all interactions.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<Interaction>> GetAll()
        {
            // TODO: Replace with real data source
            return Ok(new List<Interaction>());
        }

        /// <summary>
        /// Gets a specific interaction by ID.
        /// </summary>
        [HttpGet("{id}")]
        public ActionResult<Interaction> GetById(Guid id)
        {
            // TODO: Replace with real data source
            return Ok(new Interaction { Id = id });
        }

        /// <summary>
        /// Creates a new interaction.
        /// </summary>
        [HttpPost]
        public ActionResult<Interaction> Create([FromBody] Interaction interaction)
        {
            // TODO: Replace with real creation logic
            interaction.Id = Guid.NewGuid();
            return CreatedAtAction(nameof(GetById), new { id = interaction.Id }, interaction);
        }

        /// <summary>
        /// [AI Agent] Retrieve agent interactions in CosmosDB using typed parameters.
        /// </summary>
        /// <param name="query">Query DTO with optional ContactId, AgentId, Start, End.</param>
        /// <remarks>
        /// Sample payload:
        /// {
        ///   "contactId": "00000000-0000-0000-0000-000000000000",
        ///   "agentId": "00000000-0000-0000-0000-000000000000",
        ///   "start": "2024-05-01T00:00:00Z",
        ///   "end": "2024-05-28T23:59:59Z"
        /// }
        /// </remarks>
        [HttpPost("search-by-query")]
        public async Task<ActionResult<IEnumerable<FulltextInteractionDocument>>> SearchByQuery([FromBody] Models.InteractionQueryDto query, [FromServices] AIFoundryService aiFoundryService)
        {
            var results = await aiFoundryService.RetrieveAgentInteractionsAsync(query);
            return Ok(results);
        }
    }
}
