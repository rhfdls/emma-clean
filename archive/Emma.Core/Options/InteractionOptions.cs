namespace Emma.Core.Options
{
    /// <summary>
    /// Represents configuration options for the interaction service.
    /// </summary>
    public class InteractionOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use soft delete for interactions.
        /// When true, interactions are marked as deleted instead of being physically removed.
        /// </summary>
        public bool SoftDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets the default page size for paginated results.
        /// </summary>
        public int DefaultPageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the maximum page size allowed for paginated results.
        /// </summary>
        public int MaxPageSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the minimum confidence score for semantic search results.
        /// Results with scores below this threshold will be filtered out.
        /// </summary>
        public float MinRelevanceScore { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets the maximum number of similar interactions to return.
        /// </summary>
        public int MaxSimilarInteractions { get; set; } = 5;

        /// <summary>
        /// Gets or sets the default number of interactions to return in contact history.
        /// </summary>
        public int DefaultContactHistoryLimit { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum number of interactions to return in contact history.
        /// </summary>
        public int MaxContactHistoryLimit { get; set; } = 200;
    }
}
