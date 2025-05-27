using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Core.Dtos;
using Emma.Core.Interfaces;
using Emma.Data;
using Emma.Data.Enums;
using Emma.Data.Models;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataEntryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEmmaAgentService _emmaAgentService;
    private readonly ILogger<DataEntryController> _logger;
    
    public DataEntryController(
        AppDbContext db, 
        IEmmaAgentService emmaAgentService,
        ILogger<DataEntryController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _emmaAgentService = emmaAgentService ?? throw new ArgumentNullException(nameof(emmaAgentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations()
    {
        var orgs = await _db.Organizations
            .Include(o => o.Agents)
            .ToListAsync();
        return Ok(orgs);
    }

    [HttpGet("agents/{organizationId}")]
    public async Task<IActionResult> GetAgents(Guid organizationId)
    {
        var agents = await _db.Agents.Where(a => a.OrganizationId == organizationId).ToListAsync();
        return Ok(agents);
    }

    public class DemoMessageDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public class MessageEntryDto
    {
        public Guid OrganizationId { get; set; }
        public Guid AgentId { get; set; }
        public string ClientFirstName { get; set; } = string.Empty;
        public string ClientLastName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text"; // Text, Email, Note, Call
        public DateTime? OccurredAt { get; set; }
        public bool NewInteraction { get; set; } = false;
        public Guid? InteractionId { get; set; }
    }

    /// <summary>
    /// Adds a new message to a conversation and processes it with EMMA
    /// </summary>
    /// <summary>
    /// Simplified endpoint for demo purposes that processes a message with EMMA
    /// </summary>
    [HttpPost("process-demo")]
    [ProducesResponseType(typeof(EmmaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessDemo([FromBody] DemoMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest("Message content cannot be empty");
        }

        try
        {
            // Process the message with EMMA
            var emmaResponse = await _emmaAgentService.ProcessMessageAsync(dto.Content);
            return Ok(emmaResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing demo message");
            return StatusCode(500, new { error = "An error occurred while processing the message" });
        }
    }

    [HttpPost("add-message")]
    [ProducesResponseType(typeof(EmmaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage([FromBody] MessageEntryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest("Message content cannot be empty");
        }

        try
        {
            // Process the message with EMMA
            var emmaResponse = await _emmaAgentService.ProcessMessageAsync(dto.Content);

            // Save the conversation and message to the database
            var conversation = await GetOrCreateInteractionAsync(dto);
            var message = await SaveMessageAsync(dto, conversation.Id, emmaResponse.RawModelOutput);

            // Handle call transcriptions if needed
            await HandleCallTranscriptionAsync(dto, message);

            // Save all changes to the database
            await _db.SaveChangesAsync();

            // Return the EMMA response
            return Ok(emmaResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return StatusCode(500, new { error = "An error occurred while processing the message" });
        }
    }

    private async Task<Interaction> GetOrCreateInteractionAsync(MessageEntryDto dto)
    {
        if (dto.NewInteraction || dto.InteractionId == null)
        {
            var conversation = new Interaction
            {
                Id = Guid.NewGuid(),
                AgentId = dto.AgentId,
                OrganizationId = dto.OrganizationId,
                ClientFirstName = dto.ClientFirstName,
                ClientLastName = dto.ClientLastName,
                CreatedAt = DateTime.UtcNow
            };
            await _db.Interactions.AddAsync(conversation);
            return conversation;
        }
        else
        {
            var existing = await _db.Interactions.FindAsync(dto.InteractionId);
            if (existing == null)
            {
                throw new InvalidOperationException("Interaction not found");
            }
            return existing;
        }
    }

    private async Task<Message> SaveMessageAsync(MessageEntryDto dto, Guid conversationId, string? aiResponse = null)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            InteractionId = conversationId,
            Payload = dto.Content,
            Type = Enum.TryParse<MessageType>(dto.MessageType, true, out var mt) ? mt : MessageType.Text,
            OccurredAt = dto.OccurredAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            BlobStorageUrl = string.Empty, // not used for manual entry
            AiResponse = aiResponse
        };
        await _db.Messages.AddAsync(message);
        return message;
    }

    private async Task HandleCallTranscriptionAsync(MessageEntryDto dto, Message message)
    {
        if (Enum.TryParse<MessageType>(dto.MessageType, true, out var messageType) && 
            messageType == MessageType.Call)
        {
            var transcription = new Transcription
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                BlobStorageUrl = string.Empty,
                Type = TranscriptionType.Full,
                CreatedAt = DateTime.UtcNow
            };
            await _db.Transcriptions.AddAsync(transcription);
        }
    }

    [HttpGet("all-agents")]
    public async Task<IActionResult> GetAllAgents()
    {
        var agents = await _db.Agents.ToListAsync();
        return Ok(agents);
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] Guid organizationId, [FromQuery] Guid agentId, [FromQuery] int count = 10)
    {
        var messages = await _db.Messages
            .Include(m => m.Interaction)
            .Where(m => m.Interaction != null && 
                      m.Interaction.OrganizationId == organizationId && 
                      m.Interaction.AgentId == agentId)
            .OrderByDescending(m => m.OccurredAt)
            .Take(count)
            .Select(m => new {
                MessageId = m.Id,
                PayloadValue = m.Payload,
                TypeValue = m.Type,
                OccurredAtValue = m.OccurredAt,
                CreatedAtValue = m.CreatedAt,
                InteractionIdValue = m.Interaction != null ? m.Interaction.Id : Guid.Empty,
                ClientFirstNameValue = m.Interaction != null ? m.Interaction.ClientFirstName : null,
                ClientLastNameValue = m.Interaction != null ? m.Interaction.ClientLastName : null
            })
            .ToListAsync();
        return Ok(messages);
    }
}
