using Emma.Data;
using Emma.Data.Models;
using Emma.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services;

/// <summary>
/// Service for managing Resources (service providers) and their assignments.
/// Provides enhanced Resource discovery, filtering, and compliance tracking.
/// </summary>
public class ResourceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(AppDbContext context, ILogger<ResourceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Discover available Resources (service providers) based on filters.
    /// </summary>
    public async Task<List<Contact>> DiscoverResourcesAsync(
        string? specialty = null,
        string? serviceArea = null,
        decimal? minRating = null,
        bool? isPreferred = null)
    {
        var query = _context.Contacts
            .Where(c => c.RelationshipState == RelationshipState.ServiceProvider);

        // Apply filters
        if (!string.IsNullOrEmpty(specialty))
        {
            query = query.Where(c => c.Specialties.Any(s => s.Contains(specialty)));
        }

        if (!string.IsNullOrEmpty(serviceArea))
        {
            query = query.Where(c => c.ServiceAreas.Any(sa => sa.Contains(serviceArea)));
        }

        if (minRating.HasValue)
        {
            query = query.Where(c => c.Rating.HasValue && c.Rating.Value >= minRating.Value);
        }

        if (isPreferred.HasValue)
        {
            query = query.Where(c => c.IsPreferred == isPreferred.Value);
        }

        return await query
            .OrderByDescending(c => c.GetResourcePerformanceScore())
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    /// <summary>
    /// Get Resources with the highest performance scores for a given specialty.
    /// </summary>
    public async Task<List<Contact>> GetTopPerformingResourcesAsync(string specialty, int count = 5)
    {
        var resources = await _context.Contacts
            .Where(c => c.RelationshipState == RelationshipState.ServiceProvider &&
                       c.Specialties.Any(s => s.Contains(specialty)))
            .ToListAsync();

        return resources
            .OrderByDescending(r => r.GetResourcePerformanceScore())
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Assign a Resource to a client for a specific purpose.
    /// </summary>
    public async Task<ContactAssignment> AssignResourceAsync(
        Guid clientContactId,
        Guid serviceContactId,
        Guid assignedByAgentId,
        Guid organizationId,
        string purpose,
        ResourceAssignmentStatus status = ResourceAssignmentStatus.Active,
        Priority priority = Priority.Normal)
    {
        // Validate the service contact is actually a Resource
        var serviceContact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == serviceContactId);

        if (serviceContact == null || !serviceContact.IsResource())
        {
            throw new ArgumentException("Service contact must be a valid Resource (ServiceProvider)");
        }

        var assignment = new ContactAssignment
        {
            ClientContactId = clientContactId,
            ServiceContactId = serviceContactId,
            AssignedByAgentId = assignedByAgentId,
            OrganizationId = organizationId,
            Purpose = purpose,
            Status = status,
            Priority = priority
        };

        _context.ContactAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Resource assigned: ServiceContact {ServiceContactId} to Client {ClientContactId} for {Purpose}",
            serviceContactId, clientContactId, purpose);

        return assignment;
    }

    /// <summary>
    /// Find Resources matching specific criteria for assignment recommendations.
    /// </summary>
    public async Task<List<Contact>> FindMatchingResourcesAsync(
        Guid organizationId,
        string specialty,
        string? serviceArea = null,
        decimal? minRating = null,
        int maxResults = 10)
    {
        var resources = await DiscoverResourcesAsync(specialty, serviceArea, minRating);
        
        return resources
            .Where(r => r.MatchesResourceCriteria(specialty, serviceArea, minRating))
            .OrderByDescending(r => r.GetResourcePerformanceScore())
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Get Resource assignments with specific filters.
    /// </summary>
    public async Task<List<ContactAssignment>> GetResourceAssignmentsAsync(
        Guid? clientContactId = null,
        Guid? serviceContactId = null,
        ResourceAssignmentStatus? status = null)
    {
        var query = _context.ContactAssignments.AsQueryable();

        if (clientContactId.HasValue)
        {
            query = query.Where(ca => ca.ClientContactId == clientContactId.Value);
        }

        if (serviceContactId.HasValue)
        {
            query = query.Where(ca => ca.ServiceContactId == serviceContactId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(ca => ca.Status == status.Value);
        }

        return await query
            .Include(ca => ca.ClientContact)
            .Include(ca => ca.ServiceContact)
            .Include(ca => ca.AssignedByAgent)
            .ToListAsync();
    }

    /// <summary>
    /// Get Resource assignments for a Contact with compliance status.
    /// </summary>
    public async Task<List<ContactAssignment>> GetResourceAssignmentsAsync(
        Guid contactId,
        bool includeCompleted = false)
    {
        var query = _context.ContactAssignments
            .Include(ca => ca.ServiceContact)
            .Include(ca => ca.AssignedByAgent)
            .Where(ca => ca.ClientContactId == contactId);

        if (!includeCompleted)
        {
            query = query.Where(ca => ca.Status != ResourceAssignmentStatus.Completed && ca.Status != ResourceAssignmentStatus.Cancelled);
        }

        return await query
            .OrderByDescending(ca => ca.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get Resource performance metrics for reporting.
    /// </summary>
    public async Task<object> GetResourceMetricsAsync(Guid resourceId)
    {
        var resource = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == resourceId && c.IsResource());

        if (resource == null)
        {
            throw new ArgumentException("Resource not found");
        }

        var assignments = await _context.ContactAssignments
            .Where(ca => ca.ServiceContactId == resourceId)
            .ToListAsync();

        var completedAssignments = assignments.Where(a => a.Status == ResourceAssignmentStatus.Completed).ToList();
        var complianceCompleteCount = assignments.Count(a => a.IsComplianceComplete());

        return new
        {
            ResourceId = resourceId,
            ResourceName = $"{resource.FirstName} {resource.LastName}",
            CompanyName = resource.CompanyName,
            PerformanceScore = resource.GetResourcePerformanceScore(),
            Rating = resource.Rating,
            ReviewCount = resource.ReviewCount,
            TotalAssignments = assignments.Count,
            CompletedAssignments = completedAssignments.Count,
            ComplianceRate = assignments.Count > 0 ? (decimal)complianceCompleteCount / assignments.Count * 100 : 0,
            IsPreferred = resource.IsPreferred,
            Specialties = resource.Specialties?.ToList() ?? new List<string>(),
            ServiceAreas = resource.ServiceAreas?.ToList() ?? new List<string>()
        };
    }

    /// <summary>
    /// Update Resource rating based on client feedback.
    /// </summary>
    public async Task UpdateResourceRatingAsync(Guid resourceId, decimal rating, string? feedback = null)
    {
        var resource = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Id == resourceId && c.IsResource());

        if (resource == null)
        {
            throw new ArgumentException("Resource not found");
        }

        resource.UpdateRating(rating);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rating {Rating} added to Resource {ResourceId}. New average: {NewRating}", 
            rating, resourceId, resource.Rating);
    }

    /// <summary>
    /// Mark referral disclosure as provided for a Resource assignment.
    /// </summary>
    public async Task MarkDisclosureProvidedAsync(
        Guid assignmentId,
        string method,
        string? disclaimerText = null)
    {
        var assignment = await _context.ContactAssignments
            .FirstOrDefaultAsync(ca => ca.Id == assignmentId);

        if (assignment == null)
        {
            throw new ArgumentException("Assignment not found");
        }

        assignment.MarkReferralDisclosureProvided(method, disclaimerText);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Referral disclosure marked as provided for Assignment {AssignmentId} via {Method}", 
            assignmentId, method);
    }

    /// <summary>
    /// Marks liability disclaimer as acknowledged for a Resource assignment.
    /// </summary>
    public async Task MarkLiabilityDisclaimerAcknowledged(Guid assignmentId)
    {
        var assignment = await _context.ContactAssignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            throw new ArgumentException("Assignment not found");
        }

        assignment.AcknowledgeLiabilityDisclaimer();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Liability disclaimer acknowledged for assignment {AssignmentId}", assignmentId);
    }
}
