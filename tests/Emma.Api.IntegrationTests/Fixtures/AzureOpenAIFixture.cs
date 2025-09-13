using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Emma.Api.IntegrationTests.Configuration;
using CoreAzureOpenAIConfig = Emma.Core.Config.AzureOpenAIConfig;

namespace Emma.Api.IntegrationTests.Fixtures
{
    /// <summary>
    /// Test fixture that provides a real Azure OpenAI client for integration testing.
    /// This should only be used for tests that require actual API calls to Azure OpenAI.
    /// </summary>
    public class AzureOpenAIFixture : IAsyncLifetime
    {
        public CoreAzureOpenAIConfig Config { get; private set; } = new();
        public bool IsAvailable { get; private set; }

        public AzureOpenAIFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var parsed = configuration.GetSection("AzureOpenAI").Get<CoreAzureOpenAIConfig>();
            if (parsed != null)
            {
                Config = parsed;
            }
            
            // Only mark available if we have the required configuration
            IsAvailable = !string.IsNullOrEmpty(Config?.ApiKey)
                           && !string.IsNullOrEmpty(Config?.Endpoint)
                           && !string.IsNullOrEmpty(Config?.ChatDeploymentName);
        }

        public async Task InitializeAsync()
        {
            // No-op: fixture no longer calls external services in tests.
            await Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            // No unmanaged resources to dispose
            return Task.CompletedTask;
        }
    }
}
