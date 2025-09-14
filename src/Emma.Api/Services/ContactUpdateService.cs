using System.Security.Claims;
using Emma.Api.Authorization;
using Emma.Api.Dtos;
using Emma.Api.Infrastructure;
using Emma.Core.Interfaces.Repositories;
using Emma.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Emma.Api.Services
{
    /// <summary>
    /// Centralizes Contact update guardrails and ProblemDetails shapes.
    /// Returns IActionResult to allow controllers to forward the result directly.
    /// </summary>
    public class ContactUpdateService
    {
        public async Task<IActionResult> UpdateAsync(
            Guid contactId,
            Guid orgId,
            Guid callerUserId,
            IReadOnlyCollection<string> callerRoles,
            ContactUpdateDto dto,
            IContactRepository repo,
            EmmaDbContext db)
        {
            if (dto is null)
                return new ObjectResult(ProblemFactory.Create(null, 400, title: "Validation failed", detail: "Request body is required.", type: ProblemFactory.ValidationError)) { StatusCode = 400 };

            var contact = await repo.GetByIdAsync(contactId);
            if (contact is null || contact.OrganizationId != orgId)
                return new ObjectResult(ProblemFactory.Create(null, 404, title: "Contact not found", detail: $"No contact with id {contactId}.", type: ProblemFactory.NotFound)) { StatusCode = 404 };

            var isOwnerOrAdmin = ContactAccessService.IsOwnerOrAdmin(callerRoles);
            var isCurrentOwner = ContactAccessService.IsCurrentOwner(callerUserId, contact);

            // Field-level guardrails: only admins may change OwnerId or RelationshipState
            var forbiddenTouched = dto.OwnerId.HasValue || dto.RelationshipState.HasValue;
            if (forbiddenTouched && !isOwnerOrAdmin)
            {
                var blocked = new List<string>();
                if (dto.OwnerId.HasValue) blocked.Add(nameof(dto.OwnerId));
                if (dto.RelationshipState.HasValue) blocked.Add(nameof(dto.RelationshipState));
                return ApiErrors.ForbiddenFields(new ControllerBaseAdapter(), "Only admins may modify ownership or relationship state.", blocked);
            }

            if (!isOwnerOrAdmin && !isCurrentOwner)
            {
                // Must be a collaborator to edit business-only fields
                var collabs = await db.ContactCollaborators.AsNoTracking()
                    .Where(c => c.ContactId == contactId && c.OrganizationId == orgId && c.IsActive)
                    .ToListAsync();
                var isCollaborator = ContactAccessService.IsCollaborator(callerUserId, collabs);
                if (!isCollaborator)
                {
                    var pd = ProblemFactory.Create(null, 403, title: "Forbidden", detail: "Insufficient permissions to edit contact.", type: ProblemFactory.Forbidden);
                    return new ObjectResult(pd) { StatusCode = 403 };
                }

                var (ok, blocked) = ContactAccessService.ValidateCollaboratorUpdate(dto);
                if (!ok)
                    return ApiErrors.ForbiddenFields(new ControllerBaseAdapter(), "Collaborators may only modify business fields.", blocked);
            }

            // Apply updates (only provided fields)
            if (dto.FirstName != null) contact.FirstName = dto.FirstName;
            if (dto.LastName != null) contact.LastName = dto.LastName;
            if (dto.MiddleName != null) contact.MiddleName = dto.MiddleName;
            if (dto.PreferredName != null) contact.PreferredName = dto.PreferredName;
            if (dto.Title != null) contact.Title = dto.Title;
            if (dto.JobTitle != null) contact.JobTitle = dto.JobTitle;
            if (dto.Company != null) contact.Company = dto.Company;
            if (dto.Department != null) contact.Department = dto.Department;
            if (dto.Source != null) contact.Source = dto.Source;
            if (dto.OwnerId.HasValue) contact.OwnerId = dto.OwnerId.Value; // admin/owner can transfer
            if (dto.PreferredContactMethod != null) contact.PreferredContactMethod = dto.PreferredContactMethod;
            if (dto.PreferredContactTime != null) contact.PreferredContactTime = dto.PreferredContactTime;
            if (dto.Notes != null) contact.Notes = dto.Notes;
            if (dto.ProfilePictureUrl != null) contact.ProfilePictureUrl = dto.ProfilePictureUrl;

            if (dto.RelationshipState.HasValue) contact.RelationshipState = dto.RelationshipState.Value;
            if (dto.CompanyName != null) contact.CompanyName = dto.CompanyName;
            if (dto.LicenseNumber != null) contact.LicenseNumber = dto.LicenseNumber;
            if (dto.IsPreferred.HasValue) contact.IsPreferred = dto.IsPreferred.Value;
            if (dto.Website != null) contact.Website = dto.Website;

            contact.UpdatedAt = DateTime.UtcNow;
            repo.Update(contact);
            await repo.SaveChangesAsync();

            var dtoOut = new Emma.Api.Dtos.ContactReadDto
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
            return new OkObjectResult(dtoOut);
        }

        // Minimal adapter to reuse ApiErrors helpers without a real ControllerBase instance
        private sealed class ControllerBaseAdapter : ControllerBase { }
    }
}
