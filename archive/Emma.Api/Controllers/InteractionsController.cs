using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emma.Api.Dtos;
using Emma.Api.Services;
using Emma.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// API endpoints for managing Interactions in the Emma AI Platform.
    /// </summary>
    [ApiController]
    [Route("api/v{version:apiVersion}/interactions")]
    [ApiVersion("1.0")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public class InteractionsController : ControllerBase
    {
        private readonly ILogger<InteractionsController> _logger;
        private readonly IInteractionService _interactionService;

        public InteractionsController(
            ILogger<InteractionsController> logger,
            IInteractionService interactionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
        }

        /// <summary>
        /// Search interactions with filtering and pagination
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedResult<Interaction>>> Search([FromBody] InteractionSearchDto searchDto)
        {
            try
            {
                var result = await _interactionService.SearchInteractionsAsync(searchDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching interactions");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching interactions");
            }
        }

        /// <summary>
        /// Get an interaction by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Interaction>> GetById(Guid id)
        {
            try
            {
                var interaction = await _interactionService.GetInteractionByIdAsync(id);
                if (interaction == null)
                    return NotFound();

                return Ok(interaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting interaction with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the interaction");
            }
        }

        /// <summary>
        /// Create a new interaction
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Interaction>> Create([FromBody] InteractionDto interactionDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var interaction = MapToModel(interactionDto);
                await _interactionService.CreateInteractionAsync(interaction);
                
                return CreatedAtAction(nameof(GetById), new { id = interaction.Id }, interaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating interaction");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the interaction");
            }
        }

        /// <summary>
        /// Update an existing interaction
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Interaction>> Update(Guid id, [FromBody] InteractionDto interactionDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existingInteraction = await _interactionService.GetInteractionByIdAsync(id);
                if (existingInteraction == null)
                    return NotFound();

                var updatedInteraction = MapToModel(interactionDto, existingInteraction);
                await _interactionService.UpdateInteractionAsync(updatedInteraction);

                return Ok(updatedInteraction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating interaction with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the interaction");
            }
        }

        /// <summary>
        /// Delete an interaction
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var success = await _interactionService.DeleteInteractionAsync(id);
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting interaction with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the interaction");
            }
        }

        /// <summary>
        /// Search interactions using AI-powered semantic search
        /// </summary>
        [HttpPost("semantic-search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Interaction>>> SemanticSearch([FromBody] SemanticSearchDto searchDto)
        {
            if (string.IsNullOrWhiteSpace(searchDto?.Query))
                return BadRequest("Search query is required");

            try
            {
                var results = await _interactionService.SemanticSearchAsync(searchDto);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic search");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during semantic search");
            }
        }

        #region Private Methods

        private Interaction MapToModel(InteractionDto dto, Interaction? existing = null)
        {
            var interaction = existing ?? new Interaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = GetCurrentOrganizationId(),
                TenantId = GetCurrentTenantId(),
                CreatedById = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow,
                Version = 1
            };

            // Update properties from DTO
            interaction.Type = dto.Type?.ToLower() ?? "other";
            interaction.Direction = dto.Direction?.ToLower() ?? "inbound";
            interaction.Status = dto.Status?.ToLower() ?? "draft";
            interaction.Priority = dto.Priority?.ToLower() ?? "normal";
            interaction.Subject = dto.Subject;
            interaction.Content = dto.Content;
            interaction.Summary = dto.Summary;
            interaction.PrivacyLevel = dto.PrivacyLevel?.ToLower() ?? "internal";
            interaction.Confidentiality = dto.Confidentiality?.ToLower() ?? "standard";
            interaction.RetentionPolicy = dto.RetentionPolicy;
            interaction.FollowUpRequired = dto.FollowUpRequired ?? false;
            interaction.Channel = dto.Channel?.ToLower() ?? "other";
            interaction.ChannelData = dto.ChannelData;
            interaction.Tags = dto.Tags ?? new List<string>();
            interaction.CustomFields = dto.CustomFields ?? new Dictionary<string, object>();
            interaction.ExternalIds = dto.ExternalIds ?? new Dictionary<string, string>();
            interaction.StartedAt = dto.StartedAt;
            interaction.EndedAt = dto.EndedAt;
            interaction.ScheduledFor = dto.ScheduledFor;
            interaction.DurationSeconds = dto.DurationSeconds;
            interaction.FollowUpBy = dto.FollowUpBy;
            interaction.AssignedToId = dto.AssignedToId;
            interaction.UpdatedAt = DateTime.UtcNow;

            // Map participants
            if (dto.Participants != null)
            {
                interaction.Participants = dto.Participants.Select(p => new Participant
                {
                    ContactId = p.ContactId,
                    Role = p.Role?.ToLower() ?? "participant",
                    Name = p.Name,
                    Email = p.Email,
                    Phone = p.Phone
                }).ToList();
            }

            // Map related entities
            if (dto.RelatedEntities != null)
            {
                interaction.RelatedEntities = dto.RelatedEntities.Select(e => new RelatedEntity
                {
                    Type = e.Type,
                    Id = e.Id,
                    Role = e.Role,
                    Name = e.Name
                }).ToList();
            }

            return interaction;
        }

        private Guid GetCurrentUserId()
        {
            // TODO: Implement actual user ID retrieval from claims
            return Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("User ID not found in claims"));
        }

        private Guid GetCurrentOrganizationId()
        {
            // TODO: Implement actual org ID retrieval from claims
            return Guid.Parse(User.FindFirst("org_id")?.Value ?? throw new InvalidOperationException("Organization ID not found in claims"));
        }

        private Guid GetCurrentTenantId()
        {
            // TODO: Implement actual tenant ID retrieval from claims
            return Guid.Parse(User.FindFirst("tenant_id")?.Value ?? throw new InvalidOperationException("Tenant ID not found in claims"));
        }

        #endregion
    }
}
