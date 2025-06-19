# Azure OpenAI Usage Guide

This document provides comprehensive guidance on integrating and using Azure OpenAI services within the EMMA platform, including configuration, implementation patterns, and best practices.

## Table of Contents

- [Configuration](#configuration)
  - [Environment Variables](#environment-variables)
  - [Configuration Options](#configuration-options)
  - [Multi-tenant Configuration](#multi-tenant-configuration)
- [Service Registration](#service-registration)
- [Basic Usage](#basic-usage)
- [Advanced Scenarios](#advanced-scenarios)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)
- [Monitoring and Logging](#monitoring-and-logging)

## Configuration

### Environment Variables

The Azure OpenAI service can be configured using the following environment variables:

| Variable | Description | Required |
|----------|-------------|:--------:|
| `AzureOpenAI__ApiKey` | API key for Azure OpenAI authentication | Yes* |
| `AzureOpenAI__Endpoint` | Endpoint URL for the Azure OpenAI service | Yes |
| `AzureOpenAI__DeploymentName` | Name of the model deployment | Yes |
| `AzureOpenAI__ApiVersion` | API version (e.g., `2024-02-15`) | No |
| `AzureOpenAI__MaxTokens` | Maximum tokens to generate | No |
| `AzureOpenAI__Temperature` | Controls response randomness (0-2) | No |
| `AZURE_TENANT_ID` | Azure AD tenant ID for managed identity | No |
| `AZURE_CLIENT_ID` | Client ID for managed identity | No |
| `AZURE_CLIENT_SECRET` | Client secret for managed identity | No |

> *Required when not using managed identity authentication

### Configuration Options

Configuration can be provided through `appsettings.json`, environment variables, or Azure Key Vault:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "gpt-4-turbo",
    "ApiVersion": "2024-02-15",
    "MaxTokens": 1000,
    "Temperature": 0.7,
    "OverrideSettings": {
      "RequiresApproval": true,
      "ApprovalWorkflow": "default",
      "AuditLogging": true
    }
  }
}
```

### Multi-tenant Configuration

For multi-tenant applications, use tenant-specific configuration:

```csharp
// In Program.cs
builder.Services.AddScoped<IOpenAIService>(sp => 
{
    var tenantContext = sp.GetRequiredService<ITenantContext>();
    var config = sp.GetRequiredService<IOptionsMonitor<OpenAIConfig>>()
        .Get(tenantContext.TenantId);
    return new OpenAIService(config);
});
```

## Service Registration

Register the Azure OpenAI client in `Program.cs`:

```csharp
// Basic registration
builder.Services.AddAzureOpenAIClient(builder.Configuration);

// With custom configuration
builder.Services.AddAzureOpenAIClient(options =>
{
    options.Endpoint = new Uri(builder.Configuration["AzureOpenAI:Endpoint"]);
    options.Credential = new DefaultAzureCredential(); // or new AzureKeyCredential(apiKey)
    options.DeploymentName = builder.Configuration["AzureOpenAI:DeploymentName"];
    options.MaxRetries = 3;
});
```

## Basic Usage

### Dependency Injection

Inject the `OpenAIClient` into your services:

```csharp
public class AIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<AIService> _logger;
    private readonly string _deploymentName;

    public AIService(
        OpenAIClient openAIClient,
        IOptions<AzureOpenAIConfig> config,
        ILogger<AIService> logger)
    {
        _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deploymentName = config?.Value?.DeploymentName ?? 
            throw new ArgumentNullException(nameof(config));
    }
}
```

### Basic Completion

```csharp
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
        MaxTokens = 1000,
        NucleusSamplingFactor = 0.95f,
        PresencePenalty = 0,
        FrequencyPenalty = 0,
        ResponseFormat = ChatCompletionsResponseFormat.Text
    };

    try
    {
        var response = await _openAIClient.GetChatCompletionsAsync(options);
        return response.Value.Choices[0].Message.Content;
    }
    catch (RequestFailedException ex) when (ex.Status == 429)
    {
        _logger.LogWarning("Rate limit exceeded. Consider implementing backoff.");
        throw;
    }
}
```

## Advanced Scenarios

### Streaming Responses

```csharp
public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt)
{
    var options = new ChatCompletionsOptions
    {
        DeploymentName = _deploymentName,
        Messages = { new ChatRequestUserMessage(prompt) },
        Temperature = 0.7f
    };

    var response = await _openAIClient.GetChatCompletionsStreamingAsync(options);
    await foreach (var message in response.EnumerateValues())
    {
        if (message.ContentUpdate != null)
        {
            yield return message.ContentUpdate;
        }
    }
}
```

### Function Calling

```csharp
var function = new ChatCompletionsFunctionToolDefinition
{
    Name = "get_weather",
    Description = "Get the current weather for a location",
    Parameters = BinaryData.FromObjectAsJson(new
    {
        type = "object",
        properties = new
        {
            location = new { type = "string", description = "The city and state, e.g. San Francisco, CA" },
            unit = new { type = "string", enum = new[] { "celsius", "fahrenheit" } }
        },
        required = new[] { "location" }
    })
};

var options = new ChatCompletionsOptions
{
    DeploymentName = _deploymentName,
    Messages = { new ChatRequestUserMessage("What's the weather like in Boston?") },
    Tools = { function }
};

var response = await _openAIClient.GetChatCompletionsAsync(options);
```

## Error Handling

### Common Exceptions

Handle specific exceptions appropriately:

```csharp
try
{
    // Your OpenAI API call
}
catch (RequestFailedException ex) when (ex.Status == 401)
{
    _logger.LogError("Authentication failed. Check your API key or managed identity configuration.");
    throw new UnauthorizedAccessException("Failed to authenticate with Azure OpenAI", ex);
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    _logger.LogError("Deployment not found: {DeploymentName}", _deploymentName);
    throw new InvalidOperationException("The specified deployment was not found", ex);
}
catch (RequestFailedException ex) when (ex.Status == 429)
{
    _logger.LogWarning("Rate limit exceeded. Consider implementing backoff.");
    throw new RateLimitExceededException("Too many requests to Azure OpenAI", ex);
}
catch (RequestFailedException ex) when (ex.Status >= 500)
{
    _logger.LogError(ex, "Azure OpenAI service error");
    throw new ServiceUnavailableException("Azure OpenAI service is currently unavailable", ex);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error calling Azure OpenAI API");
    throw;
}
```

### Retry Policies

Implement retry policies for transient failures:

```csharp
var retryOptions = new RetryOptions
{
    MaxRetries = 3,
    Mode = RetryMode.Exponential,
    Delay = TimeSpan.FromSeconds(1)
};

var pipeline = HttpPipelineBuilder.Build(new AzureOpenAIClientOptions()
{
    Retry = retryOptions
});
```

## Best Practices

### Security

- **Authentication**
  - Prefer managed identities over API keys in production
  - Use Azure Key Vault for secret management
  - Implement proper RBAC for Azure OpenAI resources
  - Rotate API keys and secrets regularly

### Performance

- **Optimization**
  - Reuse `OpenAIClient` instance (registered as singleton)
  - Implement response caching for deterministic requests
  - Use streaming for long-running completions
  - Batch similar requests when possible
  - Set appropriate timeouts

### Reliability

- **Resilience**
  - Implement retry policies with exponential backoff
  - Use circuit breakers for cascading failures
  - Implement fallback mechanisms for critical paths
  - Monitor and alert on error rates

### Cost Management

- **Optimization**
  - Monitor token usage and costs
  - Set up budget alerts in Azure
  - Use appropriate model sizes for the task
  - Implement usage quotas for different environments

## Troubleshooting

### Common Issues

| Issue | Possible Cause | Resolution |
|-------|---------------|------------|
| 401 Unauthorized | Invalid API key or token | Verify API key or managed identity configuration |
| 404 Not Found | Incorrect deployment name | Check deployment name in Azure portal |
| 429 Too Many Requests | Rate limit exceeded | Implement backoff or increase quota |
| 503 Service Unavailable | Service outage | Check Azure status page |
| Timeout | Network or service issue | Increase timeout or implement retry |
| Invalid Request | Malformed input | Validate input parameters |

### Diagnostic Logging

Enable detailed logging in `appsettings.json`:

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

## Monitoring and Logging

### Azure Monitor Integration

1. **Metrics**
   - Track token usage
   - Monitor latency and response times
   - Set up alerts for error rates

2. **Application Insights**
   - Log custom events and metrics
   - Track request/response payloads
   - Set up dashboards for key metrics

3. **Audit Logging**
   - Log all API calls with user context
   - Track model usage by tenant/user
   - Monitor for unusual activity patterns

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0.0 | 2024-03-15 | Added support for latest models and features |
| 1.1.0 | 2024-01-10 | Added multi-tenant support |
| 1.0.0 | 2023-11-01 | Initial release |

## Getting Help

For additional support:

- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Azure Support](https://azure.microsoft.com/en-us/support/)
- [EMMA Support Portal](https://support.emma.example.com)
