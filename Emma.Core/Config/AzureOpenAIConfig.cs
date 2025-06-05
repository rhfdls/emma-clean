using System.ComponentModel.DataAnnotations;

namespace Emma.Core.Config;

/// <summary>
/// Configuration settings for Azure OpenAI services
/// </summary>
public class AzureOpenAIConfig
{
    /// <summary>
    /// Azure OpenAI endpoint URL
    /// </summary>
    [Required(ErrorMessage = "Azure OpenAI Endpoint is required")]
    [Url(ErrorMessage = "A valid URL is required for Azure OpenAI Endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    [Required(ErrorMessage = "Azure OpenAI API Key is required")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name for chat completions (GPT-3.5/GPT-4)
    /// </summary>
    [Required(ErrorMessage = "Azure OpenAI Chat Deployment Name is required")]
    public string ChatDeploymentName { get; set; } = "gpt-35-turbo";

    /// <summary>
    /// Deployment name for embeddings (text-embedding-ada-002)
    /// </summary>
    [Required(ErrorMessage = "Azure OpenAI Embedding Deployment Name is required")]
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Maximum tokens for chat completions
    /// </summary>
    [Range(1, 4000, ErrorMessage = "MaxTokens must be between 1 and 4000")]
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature for chat completions (0.0 to 1.0)
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Temperature must be between 0.0 and 1.0")]
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// Maximum retries for API calls
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Timeout for API calls in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Endpoint) &&
               !string.IsNullOrWhiteSpace(ApiKey) &&
               !string.IsNullOrWhiteSpace(ChatDeploymentName) &&
               !string.IsNullOrWhiteSpace(EmbeddingDeploymentName);
    }
}
