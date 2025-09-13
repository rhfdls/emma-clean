using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emma.Api.Services;
using Emma.Core.Config;
using Emma.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Emma.Api.IntegrationTests.Services
{
    public class EmmaAgentServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<ILogger<EmmaAgentService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IOptions<AzureOpenAIConfig>> _configOptionsMock;
        private readonly Mock<IChatCompletionsClient> _chatClientMock;
        private readonly EmmaAgentService _service;
        private readonly Guid _testDeploymentId = Guid.NewGuid();
        private readonly string _testDeployment = "test-deployment";

        public EmmaAgentServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _loggerMock = new Mock<ILogger<EmmaAgentService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            
            // Setup config options using test helper
            var config = BuildConfiguration().GetSection("AzureOpenAI").Get<AzureOpenAIConfig>();
            _configOptionsMock = new Mock<IOptions<AzureOpenAIConfig>>();
            _configOptionsMock.Setup(x => x.Value).Returns(config);
            
            // Setup OpenAIClient mock
            _chatClientMock = CreateOpenAIClientMock();
            
            _service = new EmmaAgentService(
                _loggerMock.Object,
                _httpContextAccessorMock.Object,
                _chatClientMock.Object,
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
                _chatClientMock.Object,
                _configOptionsMock.Object);

            // Assert
            Assert.NotNull(service);
        }


        [Fact]
        public async Task ProcessMessageAsync_WithValidInput_ReturnsSuccessResponse()
        {
            // Arrange
            var testMessage = "Hello, EMMA!";
            var expectedResponse = new EmmaAction { Action = EmmaActionType.None, Payload = "" };
            
            // Setup successful chat completion using test helper
            _chatClientMock.SetupSuccessfulChatCompletion(
                JsonSerializer.Serialize(expectedResponse));

            // Act
            var result = await _service.ProcessMessageAsync(testMessage);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(expectedResponse.Action, result.Action.Action);
            Assert.Equal(expectedResponse.Payload, result.Action.Payload);
            Assert.NotNull(result.CorrelationId);
            
            // Verify the request was made with the correct parameters
            _chatClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.IsAny<object>(),
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
            _chatClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessMessageAsync_WhenRateLimited_RetriesAndThenFails()
        {
            // Arrange
            var testMessage = "Rate limited message";
            
            // Setup to throw 429 (Too Many Requests) on all calls
            _chatClientMock.SetupFailedChatCompletion(
                statusCode: 429,
                errorMessage: "Rate limit exceeded");

            // Act
            var result = await _service.ProcessMessageAsync(testMessage);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("overloaded", result.Error);
            Assert.NotNull(result.CorrelationId);
            
            // Verify retry policy was applied (3 attempts total)
            _chatClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }
    }
}
