using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Emma.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Emma.Api.IntegrationTests.TestHelpers
{
    /// <summary>
    /// TestHelper provides utilities for integration tests, including configuration loading,
    /// OpenAI client mocking, and CosmosDB connectivity checks.
    /// </summary>
    public static class TestHelper
    {
        public static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false)
                .AddEnvironmentVariables()
                .Build();
        }

        public static IServiceCollection ConfigureTestServices(this IServiceCollection services, ITestOutputHelper output = null)
        {
            var configuration = BuildConfiguration();
            
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });
            
            // Add test configuration
            services.Configure<AzureOpenAIConfig>(configuration.GetSection("AzureOpenAI"));
            
            // Add any other test services here
            
            return services;
        }

        public static Mock<OpenAIClient> CreateOpenAIClientMock()
        {
            return new Mock<OpenAIClient>();
        }

        public static void SetupSuccessfulChatCompletion(
            this Mock<OpenAIClient> mockClient,
            string responseContent = "{ \"action\": \"none\", \"payload\": \"\" }")
        {
            var chatCompletionsObj = new
            {
                id = $"chatcmpl-{Guid.NewGuid()}",
                @object = "chat.completion",
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                model = "gpt-4.1",
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new
                        {
                            role = "assistant",
                            content = responseContent
                        },
                        finish_reason = "stop"
                    }
                },
                usage = new
                {
                    prompt_tokens = 10,
                    completion_tokens = 15,
                    total_tokens = 25
                }
            };

            var chatCompletionsJson = JsonSerializer.Serialize(chatCompletionsObj);
            var chatCompletions = JsonSerializer.Deserialize<ChatCompletions>(chatCompletionsJson);

            mockClient
                .Setup(x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(chatCompletions, new Mock<Response>().Object));
        }

        public static void SetupFailedChatCompletion(
            this Mock<OpenAIClient> mockClient,
            int statusCode = 500,
            string errorMessage = "Internal Server Error")
        {
            mockClient
                .Setup(x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(statusCode, errorMessage));
        }
        /// <summary>
        /// Verifies that CosmosDB is reachable and queries can be performed.
        /// Returns true if successful; otherwise, false.
        /// </summary>
        /// <param name="cosmosRepo">The CosmosAgentRepository instance to test.</param>
        /// <returns>True if CosmosDB is reachable and responds to a simple query; otherwise, false.</returns>
        public static async Task<bool> CanConnectToCosmosDb(object cosmosRepo)
        {
            try
            {
                // Use reflection to call QueryItemsAsync<object>("SELECT TOP 1 * FROM c")
                var method = cosmosRepo.GetType().GetMethod("QueryItemsAsync");
                if (method == null) return false;
                var task = (Task)method.MakeGenericMethod(typeof(object)).Invoke(cosmosRepo, new object[] { "SELECT TOP 1 * FROM c" });
                await task.ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
