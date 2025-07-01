using System;
using System.Collections.Generic;

namespace Emma.Api.Dtos
{
    /// <summary>
    /// Represents search criteria for filtering and paginating interactions.
    /// </summary>
    public class InteractionSearchDto
    {
        /// <summary>
        /// Search term to match against interaction content, subject, or summary.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by contact ID.
        /// </summary>
        public Guid? ContactId { get; set; }


        /// <summary>
        /// Filter by agent/user ID who is assigned to the interaction.
        /// </summary>
        public Guid? AssignedToId { get; set; }

        /// <summary>
        /// Filter by interaction type (e.g., 'email', 'call', 'meeting', 'task').
        /// </summary>
        public List<string>? Types { get; set; }

        /// <summary>
        /// Filter by interaction status (e.g., 'new', 'in-progress', 'completed', 'archived').
        /// </summary>
        public List<string>? Statuses { get; set; }

        /// <summary>
        /// Filter by interaction priority (e.g., 'low', 'normal', 'high', 'urgent').
        /// </summary>
        public List<string>? Priorities { get; set; }

        /// <summary>
        /// Filter by interaction channel (e.g., 'email', 'phone', 'sms', 'in-person').
        /// </summary>
        public List<string>? Channels { get; set; }

        /// <summary>
        /// Filter by privacy level (e.g., 'public', 'internal', 'confidential', 'private').
        /// </summary>
        public List<string>? PrivacyLevels { get; set; }

        /// <summary>
        /// Filter by confidentiality level (e.g., 'standard', 'sensitive', 'restricted').
        /// </summary>
        public List<string>? ConfidentialityLevels { get; set; }

        /// <summary>
        /// Filter by tags associated with the interaction.
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Filter interactions that require follow-up.
        /// </summary>
        public bool? FollowUpRequired { get; set; }

        /// <summary>
        /// Filter interactions that are starred/flagged.
        /// </summary>
        public bool? IsStarred { get; set; }

        /// <summary>
        /// Filter interactions that are read/unread.
        /// </summary>
        public bool? IsRead { get; set; }

        /// <summary>
        /// Filter by start date range (inclusive).
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Filter by end date range (inclusive).
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Filter by follow-up due date range (inclusive).
        /// </summary>
        public DateTime? FollowUpByStart { get; set; }

        /// <summary>
        /// Filter by follow-up due date range (inclusive).
        /// </summary>
        public DateTime? FollowUpByEnd { get; set; }

        /// <summary>
        /// The field to sort by (e.g., 'createdAt', 'updatedAt', 'priority', 'status').
        /// </summary>
        public string? SortBy { get; set; } = "createdAt";

        /// <summary>
        /// The sort direction ('asc' or 'desc').
        /// </summary>
        public string? SortDirection { get; set; } = "desc";

        /// <summary>
        /// The page number for pagination (1-based).
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}
