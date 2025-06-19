# Azure OpenAI Integration Guide

This document provides comprehensive guidance for integrating and using Azure OpenAI services within the EMMA platform, including configuration, implementation details, and best practices.

## Table of Contents

- [Configuration](#configuration)
- [Service Registration](#service-registration)
- [Usage](#usage)
- [Error Handling and Resilience](#error-handling-and-resilience)
- [Best Practices](#best-practices)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Multi-tenant Considerations](#multi-tenant-considerations)
- [Monitoring and Logging](#monitoring-and-logging)
- [Version History](#version-history)

## Configuration

The Azure OpenAI service is configured using the `AzureOpenAIConfig` class through the application's configuration system.

### Configuration Properties

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key",
    "ChatDeploymentName": "your-deployment-name",
    "ApiVersion": "2023-05-15",
    "MaxTokens": 800,
    "Temperature": 0.7,
    "TenantId": "your-tenant-id",
    "UseManagedIdentity": false,
    "OverrideSettings": {
      "RequiresApproval": true,
      "ApprovalWorkflow": "default",
      "AuditLogging": true
    }
  }
}
```

#### Core Properties

| Property | Type | Description |
|----------|------|-------------|
| `Endpoint` | string | The Azure OpenAI service endpoint URL |
| `ApiKey` | string | API key for authentication (use Key Vault in production) |
| `ChatDeploymentName` | string | Name of the deployed chat model |
| `ApiVersion` | string | API version (e.g., "2023-05-15") |
| `MaxTokens` | int | Maximum tokens to generate in responses |
| `Temperature` | float | Controls response randomness (0-2) |

#### Advanced Properties

| Property | Type | Description |
|----------|------|-------------|
| `TenantId` | string | Azure AD tenant ID for multi-tenant auth |
| `UseManagedIdentity` | bool | Use managed identity instead of API key |

#### Override Settings

| Property | Type | Description |
|----------|------|-------------|
| `RequiresApproval` | bool | Whether overrides need approval |
| `ApprovalWorkflow` | string | Workflow for handling approvals |
| `AuditLogging` | bool | Enable audit logging for overrides |

### Configuration Validation

The following validation rules are applied at startup:

- **Endpoint**: Must be a valid absolute URI
- **ApiKey**: Required when not using managed identity
- **ChatDeploymentName**: Must be a non-empty string
- **ApiVersion**: Must be a valid API version string
- **MaxTokens**: Must be between 1 and 4000
- **Temperature**: Must be between 0 and 2

## Service Registration

The Azure OpenAI client is registered in the DI container using the `AddAzureOpenAI()` extension method in `Program.cs`:

```csharp
// In Program.cs
builder.Services.AddAzureOpenAI(builder.Configuration);
```

This registration includes:

1. Configuration validation using data annotations
2. HTTP client configuration with retry policies (exponential backoff)
3. Health check registration for monitoring service availability
4. Automatic retry logic for transient failures
5. Detailed logging for troubleshooting

## Usage

### Using the EmmaAgentService

The `EmmaAgentService` is the primary way to interact with Azure OpenAI in the EMMA project. It provides a higher-level abstraction over the Azure OpenAI client with built-in retry policies and error handling.

```csharp
public class MyService
{
    private readonly IEmmaAgentService _agentService;
    private readonly ILogger<MyService> _logger;
    
    public MyService(IEmmaAgentService agentService, ILogger<MyService> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }
    
    public async Task ProcessInteractionAsync(string userMessage)
    {
        try
        {
            var response = await _agentService.ProcessMessageAsync(userMessage);
            
            // Handle the response based on the action type
            switch (response.Action)
            {
                case "sendemail":
                    await HandleEmailAction(response.Payload);
                    break;
                    
                case "schedulefollowup":
                    await ScheduleFollowUp(response.Payload);
                    break;
                    
                case "none":
                    _logger.LogInformation("No action required for this message");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            throw;
        }
    }
}
```

### Direct OpenAIClient Usage (Advanced)

For advanced scenarios, you can inject the `OpenAIClient` directly:

```csharp
public class AdvancedAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<AdvancedAIService> _logger;
    private readonly string _deploymentName;
    
    public AdvancedAIService(
        OpenAIClient openAIClient,
        IOptions<AzureOpenAIConfig> config,
        ILogger<AdvancedAIService> logger)
    {
        _openAIClient = openAIClient;
        _logger = logger;
        _deploymentName = config.Value.ChatDeploymentName;
    }

    public async Task<string> GetCompletionAsync(string systemPrompt, string userPrompt)
    {
        var options = new ChatCompletionsOptions
        {
            DeploymentName = _deploymentName,
            Messages =
            {
                new ChatRequestSystemMessage(systemPrompt),
                new ChatRequestUserMessage(userPrompt)
            },
            Temperature = 0.7f,
            MaxTokens = 800
        };
        
        try
        {
            var response = await _openAIClient.GetChatCompletionsAsync(options);
            return response.Value.Choices[0].Message.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger.LogWarning("Rate limit exceeded. Consider implementing backoff or rate limiting.");
            throw;
        }
    }
}
```

## Error Handling and Resilience

The integration includes comprehensive error handling and resilience patterns:

### Automatic Retry Policies

The service is configured with Polly policies that automatically handle transient failures:
- **Exponential backoff** for rate limiting (HTTP 429) and server errors (5xx)
- Maximum of 3 retry attempts
- Network timeout of 30 seconds per request

### Common Exceptions

Handle these exceptions in your code:

```csharp
try 
{
    // Your OpenAI API call
} 
catch (RequestFailedException ex) when (ex.Status == 429) 
{
    // Rate limit exceeded
    _logger.LogWarning("Rate limit exceeded. Consider implementing backoff.");
    throw;
}
catch (RequestFailedException ex) when (ex.Status >= 500)
{
    // Server error
    _logger.LogError(ex, "OpenAI service error");
    throw;
}
catch (Exception ex)
{
    // Other errors
    _logger.LogError(ex, "Unexpected error calling OpenAI API");
    throw;
}
```

### Health Checks

The integration includes health checks that can be used to monitor the service status:

```csharp
// In Program.cs
app.MapHealthChecks("/health");
```

This will return a 200 OK status when the service is healthy and can connect to Azure OpenAI.

## Best Practices

### Configuration Management

1. **Secrets Management**
   - Store API keys in Azure Key Vault or a secure secret store
   - Use User Secrets for local development
   - Never commit secrets to source control

2. **Error Handling**
   - Always wrap OpenAI calls in try-catch blocks
   - Log detailed error information including correlation IDs
   - Implement circuit breakers for cascading failures

3. **Performance Optimization**
   - Reuse the `OpenAIClient` instance (registered as singleton)
   - Set appropriate timeouts and retry policies
   - Implement response caching for deterministic requests
   - Batch similar requests when possible

4. **Security**
   - Use Managed Identity when running in Azure
   - Implement proper RBAC for Azure OpenAI resources
   - Enable diagnostic logging and monitoring
   - Set up alerts for unusual activity patterns
   - Regularly rotate API keys

5. **Cost Management**
   - Monitor token usage and costs
   - Set up budget alerts in Azure
   - Consider implementing rate limiting at the application level

## Testing

### Unit Testing

Mock the `IEmmaAgentService` in your unit tests:

```csharp
public class MyServiceTests
{
    private readonly Mock<IEmmaAgentService> _mockAgentService;
    private readonly MyService _service;
    
    public MyServiceTests()
    {
        _mockAgentService = new Mock<IEmmaAgentService>();
        _service = new MyService(_mockAgentService.Object, Mock.Of<ILogger<MyService>>());
    }
    
    [Fact]
    public async Task ProcessInteraction_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var expectedResponse = new EmmaResponseDto 
        { 
            Action = "sendemail", 
            Payload = "Test email content" 
        };
        
        _mockAgentService
            .Setup(x => x.ProcessMessageAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);
            
        // Act
        await _service.ProcessInteractionAsync("Test message");
        
        // Assert
        _mockAgentService.Verify(x => x.ProcessMessageAsync("Test message"), Times.Once);
    }
}
```

### Integration Testing

For integration tests, use the test server with a mock HTTP handler:

```csharp
public class AzureOpenAIIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public AzureOpenAIIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace real services with test doubles
                services.AddSingleton<IOpenAIService, MockOpenAIService>();
            });
        });
    }
    
    [Fact]
    public async Task GetCompletion_ReturnsExpectedResponse()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.PostAsJsonAsync("/api/chat", new { message = "Hello" });
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("expected response", content);
    }
}
```

## Troubleshooting

### Common Issues and Solutions

1. **Authentication Failures (401/403)**
   - Verify the API key in your configuration
   - Check that the endpoint URL is correct and accessible
   - Ensure the API key has the correct permissions in Azure
   - For managed identity, verify the identity has the "Cognitive Services OpenAI User" role

2. **Model Deployment Not Found (404)**
   - Verify the deployment name in `AzureOpenAIConfig`
   - Check that the deployment exists in the Azure portal
   - Ensure the deployment is in the same region as your resource
   - Verify the deployment is in the "Succeeded" state

3. **Rate Limiting (429)**
   - The client automatically retries with exponential backoff
   - Check your Azure OpenAI service quotas and limits
   - Consider upgrading your service tier if hitting limits frequently
   - Implement application-level rate limiting if needed

4. **Timeouts**
   - Default timeout is 30 seconds per request
   - For complex prompts, consider increasing the timeout
   - Check network connectivity to the Azure OpenAI endpoint
   - Verify there are no firewall rules blocking the connection

5. **High Latency**
   - Deploy your application in the same Azure region as your OpenAI resource
   - Use the latest version of the Azure.AI.OpenAI package
   - Consider implementing response caching for repeated requests

### Diagnostic Logging

Enable detailed logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Azure": "Warning",
      "Azure.AI.OpenAI": "Debug"
    }
  }
}
```

### Azure Monitor

Set up Azure Monitor to track:
- Request latency
- Token usage
- Error rates
- Throttling events

## Multi-tenant Considerations

When deploying in a multi-tenant environment, consider the following aspects:

### API Key Management

- Tenant-specific API keys for isolation
- Shared keys with tenant-based rate limiting
- Per-tenant model deployment options

### Data Isolation

- Ensure prompt and completion data is properly scoped to tenants
- Implement tenant-aware logging and monitoring
- Support tenant-specific model fine-tuning

### User Override Integration

- Support for different override modes per tenant
- Tenant-specific approval workflows
- Audit trails for all AI decisions with tenant context

### Rate Limiting and Retries

The service includes built-in rate limiting and retry logic with tenant awareness:

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
