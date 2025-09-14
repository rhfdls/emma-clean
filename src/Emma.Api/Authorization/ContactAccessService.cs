using System;
using System.Collections.Generic;
using System.Linq;
using Emma.Models.Models;

namespace Emma.Api.Authorization
{
    // SPRINT3-API: Owner-based assignment + Collaborator business-only guardrails
    public static class ContactAccessService
    {
        public static bool IsOwnerOrAdmin(IReadOnlyCollection<string> roles)
            => roles.Any(r => string.Equals(r, "Owner", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(r, "OrgOwner", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(r, "OrgAdmin", StringComparison.OrdinalIgnoreCase));

        public static bool IsCurrentOwner(Guid userId, Contact c) => c.OwnerId == userId;

        public static bool IsCollaborator(Guid userId, IEnumerable<ContactCollaborator> collabs)
            => collabs.Any(x => x.CollaboratorUserId == userId && x.IsActive);

        // Business-only fields collaborators may edit (match ContactUpdateDto property names)
        public static readonly HashSet<string> BusinessEditableFields = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Contact.CompanyName),
            nameof(Contact.Department),
            nameof(Contact.JobTitle),
            nameof(Contact.Website),
            // Tags are segmentation-only but not part of ContactUpdateDto currently
            "Notes",
            nameof(Contact.LicenseNumber),
            nameof(Contact.IsPreferred)
        };

        // Forbidden for collaborators (ensure names match ContactUpdateDto property names)
        public static readonly HashSet<string> ForbiddenForCollaborators = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Contact.OwnerId),
            nameof(Contact.RelationshipState),
            // PII/Consent placeholders (not present in ContactUpdateDto today, but reserve names for guardrails)
            "PrimaryEmail","AlternateEmails","PrimaryPhone","AlternatePhones",
            "Address","City","Province","PostalCode",
            "Consent","DoNotCall","DoNotEmail","MarketingConsent"
        };

        public static (bool ok, string[] blocked) ValidateCollaboratorUpdate(object updateDto)
        {
            var fields = ExtractNonNullPropertyNames(updateDto);
            var blocked = fields.Where(f => ForbiddenForCollaborators.Contains(f) || !BusinessEditableFields.Contains(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return (blocked.Length == 0, blocked);
        }

        private static IEnumerable<string> ExtractNonNullPropertyNames(object dto)
            => dto.GetType().GetProperties()
                  .Where(p => p.GetValue(dto) is not null)
                  .Select(p => p.Name);
    }
}
