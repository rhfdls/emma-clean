namespace Emma.Core.Config
{
    /// <summary>
    /// Configuration for Azure AI Foundry integration
    /// </summary>
    public class AzureAIFoundryConfig
    {
        /// <summary>
        /// The endpoint URL for the Azure OpenAI service
        /// Example: https://your-resource-name.openai.azure.com/
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// The API key for authenticating with the Azure OpenAI service
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The deployment name for the chat model
        /// Example: gpt-4.1-2
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of tokens to generate in the response
        /// </summary>
        public int? MaxTokens { get; set; } = 1000;

        /// <summary>
        /// Controls randomness in the response generation (0.0 to 1.0)
        /// Lower values make the output more deterministic
        /// </summary>
        public float? Temperature { get; set; } = 0.7f;
    }
}
