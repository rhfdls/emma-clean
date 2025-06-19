using System;
using System.Collections.Generic;

namespace Emma.Api.Dtos
{
    /// <summary>
    /// Represents criteria for semantic search of interactions using natural language.
    /// </summary>
    public class SemanticSearchDto
    {
        /// <summary>
        /// The natural language query to search for.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional contact ID to scope the search to a specific contact's interactions.
        /// </summary>
        public Guid? ContactId { get; set; }

        /// <summary>
        /// Optional user ID to scope the search to interactions assigned to a specific user.
        /// </summary>
        public Guid? AssignedToId { get; set; }

        /// <summary>
        /// Optional list of interaction types to include in the search.
        /// </summary>
        public List<string>? Types { get; set; }

        /// <summary>
        /// Optional date range start to filter interactions by creation date.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optional date range end to filter interactions by creation date.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Optional list of tags to filter interactions.
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// Optional privacy level to filter interactions.
        /// </summary>
        public string? PrivacyLevel { get; set; }

        /// <summary>
        /// Optional confidentiality level to filter interactions.
        /// </summary>
        public string? Confidentiality { get; set; }

        /// <summary>
        /// The maximum number of results to return (default: 10).
        /// </summary>
        public int MaxResults { get; set; } = 10;

        /// <summary>
        /// The minimum relevance score (0-1) for results to be included (default: 0.7).
        /// </summary>
        public double MinRelevanceScore { get; set; } = 0.7;

        /// <summary>
        /// Whether to include interaction content in the search (default: true).
        /// </summary>
        public bool IncludeContent { get; set; } = true;

        /// <summary>
        /// Whether to include interaction summaries in the search (default: true).
        /// </summary>
        public bool IncludeSummaries { get; set; } = true;

        /// <summary>
        /// Whether to include action items in the search (default: true).
        /// </summary>
        public bool IncludeActionItems { get; set; } = true;

        /// <summary>
        /// Whether to include tags in the search (default: true).
        /// </summary>
        public bool IncludeTags { get; set; } = true;
    }
}
