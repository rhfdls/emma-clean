namespace Emma.Api.Configuration;

public class AzureOpenAIConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChatDeploymentName { get; set; } = "gpt-4";
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";
    public int MaxTokens { get; set; } = 4000;
    public double Temperature { get; set; } = 0.7;
}
