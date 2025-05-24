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
                
                if (output != null)
                {
                    builder.AddXUnit(output);
                }
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
            var chatCompletions = ChatCompletions.DeserializeChatCompletions(
                JsonDocument.Parse($"""
                {{
                    "id": "chatcmpl-{Guid.NewGuid()}",
                    "object": "chat.completion",
                    "created": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
                    "model": "gpt-4.1",
                    "choices": [{{
                        "index": 0,
                        "message": {{
                            "role": "assistant",
                            "content": {JsonSerializer.Serialize(responseContent)}
                        }},
                        "finish_reason": "stop"
                    }}],
                    "usage": {{
                        "prompt_tokens": 10,
                        "completion_tokens": 15,
                        "total_tokens": 25
                    }}
                }}
                """).RootElement);

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
    }
}
