using Microsoft.AspNetCore.Mvc;
using Emma.Core.Interfaces.Repositories;
using Emma.Models.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using Emma.Api.Authorization;
using Emma.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Emma.Api.Controllers
{
    [ApiController]
    [Authorize(Policy = "VerifiedUser")]
    [Route("api/contacts")] // plural standard
    public class ContactController : ControllerBase
    {
        private readonly ILogger<ContactController> _logger;
        public ContactController(ILogger<ContactController> logger)
        {
            _logger = logger;
        }
        private Guid? GetOrgIdFromClaims()
        {
            var orgIdStr = User?.FindFirstValue("orgId");
            if (string.IsNullOrWhiteSpace(orgIdStr) || !Guid.TryParse(orgIdStr, out var orgId))
                return null;
            return orgId;
        }

        private Guid? GetUserIdFromClaims()
        {
            var userIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");
            if (Guid.TryParse(userIdStr, out var userId)) return userId;
            return null;
        }

        private IReadOnlyCollection<string> GetRolesFromClaims()
        {
            var roles = new List<string>();
            var roleClaims = User?.Claims?.Where(c => c.Type == ClaimTypes.Role || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)) ?? Enumerable.Empty<Claim>();
            foreach (var rc in roleClaims)
            {
                if (!string.IsNullOrWhiteSpace(rc.Value)) roles.Add(rc.Value);
            }
            return roles;
        }

        /// <summary>Create a contact in the caller's organization.</summary>
        /// <remarks>
        /// - <b>Org scoping:</b> OrganizationId is derived from JWT; any client-supplied org is ignored.
        /// - <b>Relationship:</b> Optional <c>relationshipState</c> (defaults to <c>Lead</c>).
        /// - <b>Provider fields:</b> companyName, licenseNumber, isPreferred, website.
        /// </remarks>
        /// <response code="201">Created with Location header and ContactReadDto.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden (missing org/user claims).</response>
        /// <response code="422">Validation failed (RFC7807).</response>
        [HttpPost]
        [ProducesResponseType(typeof(Emma.Api.Dtos.ContactReadDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateContact([FromBody] Dtos.ContactCreateDto dto, [FromServices] IContactRepository repo)
        {
            if (dto == null)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Contact data is required.");
            if (!ModelState.IsValid)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Invalid request body.");
            try
            {
                // Org scoping from JWT claim
                var orgId = GetOrgIdFromClaims();
                if (orgId is null)
                    return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

                var contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId.Value,
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
                    // Provider fields (optional)
                    CompanyName = dto.CompanyName,
                    LicenseNumber = dto.LicenseNumber,
                    IsPreferred = dto.IsPreferred ?? false,
                    Website = dto.Website,
                    RelationshipState = dto.RelationshipState ?? RelationshipState.Lead,
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
                    NextFollowUpAt = contact.NextFollowUpAt,
                    RelationshipState = contact.RelationshipState,
                    CompanyName = contact.CompanyName,
                    LicenseNumber = contact.LicenseNumber,
                    IsPreferred = contact.IsPreferred,
                    Website = contact.Website
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

        /// <summary>Get a single contact by id within the caller's organization.</summary>
        /// <remarks>
        /// Returns 404 if the contact does not exist or does not belong to the caller's organization (tenant scoping).
        /// </remarks>
        /// <response code="200">Contact found.</response>
        /// <response code="404">Not found within caller's org.</response>
        /// <response code="400">Missing org context.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Emma.Api.Dtos.ContactReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetContactById(Guid id, [FromServices] IContactRepository repo)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");
            var contact = await repo.GetByIdAsync(id);
            if (contact == null)
                return Problem(statusCode: 404, title: "Contact not found", detail: $"No contact with id {id}.");
            if (contact.OrganizationId != orgId.Value)
                return Problem(statusCode: 404, title: "Contact not found", detail: $"No contact with id {id}.");
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
                NextFollowUpAt = contact.NextFollowUpAt,
                RelationshipState = contact.RelationshipState,
                CompanyName = contact.CompanyName,
                LicenseNumber = contact.LicenseNumber,
                IsPreferred = contact.IsPreferred,
                Website = contact.Website
            };
            return Ok(dto);
        }

        /// <summary>List contacts for the caller's organization.</summary>
        /// <remarks>
        /// Tenant scoping is derived from the orgId claim. Any client-provided orgId is ignored.
        /// Optional filters: ownerId, relationshipState, q (name/company contains), page, size.
        /// </remarks>
        /// <response code="200">List of contacts scoped to the caller's organization.</response>
        /// <response code="400">Missing org context.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Emma.Api.Dtos.ContactReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetContactsByOrg(
            [FromServices] EmmaDbContext db,
            [FromQuery] Guid? ownerId,
            [FromQuery] RelationshipState? relationshipState,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int size = 50)
        {
            var claimOrgId = GetOrgIdFromClaims();
            if (claimOrgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");
            page = page < 1 ? 1 : page;
            size = size < 1 ? 1 : (size > 200 ? 200 : size);

            var query = db.Contacts.AsNoTracking().Where(c => c.OrganizationId == claimOrgId.Value);
            if (ownerId.HasValue) query = query.Where(c => c.OwnerId == ownerId.Value);
            if (relationshipState.HasValue) query = query.Where(c => c.RelationshipState == relationshipState.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var ql = q.ToLower();
                query = query.Where(c =>
                    (c.FirstName != null && c.FirstName.ToLower().Contains(ql)) ||
                    (c.LastName != null && c.LastName.ToLower().Contains(ql)) ||
                    (c.Company != null && c.Company.ToLower().Contains(ql))
                );
            }

            var contacts = await query
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
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
                NextFollowUpAt = contact.NextFollowUpAt,
                RelationshipState = contact.RelationshipState,
                CompanyName = contact.CompanyName,
                LicenseNumber = contact.LicenseNumber,
                IsPreferred = contact.IsPreferred,
                Website = contact.Website
            }).ToList();
            return Ok(dtos);
        }

        /// <summary>Delete a contact in the caller's organization.</summary>
        /// <response code="204">Deleted.</response>
        /// <response code="404">Not found in caller's org.</response>
        /// <response code="400">Missing org context.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteContact(Guid id, [FromServices] IContactRepository repo)
        {
            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var contact = await repo.GetByIdAsync(id);
            if (contact is null || contact.OrganizationId != orgId.Value)
                return Problem(statusCode: 404, title: "Contact not found", detail: $"No contact with id {id}.");

            repo.Remove(contact);
            await repo.SaveChangesAsync();
            return NoContent();
        }

        // PUT /contacts/{id}/assign (legacy plural assign)
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

        /// <summary>Assign or reassign ownership (sets <c>OwnerId</c>).</summary>
        /// <remarks>
        /// <b>Permissions:</b> Owner/Admin or current Owner. Collaborators and other users are forbidden.
        /// <b>Behavior:</b> Single-primary is implicit: <c>OwnerId</c> is the canonical primary assignee.
        /// </remarks>
        /// <response code="204">Ownership updated.</response>
        /// <response code="403">Forbidden (not Owner/Admin/current Owner).</response>
        /// <response code="404">Not found in caller's org.</response>
        public class ContactAssignmentRequest
        {
            public Guid AssigneeUserId { get; set; }
            public bool IsPrimary { get; set; } = true;
        }

        [HttpPost("{id}/assignments")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateUserAssignment(Guid id, [FromBody] ContactAssignmentRequest body, [FromServices] IContactRepository repo, [FromServices] EmmaDbContext db)
        {
            if (body is null)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Request body is required.");

            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var callerUserId = GetUserIdFromClaims();
            if (callerUserId is null)
                return Problem(statusCode: 401, title: "Unauthorized", detail: "Missing or invalid user identity.");

            var contact = await repo.GetByIdAsync(id);
            if (contact is null || contact.OrganizationId != orgId.Value)
                return Problem(statusCode: 404, title: "Contact not found", detail: $"No contact with id {id}.");

            // Allow initial ownership claim when no owner has been set yet
            if (!contact.OwnerId.HasValue || contact.OwnerId.Value == Guid.Empty)
            {
                contact.OwnerId = body.AssigneeUserId;
                contact.UpdatedAt = DateTime.UtcNow;
                repo.Update(contact);
                await repo.SaveChangesAsync();
                return NoContent();
            }

            // RBAC: only Owner/Admin or current Owner may (re)assign; self-assign for non-owners is disabled
            var roles = GetRolesFromClaims();
            var isOwnerOrAdmin = ContactAccessService.IsOwnerOrAdmin(roles);
            var isCurrentOwner = ContactAccessService.IsCurrentOwner(callerUserId.Value, contact);
            if (!isOwnerOrAdmin && !isCurrentOwner)
                return Problem(statusCode: 403, title: "Forbidden", detail: "Only Owner/Admin or current Owner can assign.");

            // Validate assignee exists and belongs to same org
            var assignee = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == body.AssigneeUserId);
            if (assignee is null || assignee.OrganizationId != orgId.Value)
                return Problem(statusCode: 422, title: "Validation failed", detail: "Assignee must be a valid user in the same organization.");

            // Single-primary rule maps to single OwnerId in current model; set owner to assignee
            contact.OwnerId = body.AssigneeUserId;
            contact.UpdatedAt = DateTime.UtcNow;
            repo.Update(contact);
            await repo.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Update a contact.</summary>
        /// <remarks>
        /// <b>Permissions:</b>
        /// - Owner/Admin and current Owner → full edit.
        /// - Collaborator → <em>business-only</em> fields (companyName, department, jobTitle, website, tags, notes, licenseNumber, isPreferred).
        /// - Collaborators <b>cannot</b> change ownership (OwnerId), relationshipState, PII (emails/phones/addresses), or consent (view-only).
        /// Returns RFC7807 with <c>errors.blockedFields</c> when a collaborator tries forbidden fields.
        /// </remarks>
        /// <response code="200">Updated contact (ContactReadDto).</response>
        /// <response code="403">Forbidden (RBAC/field-level).</response>
        /// <response code="404">Not found in caller's org.</response>
        /// <response code="422">Validation failed.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Emma.Api.Dtos.ContactReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> UpdateContact(
            Guid id,
            [FromBody] Dtos.ContactUpdateDto dto,
            [FromServices] IContactRepository repo,
            [FromServices] EmmaDbContext db,
            [FromServices] Emma.Api.Services.ContactUpdateService updater)
        {
            if (dto is null)
                return Problem(statusCode: 400, title: "Validation failed", detail: "Request body is required.");

            var orgId = GetOrgIdFromClaims();
            if (orgId is null)
                return Problem(statusCode: 400, title: "Missing org context", detail: "Missing or invalid orgId claim.");

            var callerUserId = GetUserIdFromClaims();
            if (callerUserId is null)
                return Problem(statusCode: 401, title: "Unauthorized", detail: "Missing or invalid user identity.");

            var roles = GetRolesFromClaims();
            return await updater.UpdateAsync(id, orgId.Value, callerUserId.Value, roles, dto, repo, db);
        }

        // Legacy singular routes with deprecation logging
        [Obsolete("Deprecated. Use /api/contacts endpoints.")]
        [HttpPost("/api/Contact")]
        public async Task<IActionResult> LegacyCreate([FromBody] Dtos.ContactCreateDto dto, [FromServices] IContactRepository repo)
        {
            _logger.LogWarning("DEPRECATED ROUTE HIT: {Route}. Please migrate to /api/contacts.", HttpContext.Request.Path);
            return await CreateContact(dto, repo);
        }

        [Obsolete("Deprecated. Use /api/contacts endpoints.")]
        [HttpPut("/api/Contact/{id}/assign")]
        public async Task<IActionResult> LegacyAssign(Guid id, [FromBody] Dtos.ContactAssignDto dto, [FromServices] IContactRepository repo, [FromServices] ILogger<ContactController> logger)
        {
            _logger.LogWarning("DEPRECATED ROUTE HIT: {Route}. Please migrate to /api/contacts/{id}/assignments.", HttpContext.Request.Path);
            return await AssignContact(id, dto, repo, logger);
        }
    }
}
