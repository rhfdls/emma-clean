using Microsoft.AspNetCore.Mvc;
using Emma.Core.Interfaces.Repositories;
using Emma.Models.Models;
using Microsoft.AspNetCore.Authorization;

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
                return BadRequest("Contact data is required.");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
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
                return StatusCode(500, $"Error creating contact: {details}");
            }
        }

        // GET /contacts/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactById(Guid id, [FromServices] IContactRepository repo)
        {
            var contact = await repo.GetByIdAsync(id);
            if (contact == null)
                return NotFound();
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
        [HttpGet]
        public async Task<IActionResult> GetContactsByOrg([FromQuery] Guid orgId, [FromServices] IContactRepository repo)
        {
            if (orgId == Guid.Empty)
                return BadRequest("orgId is required.");
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
        public async Task<IActionResult> AssignContact(Guid id, [FromBody] Dtos.ContactAssignDto dto, [FromServices] IContactRepository repo)
        {
            if (dto == null || dto.UserId == Guid.Empty)
                return BadRequest("UserId is required.");
            var contact = await repo.GetByIdAsync(id);
            if (contact == null)
                return NotFound();
            contact.OwnerId = dto.UserId;
            contact.UpdatedAt = DateTime.UtcNow;
            repo.Update(contact);
            await repo.SaveChangesAsync();
            return NoContent();
        }
    }
}
