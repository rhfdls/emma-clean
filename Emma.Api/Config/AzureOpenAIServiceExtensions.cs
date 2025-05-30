using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Core.Pipeline;
using Emma.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Config;

public static class AzureOpenAIServiceExtensions
{
    /// <summary>
    /// Adds Azure OpenAI services to the service collection with proper configuration and validation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration containing Azure OpenAI settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureOpenAI(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Bind and validate AzureOpenAIConfig
        services.AddOptions<AzureOpenAIConfig>()
            .Bind(configuration.GetSection("AzureOpenAI"))
            .ValidateDataAnnotations()
            .Validate(config => 
            {
                if (string.IsNullOrWhiteSpace(config.Endpoint))
                    return false;
                if (string.IsNullOrWhiteSpace(config.ApiKey))
                    return false;
                if (string.IsNullOrWhiteSpace(config.DeploymentName))
                    return false;
                
                return Uri.TryCreate(config.Endpoint.TrimEnd('/'), UriKind.Absolute, out _);
            }, "Azure OpenAI configuration is invalid. Ensure Endpoint, ApiKey, and DeploymentName are properly set.");

        // Register OpenAIClient as a singleton with retry policy
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OpenAIClient>>();
            var config = provider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
            
            try
            {
                var endpoint = new Uri(config.Endpoint.TrimEnd('/'));
                
                // Configure client options with retry policy
                var clientOptions = new OpenAIClientOptions
                {
                    Retry = {
                        MaxRetries = 3,
                        Mode = RetryMode.Exponential,
                        MaxDelay = TimeSpan.FromSeconds(10),
                        NetworkTimeout = TimeSpan.FromSeconds(30)
                    },
                    Diagnostics = 
                    {
                        IsTelemetryEnabled = true,
                        ApplicationId = "Emma.Api",
                        LoggedHeaderNames = { "x-ms-request-id", "x-ms-client-request-id" },
                        LoggedQueryParameters = { "api-version" }
                    }
                };

                logger.LogInformation("Initializing Azure OpenAI client for endpoint: {Endpoint}", endpoint);
                
                return new OpenAIClient(
                    endpoint: endpoint,
                    keyCredential: new AzureKeyCredential(config.ApiKey),
                    clientOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize Azure OpenAI client");
                throw new InvalidOperationException("Failed to initialize Azure OpenAI client. Check your configuration.", ex);
            }
        });

        // Register a typed HTTP client with retry policy for direct HTTP calls if needed
        services.AddHttpClient<IAzureOpenAIService, AzureOpenAIService>("AzureOpenAI", (provider, client) =>
        {
            var config = provider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
            var baseUrl = config.Endpoint.TrimEnd('/') + "/openai/";
            
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("api-key", config.ApiKey);
            client.DefaultRequestHeaders.Add("User-Agent", "Emma.API");
        })
        .AddPolicyHandler((provider, _) => 
            Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(x => (int)x.StatusCode >= 500 || x.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, delay, retryAttempt, context) =>
                    {
                        var logger = provider.GetRequiredService<ILogger<AzureOpenAIService>>();
                        var requestUri = outcome.Result?.RequestMessage?.RequestUri?.ToString() ?? "unknown";
                        logger.LogWarning(
                            "Delaying for {delay}ms, then making retry {retryAttempt} of {retryCount} for {requestUri}",
                            delay.TotalMilliseconds,
                            retryAttempt,
                            3,
                            requestUri);
                    }))
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Add health check for Azure OpenAI
        services.AddHealthChecks()
            .AddCheck<AzureOpenAIHealthCheck>("azure-openai");

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}

/// <summary>
/// Health check for Azure OpenAI service
/// </summary>
public class AzureOpenAIHealthCheck : IHealthCheck
{
    private readonly OpenAIClient _openAIClient;
    private readonly IOptions<AzureOpenAIConfig> _config;
    private readonly ILogger<AzureOpenAIHealthCheck> _logger;

    public AzureOpenAIHealthCheck(
        OpenAIClient openAIClient,
        IOptions<AzureOpenAIConfig> config,
        ILogger<AzureOpenAIHealthCheck> logger)
    {
        _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get the list of deployments as a health check
            await _openAIClient.GetChatCompletionsAsync(
                new ChatCompletionsOptions
                {
                    DeploymentName = _config.Value.DeploymentName,
                    Messages = { new ChatRequestSystemMessage("Health check") },
                    MaxTokens = 1
                }, 
                cancellationToken);

            return HealthCheckResult.Healthy("Azure OpenAI service is available");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Deployment not found - configuration issue
            _logger.LogError(ex, "Deployment {DeploymentName} not found in Azure OpenAI", _config.Value.DeploymentName);
            return HealthCheckResult.Unhealthy("Deployment not found in Azure OpenAI");
        }
        catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
        {
            // Authentication/authorization issue
            _logger.LogError(ex, "Authentication failed for Azure OpenAI");
            return HealthCheckResult.Unhealthy("Authentication failed for Azure OpenAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Azure OpenAI");
            return HealthCheckResult.Unhealthy("Azure OpenAI service is unavailable", ex);
        }
    }
}

/// <summary>
/// Interface for Azure OpenAI service
/// </summary>
public interface IAzureOpenAIService
{
    // Add methods for direct HTTP calls if needed
}

/// <summary>
/// Implementation of IAzureOpenAIService
/// </summary>
public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureOpenAIService> _logger;

    public AzureOpenAIService(
        IHttpClientFactory httpClientFactory,
        ILogger<AzureOpenAIService> logger)
    {
        _httpClient = httpClientFactory?.CreateClient("AzureOpenAI") ?? 
            throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    // Implement methods for direct HTTP calls if needed
}

// AzureOpenAIConfig is now defined in Emma.Core.Config namespace
// This ensures consistent configuration across the application
