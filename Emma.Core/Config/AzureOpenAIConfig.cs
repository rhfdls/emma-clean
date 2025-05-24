using System.ComponentModel.DataAnnotations;

namespace Emma.Core.Config;

/// <summary>
/// Configuration options for Azure OpenAI service.
/// </summary>
public class AzureOpenAIConfig
{
    /// <summary>
    /// Gets or sets the API key for authenticating with Azure OpenAI.
    /// </summary>
    /// <value>The API key as a string.</value>
    [Required(ErrorMessage = "Azure OpenAI API Key is required")]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base endpoint URL for the Azure OpenAI service.
    /// Example: https://your-resource-name.openai.azure.com/
    /// </summary>
    [Required(ErrorMessage = "Azure OpenAI Endpoint is required")]
    [Url(ErrorMessage = "A valid URL is required for Azure OpenAI Endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the deployment to use.
    /// This should match the deployment name in the Azure OpenAI portal.
    /// Example: gpt-4.1
    /// </summary>
    [Required(ErrorMessage = "Azure OpenAI Deployment Name is required")]
    public string DeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API version to use.
    /// Defaults to 2023-05-15 which is the latest stable version as of the last update.
    /// </summary>
    [Required(ErrorMessage = "API Version is required")]
    public string ApiVersion { get; set; } = "2023-05-15";

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the response.
    /// Defaults to 500 tokens.
    /// </summary>
    [Range(1, 4000, ErrorMessage = "MaxTokens must be between 1 and 4000")]
    public int MaxTokens { get; set; } = 500;

    /// <summary>
    /// Gets or sets the temperature setting for the model.
    /// Lower values make the output more focused and deterministic.
    /// Range: 0.0 to 2.0
    /// </summary>
    [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0")]
    public float Temperature { get; set; } = 0.3f;

    /// <summary>
    /// Gets or sets the top-p sampling parameter.
    /// Range: 0.0 to 1.0
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "TopP must be between 0.0 and 1.0")]
    public float TopP { get; set; } = 0.9f;

    /// <summary>
    /// Gets the base URL for API requests.
    /// This combines the endpoint with the API version.
    /// </summary>
    public string BaseUrl => $"{Endpoint.TrimEnd('/')}?api-version={ApiVersion}";

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ApiKey) &&
               !string.IsNullOrWhiteSpace(Endpoint) &&
               !string.IsNullOrWhiteSpace(DeploymentName) &&
               !string.IsNullOrWhiteSpace(ApiVersion) &&
               Temperature >= 0.0f && Temperature <= 2.0f &&
               TopP >= 0.0f && TopP <= 1.0f;
    }
}
