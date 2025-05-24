using System;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Emma.Api.IntegrationTests.Fixtures
{
    /// <summary>
    /// Test fixture that provides a real Azure OpenAI client for integration testing.
    /// This should only be used for tests that require actual API calls to Azure OpenAI.
    /// </summary>
    public class AzureOpenAIFixture : IAsyncLifetime
    {
        public OpenAIClient Client { get; private set; }
        public AzureOpenAIConfig Config { get; private set; }
        public bool IsAvailable { get; private set; }

        public AzureOpenAIFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json")
                .AddEnvironmentVariables()
                .Build();

            Config = configuration.GetSection("AzureOpenAI").Get<AzureOpenAIConfig>();
            
            // Only create the client if we have the required configuration
            if (!string.IsNullOrEmpty(Config?.ApiKey) && 
                !string.IsNullOrEmpty(Config?.Endpoint) &&
                !string.IsNullOrEmpty(Config?.DeploymentName))
            {
                Client = new OpenAIClient(
                    new Uri(Config.Endpoint),
                    new Azure.AzureKeyCredential(Config.ApiKey));
                IsAvailable = true;
            }
        }

        public async Task InitializeAsync()
        {
            if (!IsAvailable) return;

            try
            {
                // Test the connection
                var options = new ChatCompletionsOptions
                {
                    DeploymentName = Config.DeploymentName,
                    Messages = 
                    {
                        new ChatRequestSystemMessage("You are a helpful assistant."),
                        new ChatRequestUserMessage("Hello")
                    },
                    MaxTokens = 5
                };

                // This will throw if the deployment doesn't exist or credentials are invalid
                await Client.GetChatCompletionsAsync(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Azure OpenAI connection test failed: {ex.Message}");
                IsAvailable = false;
            }
        }

        public Task DisposeAsync()
        {
            // No unmanaged resources to dispose
            return Task.CompletedTask;
        }
    }
}
