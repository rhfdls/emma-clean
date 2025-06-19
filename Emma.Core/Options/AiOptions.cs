namespace Emma.Core.Options
{
    /// <summary>
    /// Represents configuration options for AI services.
    /// </summary>
    public class AiOptions
    {
        /// <summary>
        /// Gets or sets the name of the Azure OpenAI deployment to use for completions.
        /// </summary>
        public string DeploymentName { get; set; } = "gpt-4";

        /// <summary>
        /// Gets or sets the name of the Azure OpenAI deployment to use for embeddings.
        /// </summary>
        public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";

        /// <summary>
        /// Gets or sets the name of the collection to use for storing interaction embeddings.
        /// </summary>
        public string InteractionsCollection { get; set; } = "interactions";

        /// <summary>
        /// Gets or sets the name of the collection to use for storing contact embeddings.
        /// </summary>
        public string ContactsCollection { get; set; } = "contacts";

        /// <summary>
        /// Gets or sets the default temperature setting for AI completions.
        /// </summary>
        public double DefaultTemperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate in AI completions.
        /// </summary>
        public int MaxTokens { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the system prompt to use for interaction analysis.
        /// </summary>
        public string InteractionAnalysisPrompt { get; set; } = "You are an AI assistant that helps analyze and summarize interactions.";

        /// <summary>
        /// Gets or sets the system prompt to use for action item extraction.
        /// </summary>
        public string ActionItemExtractionPrompt { get; set; } = "You are an AI assistant that extracts action items from interactions.";

        /// <summary>
        /// Gets or sets the system prompt to use for sentiment analysis.
        /// </summary>
        public string SentimentAnalysisPrompt { get; set; } = "You are an AI assistant that analyzes sentiment in text.";

        /// <summary>
        /// Gets or sets the minimum confidence threshold for AI-generated content.
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.7;
    }
}
