namespace Emma.Core.Config
{
    /// <summary>
    /// Minimal Azure OpenAI configuration used by EmmaAgentService and tests.
    /// </summary>
    public class AzureOpenAIConfig
    {
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
        public string ChatDeploymentName { get; set; } = "";
        public float Temperature { get; set; } = 0.2f;
        public int MaxTokens { get; set; } = 256;
    }
}
