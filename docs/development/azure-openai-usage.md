# Azure OpenAI Integration Guide

This document provides guidance on how to configure and use the Azure OpenAI service in the Emma application.

## Configuration

The Azure OpenAI service is configured using the following environment variables:

- `AzureOpenAI__ApiKey`: The API key for authenticating with Azure OpenAI.
- `AzureOpenAI__Endpoint`: The endpoint URL for the Azure OpenAI service.
- `AzureOpenAI__DeploymentName`: The name of the deployment to use (e.g., `gpt-4.1`).
- `AzureOpenAI__ApiVersion`: The API version to use (e.g., `2023-05-15`).

### Example Configuration

```json
{
  "AzureOpenAI": {
    "ApiKey": "your-api-key-here",
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "DeploymentName": "gpt-4.1",
    "ApiVersion": "2023-05-15"
  }
}
```

## Code Usage

The Azure OpenAI client is registered as a singleton in the dependency injection container. You can inject it into your services as follows:

```csharp
public class YourService
{
    private readonly OpenAIClient _openAIClient;
    private readonly AzureOpenAIConfig _config;

    public YourService(OpenAIClient openAIClient, IOptions<AzureOpenAIConfig> config)
    {
        _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<string> GetCompletionAsync(string prompt)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = _config.DeploymentName,
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful assistant."),
                new ChatRequestUserMessage(prompt)
            },
            Temperature = 0.3f,
            MaxTokens = 500,
            ResponseFormat = new ChatCompletionsJsonResponseFormat()
        };

        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        return response.Value.Choices[0].Message.Content;
    }
}
```

## Error Handling

The following are common errors and how to resolve them:

- **401 Unauthorized**: The API key is invalid or has expired. Verify the API key in your configuration.
- **404 Not Found**: The deployment name is incorrect or the deployment does not exist. Verify the deployment name in the Azure portal.
- **429 Too Many Requests**: You have exceeded the rate limit for your Azure OpenAI service. Consider implementing rate limiting or upgrading your service tier.

## Best Practices

1. **Secure Your API Key**:
   - Never commit your API key to version control.
   - Use environment variables or a secure secret management service to store your API key.

2. **Handle Rate Limits**:
   - Implement retry logic with exponential backoff to handle rate limits.
   - Monitor your usage to avoid hitting rate limits.

3. **Logging and Monitoring**:
   - Log all API calls and responses for debugging and monitoring purposes.
   - Monitor usage and performance to identify and address issues early.

4. **Error Handling**:
   - Implement comprehensive error handling to gracefully handle API failures.
   - Provide meaningful error messages to users when something goes wrong.

## Troubleshooting

### Common Issues

1. **Incorrect Deployment Name**:
   - Ensure the deployment name matches exactly what is shown in the Azure portal.
   - The deployment name is case-sensitive.

2. **Incorrect API Version**:
   - Use the correct API version as specified in the Azure OpenAI documentation.

3. **Network Issues**:
   - Ensure your application can reach the Azure OpenAI endpoint.
   - Check for any firewall or network restrictions that might be blocking the connection.

### Getting Help

If you encounter any issues, please refer to the [Azure OpenAI documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/) or contact your system administrator.
