# Azure OpenAI Integration Guide

This document provides an overview of the Azure OpenAI integration in the EMMA project, including configuration, usage, and best practices.

## Configuration

The Azure OpenAI service is configured using the following environment variables:

- `AZURE_OPENAI_ENDPOINT`: The base URL of your Azure OpenAI service (e.g., `https://your-resource-name.openai.azure.com/`)
- `AZURE_OPENAI_KEY`: Your Azure OpenAI API key
- `AZURE_OPENAI_DEPLOYMENT`: The name of your deployed model (e.g., `gpt-4.1`)
- `AZURE_OPENAI_API_VERSION`: (Optional) The API version to use (default: `2023-05-15`)

These can be set in your environment, in `appsettings.json`, or in a `.env` file for local development.

## Service Registration

The Azure OpenAI client is registered in the DI container during application startup. The registration includes:

1. Configuration validation
2. HTTP client configuration with retry policies
3. Health check registration

## Usage

### Basic Usage

Inject the `OpenAIClient` into your service:

```csharp
public class MyService
{
    private readonly OpenAIClient _openAIClient;
    
    public MyService(OpenAIClient openAIClient)
    {
        _openAIClient = openAIClient;
    }
    
    public async Task<string> GetCompletionAsync(string prompt)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            DeploymentName = "your-deployment-name",
            Messages =
            {
                new ChatRequestSystemMessage("You are a helpful assistant."),
                new ChatRequestUserMessage(prompt)
            }
        };
        
        var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
        return response.Value.Choices[0].Message.Content;
    }
}
```

### Using the EmmaAgentService

The `EmmaAgentService` is a higher-level service that handles common patterns for EMMA's AI interactions:

```csharp
public class MyService
{
    private readonly IEmmaAgentService _agentService;
    
    public MyService(IEmmaAgentService agentService)
    {
        _agentService = agentService;
    }
    
    public async Task<EmmaResponseDto> ProcessMessageAsync(string message)
    {
        return await _agentService.ProcessMessageAsync(message);
    }
}
```

## Error Handling

The integration includes comprehensive error handling for common scenarios:

- **Authentication failures**: Invalid API key or endpoint
- **Rate limiting**: Automatic retries with exponential backoff
- **Model deployment not found**: Clear error message about the missing deployment
- **Invalid responses**: Graceful handling of malformed responses from the API

## Best Practices

1. **Configuration Management**:
   - Never commit API keys to source control
   - Use environment variables for sensitive data
   - Validate configuration at startup

2. **Error Handling**:
   - Always handle potential exceptions from the Azure OpenAI client
   - Implement retry logic for transient failures
   - Log meaningful error messages

3. **Performance**:
   - Reuse the `OpenAIClient` instance (it's thread-safe)
   - Set appropriate timeouts
   - Consider implementing caching for frequent, similar requests

4. **Security**:
   - Use managed identities when running in Azure
   - Implement proper access controls
   - Monitor usage and set up alerts for unusual activity

## Testing

When writing tests, you can mock the `OpenAIClient` or `IEmmaAgentService` as needed. The `Azure.AI.OpenAI` package includes test doubles that can be used for unit testing.

## Troubleshooting

Common issues and solutions:

1. **Authentication Errors**:
   - Verify your API key and endpoint are correct
   - Check that your Azure subscription is active
   - Ensure the API key has the correct permissions

2. **Model Deployment Not Found**:
   - Verify the deployment name matches exactly
   - Check that the deployment exists in the specified region
   - Ensure the API version is compatible with your deployment

3. **Rate Limiting**:
   - Implement retry logic with exponential backoff
   - Consider upgrading your Azure OpenAI service tier
   - Review and optimize your usage patterns

## Monitoring and Logging

The integration includes detailed logging for troubleshooting:

- Request/response logging (at debug level)
- Error logging with correlation IDs
- Performance metrics for API calls

## Dependencies

- `Azure.AI.OpenAI`: The official Azure OpenAI SDK
- `Polly`: For retry and circuit breaker policies
- `Microsoft.Extensions.Http.Polly`: For HTTP client resilience

## Version History

- **1.0.0**: Initial implementation with basic chat completion support
- **1.1.0**: Added retry policies and improved error handling

## See Also

- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net)
- [Polly Documentation](https://github.com/App-vNext/Polly)
