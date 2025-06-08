using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Api.Dtos;
using Emma.Api.Models;
using Emma.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/fulltext-interactions")]
    public class FulltextInteractionController : ControllerBase
    {
        private readonly FulltextInteractionService _service;
        private readonly ILogger<FulltextInteractionController> _logger;

        public FulltextInteractionController(FulltextInteractionService service, ILogger<FulltextInteractionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] FulltextInteractionDto dto)
        {
            if (dto == null) return BadRequest("Request body is required.");
            try
            {
                var doc = new FulltextInteractionDocument
                {
                    AgentId = dto.AgentId,
                    ContactId = dto.ContactId,
                    OrganizationId = dto.OrganizationId,
                    Type = dto.Type,
                    Content = dto.Content,
                    Timestamp = dto.Timestamp,
                    Metadata = dto.Metadata
                };
                var saved = await _service.SaveAsync(doc);
                return Ok(saved);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed when saving fulltext document.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving fulltext document.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Query([FromQuery] Guid? agentId, [FromQuery] Guid? contactId, [FromQuery] string? type, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            try
            {
                var results = await _service.QueryAsync(agentId, contactId, type, start, end);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying fulltext documents.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
