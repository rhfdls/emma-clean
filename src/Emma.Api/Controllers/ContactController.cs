using Microsoft.AspNetCore.Mvc;
using Emma.Core.Interfaces.Repositories;
using Emma.Models.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        // POST /contacts
        [Authorize(Policy = "VerifiedUser")]
        [HttpPost]
        public async Task<IActionResult> CreateContact([FromBody] Dtos.ContactCreateDto dto, [FromServices] IContactRepository repo)
        {
            if (dto == null)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Contact data is required.");
            if (!ModelState.IsValid)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Invalid request body.");
            try
            {
                var contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = dto.OrganizationId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    MiddleName = dto.MiddleName,
                    PreferredName = dto.PreferredName,
                    Title = dto.Title,
                    JobTitle = dto.JobTitle,
                    Company = dto.Company,
                    Department = dto.Department,
                    Source = dto.Source,
                    OwnerId = dto.OwnerId,
                    PreferredContactMethod = dto.PreferredContactMethod,
                    PreferredContactTime = dto.PreferredContactTime,
                    Notes = dto.Notes,
                    ProfilePictureUrl = dto.ProfilePictureUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await repo.AddAsync(contact);
                var result = new Dtos.ContactReadDto
                {
                    Id = contact.Id,
                    OrganizationId = contact.OrganizationId,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    MiddleName = contact.MiddleName,
                    PreferredName = contact.PreferredName,
                    Title = contact.Title,
                    JobTitle = contact.JobTitle,
                    Company = contact.Company,
                    Department = contact.Department,
                    Source = contact.Source,
                    OwnerId = contact.OwnerId,
                    PreferredContactMethod = contact.PreferredContactMethod,
                    PreferredContactTime = contact.PreferredContactTime,
                    Notes = contact.Notes,
                    ProfilePictureUrl = contact.ProfilePictureUrl,
                    LastContactedAt = contact.LastContactedAt,
                    NextFollowUpAt = contact.NextFollowUpAt
                };
                return CreatedAtAction(nameof(GetContactById), new { id = contact.Id }, result);
            }
            catch (Exception ex)
            {
                // Return detailed error for diagnostics (dev only)
                var details = ex.ToString();
                if (ex.InnerException != null)
                {
                    details += "\nInner: " + ex.InnerException.ToString();
                }
                return Problem(statusCode: 500, title: "Error creating contact", detail: details);
            }
        }

        // GET /contacts/{id}
        [Authorize(Policy = "VerifiedUser")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactById(Guid id, [FromServices] IContactRepository repo)
        {
            var orgIdStr = User?.FindFirstValue("orgId");
            if (string.IsNullOrWhiteSpace(orgIdStr) || !Guid.TryParse(orgIdStr, out var orgId))
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");
            var contact = await repo.GetByIdAsync(id);
            if (contact == null)
                return Problem(statusCode: 404, title: "Contact not found", detail: $"No contact with id {id}.");
            if (contact.OrganizationId != orgId)
                return Problem(statusCode: 403, title: "Forbidden", detail: "Contact organization does not match orgId claim.");
            var dto = new Dtos.ContactReadDto
            {
                Id = contact.Id,
                OrganizationId = contact.OrganizationId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                MiddleName = contact.MiddleName,
                PreferredName = contact.PreferredName,
                Title = contact.Title,
                JobTitle = contact.JobTitle,
                Company = contact.Company,
                Department = contact.Department,
                Source = contact.Source,
                OwnerId = contact.OwnerId,
                PreferredContactMethod = contact.PreferredContactMethod,
                PreferredContactTime = contact.PreferredContactTime,
                Notes = contact.Notes,
                ProfilePictureUrl = contact.ProfilePictureUrl,
                LastContactedAt = contact.LastContactedAt,
                NextFollowUpAt = contact.NextFollowUpAt
            };
            return Ok(dto);
        }

        // GET /contacts?orgId=x
        [Authorize(Policy = "VerifiedUser")]
        [HttpGet]
        public async Task<IActionResult> GetContactsByOrg([FromQuery] Guid orgId, [FromServices] IContactRepository repo)
        {
            if (orgId == Guid.Empty)
                return Problem(statusCode: 400, title: "Validation failed", detail: "orgId is required.");
            var orgIdStr = User?.FindFirstValue("orgId");
            if (string.IsNullOrWhiteSpace(orgIdStr) || !Guid.TryParse(orgIdStr, out var claimOrgId))
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");
            if (claimOrgId != orgId)
                return Problem(statusCode: 403, title: "Forbidden", detail: "Query orgId does not match orgId claim.");
            var contacts = await repo.FindAsync(c => c.OrganizationId == orgId);
            var dtos = contacts.Select(contact => new Dtos.ContactReadDto
            {
                Id = contact.Id,
                OrganizationId = contact.OrganizationId,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                MiddleName = contact.MiddleName,
                PreferredName = contact.PreferredName,
                Title = contact.Title,
                JobTitle = contact.JobTitle,
                Company = contact.Company,
                Department = contact.Department,
                Source = contact.Source,
                OwnerId = contact.OwnerId,
                PreferredContactMethod = contact.PreferredContactMethod,
                PreferredContactTime = contact.PreferredContactTime,
                Notes = contact.Notes,
                ProfilePictureUrl = contact.ProfilePictureUrl,
                LastContactedAt = contact.LastContactedAt,
                NextFollowUpAt = contact.NextFollowUpAt
            }).ToList();
            return Ok(dtos);
        }

        // PUT /contacts/{id}/assign
        [Authorize(Policy = "VerifiedUser")]
        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignContact(
            Guid id,
            [FromBody] Dtos.ContactAssignDto dto,
            [FromServices] IContactRepository repo,
            [FromServices] ILogger<ContactController> logger)
        {
            if (dto == null)
                return Problem(statusCode: 400, title: "Invalid request", detail: "Request body is required.");
            if (dto.UserId == Guid.Empty)
                return Problem(statusCode: 400, title: "Validation failed", detail: "UserId is required.");
            if (dto.AssignedByAgentId == Guid.Empty)
                return Problem(statusCode: 400, title: "Validation failed", detail: "AssignedByAgentId is required.");
            if (dto.ContactId != Guid.Empty && dto.ContactId != id)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Path id must match body.contactId.");
            var contact = await repo.GetByIdAsync(id);
            if (contact == null)
                return Problem(statusCode: 404, title: "Contact not found", detail: $"No contact with id {id}.");
            contact.OwnerId = dto.UserId;
            contact.UpdatedAt = DateTime.UtcNow;
            repo.Update(contact);
            await repo.SaveChangesAsync();
            var traceId = HttpContext?.TraceIdentifier;
            logger.LogInformation("Assigned contact {ContactId} to user {UserId} by agent {AgentId} traceId={TraceId}", id, dto.UserId, dto.AssignedByAgentId, traceId);
            return NoContent();
        }
    }
}
