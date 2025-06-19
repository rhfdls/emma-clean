using Emma.Models.Models;
using Emma.Models.Enums;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Interface for Resource management service
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// Discover available Resources (service providers) based on filters.
        /// </summary>
        Task<List<Contact>> DiscoverResourcesAsync(
            string? specialty = null,
            string? serviceArea = null,
            decimal? minRating = null,
            bool? isPreferred = null);

        /// <summary>
        /// Get Resources with the highest performance scores for a given specialty.
        /// </summary>
        Task<List<Contact>> GetTopPerformingResourcesAsync(string specialty, int count = 5);

        /// <summary>
        /// Assign a Resource to a client for a specific purpose.
        /// </summary>
        Task<ContactAssignment> AssignResourceAsync(
            Guid clientContactId,
            Guid serviceContactId,
            Guid assignedByAgentId,
            Guid organizationId,
            string purpose,
            ResourceAssignmentStatus status = ResourceAssignmentStatus.Active,
            Priority priority = Priority.Normal);

        /// <summary>
        /// Find Resources matching specific criteria for assignment recommendations.
        /// </summary>
        Task<List<Contact>> FindMatchingResourcesAsync(
            Guid organizationId,
            string specialty,
            string? serviceArea = null,
            decimal? minRating = null,
            int maxResults = 10);

        /// <summary>
        /// Get Resource assignments with specific filters.
        /// </summary>
        Task<List<ContactAssignment>> GetResourceAssignmentsAsync(
            Guid? clientContactId = null,
            Guid? serviceContactId = null,
            ResourceAssignmentStatus? status = null);

        /// <summary>
        /// Get Resource assignments for a Contact with compliance status.
        /// </summary>
        Task<List<ContactAssignment>> GetResourceAssignmentsAsync(
            Guid contactId,
            bool includeCompleted = false);

        /// <summary>
        /// Get Resource performance metrics for reporting.
        /// </summary>
        Task<object> GetResourceMetricsAsync(Guid resourceId);

        /// <summary>
        /// Update Resource rating based on client feedback.
        /// </summary>
        Task UpdateResourceRatingAsync(Guid resourceId, decimal rating, string? feedback = null);

        /// <summary>
        /// Mark referral disclosure as provided for a Resource assignment.
        /// </summary>
        Task MarkDisclosureProvidedAsync(
            Guid assignmentId,
            string method,
            string? disclaimerText = null);

        /// <summary>
        /// Marks liability disclaimer as acknowledged for a Resource assignment.
        /// </summary>
        Task MarkLiabilityDisclaimerAcknowledged(Guid assignmentId);
    }
}
