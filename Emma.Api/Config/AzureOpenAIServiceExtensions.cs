using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Emma.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Emma.Api.Config;

public static class AzureOpenAIServiceExtensions
{
    public static IServiceCollection AddAzureOpenAI(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind AzureOpenAIConfig from configuration and validate required settings
        services.AddOptions<AzureOpenAIConfig>()
            .Bind(configuration.GetSection("AzureOpenAI"))
            .ValidateDataAnnotations()
            .Validate(config => 
                !string.IsNullOrEmpty(config.Endpoint) && 
                !string.IsNullOrEmpty(config.ApiKey),
                "Azure OpenAI configuration is missing required values (Endpoint and ApiKey must be set)");

        // Register OpenAIClient as a singleton
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
            
            // Create the Azure OpenAI client with the provided endpoint and API key
            var clientOptions = new OpenAIClientOptions()
            {
                Diagnostics = 
                {
                    IsTelemetryEnabled = true,
                    ApplicationId = "Emma.Api"
                }
            };

            return new OpenAIClient(
                endpoint: new Uri(config.Endpoint.TrimEnd('/')), // Ensure no trailing slash
                keyCredential: new AzureKeyCredential(config.ApiKey),
                clientOptions);
        });

        // Register a named client for specific deployments if needed
        services.AddHttpClient("AzureOpenAI", (provider, client) =>
        {
            var config = provider.GetRequiredService<IOptions<AzureOpenAIConfig>>().Value;
            client.BaseAddress = new Uri(config.Endpoint.TrimEnd('/') + "/openai/");
            client.DefaultRequestHeaders.Add("api-key", config.ApiKey);
        });

        return services;
    }
}

// AzureOpenAIConfig is now defined in Emma.Core.Config namespace
// This ensures consistent configuration across the application
