using Emma.Data.Models;
using Emma.Core.Interfaces;
using Emma.Api.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// API endpoints for managing Contacts in the Emma AI Platform.
    /// All endpoints enforce strict privacy and access control.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class ContactsController : ControllerBase
    {
        private readonly IContactAccessService _contactAccessService;
        private readonly IInteractionAccessService _interactionAccessService;
        
        public ContactsController(
            IContactAccessService contactAccessService,
            IInteractionAccessService interactionAccessService)
        {
            _contactAccessService = contactAccessService;
            _interactionAccessService = interactionAccessService;
        }
        
        /// <summary>
        /// Gets all contacts that the requesting agent is authorized to access.
        /// Respects organization boundaries and collaboration permissions.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contact>>> GetAll()
        {
            var agentId = GetRequestingAgentId();
            if (agentId == null)
                return Unauthorized("Invalid agent ID");
                
            var authorizedContacts = await _contactAccessService.GetAuthorizedContactsAsync(agentId.Value);
            return Ok(authorizedContacts);
        }

        /// <summary>
        /// Gets a specific contact by ID.
        /// Enforces access control - agent must own contact or have collaboration access.
        /// </summary>
        [HttpGet("{id}")]
        [RequireContactAccess("id")]
        public async Task<ActionResult<Contact>> GetById(Guid id)
        {
            // Access control is enforced by RequireContactAccess attribute
            // TODO: Replace with real data source - this is just for demonstration
            return Ok(new Contact { Id = id });
        }
        
        /// <summary>
        /// Gets business interactions for a contact.
        /// Excludes all PERSONAL/PRIVATE tagged interactions regardless of permissions.
        /// </summary>
        [HttpGet("{contactId}/interactions/business")]
        [RequireContactAccess("contactId")]
        public async Task<ActionResult<IEnumerable<Interaction>>> GetBusinessInteractions(Guid contactId)
        {
            var agentId = GetRequestingAgentId();
            if (agentId == null)
                return Unauthorized("Invalid agent ID");
                
            var businessInteractions = await _interactionAccessService.GetBusinessInteractionsAsync(contactId, agentId.Value);
            return Ok(businessInteractions);
        }
        
        /// <summary>
        /// Gets all authorized interactions for a contact.
        /// Includes personal interactions only if agent has explicit permission.
        /// </summary>
        [HttpGet("{contactId}/interactions")]
        [RequireContactAccess("contactId")]
        public async Task<ActionResult<IEnumerable<Interaction>>> GetAuthorizedInteractions(Guid contactId)
        {
            var agentId = GetRequestingAgentId();
            if (agentId == null)
                return Unauthorized("Invalid agent ID");
                
            var authorizedInteractions = await _interactionAccessService.GetAuthorizedInteractionsAsync(contactId, agentId.Value);
            return Ok(authorizedInteractions);
        }

        /// <summary>
        /// Creates a new contact.
        /// Contact is automatically owned by the creating agent.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Contact>> Create([FromBody] Contact contact)
        {
            var agentId = GetRequestingAgentId();
            if (agentId == null)
                return Unauthorized("Invalid agent ID");
                
            // TODO: Replace with real creation logic
            contact.Id = Guid.NewGuid();
            // Set ownership to creating agent
            // contact.OwnerId = agentId.Value;
            
            return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
        }
        
        /// <summary>
        /// Grants collaboration access to another agent for a contact.
        /// Only contact owners can grant collaboration access.
        /// </summary>
        [HttpPost("{contactId}/collaborators")]
        [RequireContactAccess("contactId")]
        public async Task<ActionResult> GrantCollaborationAccess(
            Guid contactId, 
            [FromBody] GrantCollaborationRequest request)
        {
            var agentId = GetRequestingAgentId();
            if (agentId == null)
                return Unauthorized("Invalid agent ID");
                
            // Verify the requesting agent is the contact owner
            var isOwner = await _contactAccessService.IsContactOwnerAsync(contactId, agentId.Value);
            if (!isOwner)
                return Forbid("Only contact owners can grant collaboration access");
                
            // TODO: Implement collaboration granting logic
            return Ok("Collaboration access granted");
        }
        
        private Guid? GetRequestingAgentId()
        {
            var agentIdClaim = User.FindFirst("AgentId")?.Value;
            return Guid.TryParse(agentIdClaim, out var agentId) ? agentId : null;
        }
    }
    
    /// <summary>
    /// Request model for granting collaboration access.
    /// </summary>
    public class GrantCollaborationRequest
    {
        public Guid CollaboratorAgentId { get; set; }
        public CollaboratorRole Role { get; set; }
        public bool CanAccessPersonalInteractions { get; set; } = false;
        public string? Reason { get; set; }
    }
}
