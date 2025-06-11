using Emma.Data;
using Emma.Data.Models;
using Emma.Data.Enums;
using Emma.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services;

/// <summary>
/// Service for Contact data access and management.
/// Provides unified contact operations for all relationship types.
/// </summary>
public class ContactService : IContactService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ContactService> _logger;

    public ContactService(AppDbContext context, ILogger<ContactService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets contacts by their relationship state(s) within an organization
    /// </summary>
    public async Task<IEnumerable<Contact>> GetContactsByRelationshipStateAsync(
        Guid organizationId, 
        IEnumerable<RelationshipState> relationshipStates)
    {
        var statesList = relationshipStates.ToList();
        
        var contacts = await _context.Contacts
            .Where(c => c.OrganizationId == organizationId && 
                       statesList.Contains(c.RelationshipState))
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} contacts for organization {OrganizationId} with relationship states: {States}",
            contacts.Count, organizationId, string.Join(", ", statesList));

        return contacts;
    }

    /// <summary>
    /// Gets a contact by ID
    /// </summary>
    public async Task<Contact?> GetContactByIdAsync(Guid contactId)
    {
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId);

        _logger.LogDebug("Retrieved contact {ContactId}: {Found}", 
            contactId, contact != null ? "Found" : "Not Found");

        return contact;
    }

    /// <summary>
    /// Gets all contacts for an organization
    /// </summary>
    public async Task<IEnumerable<Contact>> GetContactsByOrganizationAsync(Guid organizationId)
    {
        var contacts = await _context.Contacts
            .Where(c => c.OrganizationId == organizationId)
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} contacts for organization {OrganizationId}",
            contacts.Count, organizationId);

        return contacts;
    }

    /// <summary>
    /// Creates a new contact
    /// </summary>
    public async Task<Contact> CreateContactAsync(Contact contact)
    {
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new contact {ContactId} for organization {OrganizationId}",
            contact.Id, contact.OrganizationId);

        return contact;
    }

    /// <summary>
    /// Updates an existing contact
    /// </summary>
    public async Task<Contact> UpdateContactAsync(Contact contact)
    {
        _context.Contacts.Update(contact);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated contact {ContactId}", contact.Id);

        return contact;
    }

    /// <summary>
    /// Deletes a contact
    /// </summary>
    public async Task DeleteContactAsync(Guid contactId)
    {
        var contact = await _context.Contacts.FindAsync(contactId);
        if (contact != null)
        {
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted contact {ContactId}", contactId);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent contact {ContactId}", contactId);
        }
    }

    /// <summary>
    /// Searches contacts by various criteria
    /// </summary>
    public async Task<IEnumerable<Contact>> SearchContactsAsync(
        Guid organizationId,
        string? searchTerm = null,
        IEnumerable<RelationshipState>? relationshipStates = null,
        IEnumerable<string>? specialties = null,
        IEnumerable<string>? serviceAreas = null,
        decimal? minRating = null,
        bool? isPreferred = null,
        int? maxResults = null)
    {
        var query = _context.Contacts
            .Where(c => c.OrganizationId == organizationId);

        // Filter by relationship states
        if (relationshipStates != null)
        {
            var statesList = relationshipStates.ToList();
            if (statesList.Any())
            {
                query = query.Where(c => statesList.Contains(c.RelationshipState));
            }
        }

        // Convert search term to lowercase using invariant culture
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(c => 
                (c.FirstName != null && c.FirstName.ToLowerInvariant().Contains(term)) ||
                (c.LastName != null && c.LastName.ToLowerInvariant().Contains(term)) ||
                (c.CompanyName != null && c.CompanyName.ToLowerInvariant().Contains(term)) ||
                c.Emails.Any(e => e.ToLowerInvariant().Contains(term)));
        }

        // Filter by specialties
        if (specialties != null)
        {
            var specialtiesList = specialties.ToList();
            if (specialtiesList.Any())
            {
                query = query.Where(c => c.Specialties.Any(s => specialtiesList.Contains(s)));
            }
        }

        // Filter by service areas
        if (serviceAreas != null)
        {
            var serviceAreasList = serviceAreas.ToList();
            if (serviceAreasList.Any())
            {
                query = query.Where(c => c.ServiceAreas.Any(sa => serviceAreasList.Contains(sa)));
            }
        }

        // Filter by minimum rating
        if (minRating.HasValue)
        {
            query = query.Where(c => c.Rating.HasValue && c.Rating.Value >= minRating.Value);
        }

        // Filter by preferred status
        if (isPreferred.HasValue)
        {
            query = query.Where(c => c.IsPreferred == isPreferred.Value);
        }

        // Apply ordering - preferred first, then by rating, then by name
        query = query.OrderByDescending(c => c.IsPreferred)
                    .ThenByDescending(c => c.Rating ?? 0)
                    .ThenBy(c => c.FirstName)
                    .ThenBy(c => c.LastName);

        // Apply result limit
        if (maxResults.HasValue && maxResults.Value > 0)
        {
            query = query.Take(maxResults.Value);
        }

        var results = await query.ToListAsync();

        _logger.LogDebug("Search returned {Count} contacts for organization {OrganizationId} with criteria: " +
            "searchTerm={SearchTerm}, states={States}, specialties={Specialties}, serviceAreas={ServiceAreas}, " +
            "minRating={MinRating}, isPreferred={IsPreferred}, maxResults={MaxResults}",
            results.Count, organizationId, searchTerm, 
            relationshipStates != null ? string.Join(",", relationshipStates) : "null",
            specialties != null ? string.Join(",", specialties) : "null",
            serviceAreas != null ? string.Join(",", serviceAreas) : "null",
            minRating, isPreferred, maxResults);

        return results;
    }
}
