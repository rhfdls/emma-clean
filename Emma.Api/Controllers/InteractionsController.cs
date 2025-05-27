using Emma.Data.Models;
using Microsoft.AspNetCore.Mvc;
using System;
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
    }
}
