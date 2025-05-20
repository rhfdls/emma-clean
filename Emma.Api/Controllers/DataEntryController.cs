using Emma.Data;
using Emma.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Emma.Api.Services;
using Emma.Data.Enums;

namespace Emma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataEntryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Emma.Api.Services.IEmmaAgentService _emmaAgentService;
    public DataEntryController(AppDbContext db, Emma.Api.Services.IEmmaAgentService emmaAgentService)
    {
        _db = db;
        _emmaAgentService = emmaAgentService;
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

    public class MessageEntryDto
    {
        public Guid OrganizationId { get; set; }
        public Guid AgentId { get; set; }
        public string ClientFirstName { get; set; }
        public string ClientLastName { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } // Text, Email, Note, Call
        public DateTime? OccurredAt { get; set; }
        public bool NewConversation { get; set; } = false;
        public Guid? ConversationId { get; set; }
    }

    [HttpPost("add-message")] // Handles new conversation or add to existing
    public async Task<IActionResult> AddMessage([FromBody] MessageEntryDto dto)
    {
        Conversation conversation;
        if (dto.NewConversation || dto.ConversationId == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                AgentId = dto.AgentId,
                OrganizationId = dto.OrganizationId,
                ClientFirstName = dto.ClientFirstName,
                ClientLastName = dto.ClientLastName,
                CreatedAt = DateTime.UtcNow
            };
            _db.Conversations.Add(conversation);
        }
        else
        {
            var existing = await _db.Conversations.FindAsync(dto.ConversationId);
            if (existing == null) return NotFound("Conversation not found");
            conversation = existing;
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Payload = dto.Content,
            Type = Enum.TryParse<Emma.Data.Enums.MessageType>(dto.MessageType, true, out var mt) ? mt : Emma.Data.Enums.MessageType.Text,
            OccurredAt = dto.OccurredAt ?? DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            BlobStorageUrl = "" // not used for manual entry
        };
        _db.Messages.Add(message);

        // If type is Call, also add a basic Transcription
        if (message.Type == Emma.Data.Enums.MessageType.Call)
        {
            var transcription = new Transcription
            {
                Id = Guid.NewGuid(),
                MessageId = message.Id,
                BlobStorageUrl = string.Empty, // No blob for manual entry
                Type = Emma.Data.Enums.TranscriptionType.Full, // Or Partial if more appropriate
                CreatedAt = DateTime.UtcNow
            };
            _db.Transcriptions.Add(transcription);
        }

        await _db.SaveChangesAsync();

        // Gather conversation context (simple version: last 5 messages)
        var contextMessages = await _db.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.OccurredAt)
            .Take(5)
            .OrderBy(m => m.OccurredAt)
            .Select(m => $"{m.Type}: {m.Payload}")
            .ToListAsync();
        var conversationContext = string.Join("\n", contextMessages);

        // Call EmmaAgentService orchestrator
        var agentResult = await _emmaAgentService.HandleNewMessageAsync(dto.Content, conversationContext);

        return Ok(new { ConversationId = conversation.Id, MessageId = message.Id, AgentResult = agentResult });
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
            .Where(m => m.Conversation.OrganizationId == organizationId && m.Conversation.AgentId == agentId)
            .OrderByDescending(m => m.OccurredAt)
            .Take(count)
            .Select(m => new {
                MessageId = m.Id,
                PayloadValue = m.Payload,
                TypeValue = m.Type,
                OccurredAtValue = m.OccurredAt,
                CreatedAtValue = m.CreatedAt,
                ConversationIdValue = m.Conversation.Id,
                ClientFirstNameValue = m.Conversation.ClientFirstName,
                ClientLastNameValue = m.Conversation.ClientLastName
            })
            .ToListAsync();
        return Ok(messages);
    }
}
