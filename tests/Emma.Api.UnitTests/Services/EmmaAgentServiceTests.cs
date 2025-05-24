using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Emma.Api.Services;
using Emma.Core.Config;
using Emma.Core.Dtos;
using Emma.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Emma.Api.UnitTests.Services
{
    public class EmmaAgentServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<ILogger<EmmaAgentService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IOptions<AzureOpenAIConfig>> _configOptionsMock;
        private readonly Mock<OpenAIClient> _openAIClientMock;
        private readonly EmmaAgentService _service;
        private readonly string _testDeployment = "test-deployment";

        public EmmaAgentServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _loggerMock = new Mock<ILogger<EmmaAgentService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            
            // Setup config options
            var config = new AzureOpenAIConfig
            {
                ApiKey = "test-api-key",
                Endpoint = "https://test.openai.azure.com/",
                DeploymentName = _testDeployment,
                ApiVersion = "2023-05-15"
            };
            _configOptionsMock = new Mock<IOptions<AzureOpenAIConfig>>();
            _configOptionsMock.Setup(x => x.Value).Returns(config);
            
            // Setup OpenAIClient mock
            _openAIClientMock = new Mock<OpenAIClient>();
            
            _service = new EmmaAgentService(
                _loggerMock.Object,
                _httpContextAccessorMock.Object,
                _openAIClientMock.Object,
                _configOptionsMock.Object);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        [Fact]
        public void Constructor_WithValidConfig_InitializesSuccessfully()
        {
            // Arrange & Act
            var service = new EmmaAgentService(
                _loggerMock.Object,
                _httpContextAccessorMock.Object,
                _openAIClientMock.Object,
                _configOptionsMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public async Task ProcessMessageAsync_WithValidInput_ReturnsSuccessResponse()
        {
            // Arrange
            var testMessage = "Hello, EMMA!";
            var expectedResponse = new EmmaAction { Action = "none", Payload = "" };
            
            var chatCompletions = ChatCompletions.DeserializeChatCompletions(
                JsonDocument.Parse($"""
                {{
                    "id": "chatcmpl-123",
                    "object": "chat.completion",
                    "created": 1677652288,
                    "model": "gpt-4.1",
                    "choices": [{{
                        "index": 0,
                        "message": {{
                            "role": "assistant",
                            "content": "{{ \"action\": \"none\", \"payload\": \"\" }}"
                        }},
                        "finish_reason": "stop"
                    }}],
                    "usage": {{
                        "prompt_tokens": 9,
                        "completion_tokens": 12,
                        "total_tokens": 21
                    }}
                }}
                """).RootElement);

            _openAIClientMock
                .Setup(x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(chatCompletions, new Mock<Response>().Object));

            // Act
            var result = await _service.ProcessMessageAsync(testMessage);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(expectedResponse.Action, result.Action.Action);
            Assert.Equal(expectedResponse.Payload, result.Action.Payload);
            Assert.NotNull(result.CorrelationId);
            
            // Verify the request was made with the correct parameters
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.Is<ChatCompletionsOptions>(o => 
                        o.DeploymentName == _testDeployment &&
                        o.Messages.Count == 2 &&
                        o.Messages[0].Role == ChatRole.System &&
                        o.Messages[1].Role == ChatRole.User &&
                        o.Messages[1].Content == testMessage),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_WithEmptyMessage_ReturnsErrorResponse()
        {
            // Arrange
            var emptyMessage = "";

            // Act
            var result = await _service.ProcessMessageAsync(emptyMessage);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Equal("Message cannot be empty", result.Error);
            Assert.NotNull(result.CorrelationId);
            
            // Verify no API call was made
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessMessageAsync_WhenRateLimited_RetriesAndThenFails()
        {
            // Arrange
            var testMessage = "Rate limited message";
            
            // Setup to throw 429 (Too Many Requests) on all calls
            _openAIClientMock
                .SetupSequence(x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(429, "Rate limit exceeded"))
                .ThrowsAsync(new RequestFailedException(429, "Rate limit exceeded"))
                .ThrowsAsync(new RequestFailedException(429, "Rate limit exceeded"));

            // Act
            var result = await _service.ProcessMessageAsync(testMessage);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("overloaded", result.Error);
            Assert.NotNull(result.CorrelationId);
            
            // Verify retry policy was applied (3 attempts total)
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }

        [Fact]
        public async Task ProcessMessageAsync_WithInvalidJsonResponse_ReturnsErrorResponse()
        {
            // Arrange
            var testMessage = "Test message with invalid JSON response";
            
            var chatCompletions = ChatCompletions.DeserializeChatCompletions(
                JsonDocument.Parse("""
                {
                    "id": "chatcmpl-123",
                    "object": "chat.completion",
                    "created": 1677652288,
                    "model": "gpt-4.1",
                    "choices": [{
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": "This is not valid JSON"
                        },
                        "finish_reason": "stop"
                    }]
                }
                """).RootElement);

            _openAIClientMock
                .Setup(x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(chatCompletions, new Mock<Response>().Object));

            // Act
            var result = await _service.ProcessMessageAsync(testMessage);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Invalid response format", result.Error);
            Assert.NotNull(result.CorrelationId);
        }
    }
}
