using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using Emma.Api.Services;
using Emma.Core.Config;
using Emma.Core.Dtos;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;

namespace Emma.Core.Tests.Services
{
    public class EmmaAgentServiceTests : IDisposable
    {
        private readonly MockRepository _mockRepository;
        private readonly Mock<ILogger<EmmaAgentService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<HttpContext> _httpContextMock;
        private readonly Mock<HttpRequest> _httpRequestMock;
        private readonly Mock<IHeaderDictionary> _headerDictionaryMock;
        private readonly Mock<OpenAIClient> _openAIClientMock;
        private readonly AzureOpenAIConfig _config;
        private readonly IOptions<AzureOpenAIConfig> _options;
        private readonly IEmmaAgentService _service;
        private const string TestCorrelationId = "test-correlation-id";
        public EmmaAgentServiceTests()
        {
            _mockRepository = new MockRepository(MockBehavior.Strict);
            
            // Setup mocks
            _loggerMock = _mockRepository.Create<ILogger<EmmaAgentService>>();
            _httpContextAccessorMock = _mockRepository.Create<IHttpContextAccessor>();
            _httpContextMock = _mockRepository.Create<HttpContext>();
            _httpRequestMock = _mockRepository.Create<HttpRequest>();
            _headerDictionaryMock = _mockRepository.Create<IHeaderDictionary>();
            _openAIClientMock = _mockRepository.Create<OpenAIClient>();
            
            // Setup logger mock
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ));
            
            // Setup HTTP context mock
            _httpRequestMock.SetupGet(r => r.Headers).Returns(_headerDictionaryMock.Object);
            _httpContextMock.SetupGet(c => c.Request).Returns(_httpRequestMock.Object);
            _httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(_httpContextMock.Object);
            
            // Setup correlation ID header
            _headerDictionaryMock
                .Setup(h => h.TryGetValue("X-Correlation-ID", out It.Ref<StringValues>.IsAny))
                .Returns((string key, out StringValues value) =>
                {
                    value = new StringValues(TestCorrelationId);
                    return true;
                });
            
            // Setup Azure OpenAI config
            _config = new AzureOpenAIConfig
            {
                Endpoint = "https://test.openai.azure.com/",
                ApiKey = "test-key",
                DeploymentName = "test-deployment"
            };

            var optionsMock = _mockRepository.Create<IOptions<AzureOpenAIConfig>>();
            optionsMock.Setup(x => x.Value).Returns(_config);
            _options = optionsMock.Object;

            // Create the service with all required dependencies
            _service = new EmmaAgentService(
                _loggerMock.Object,
                _httpContextAccessorMock.Object,
                _openAIClientMock.Object,
                _options);
        }
        
        public void Dispose()
        {
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async Task ProcessMessageAsync_WithNullMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.ProcessMessageAsync(null!));
            
            Assert.Equal("message", exception.ParamName);
            
            // Verify logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Value cannot be null")),
                    It.Is<ArgumentNullException>(ex => ex.ParamName == "message"),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // Verify no calls to OpenAI client
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(It.IsAny<ChatCompletionsOptions>(), default),
                Times.Never);
        }

        [Fact]
        public async Task ProcessMessageAsync_WithEmptyMessage_ReturnsErrorResponse()
        {
            // Act
            var result = await _service.ProcessMessageAsync("");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Message cannot be empty", result.Error);
            Assert.NotNull(result.CorrelationId);
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message cannot be empty")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_WithValidMessage_ReturnsExpectedAction()
        {
            // Arrange
            var message = "Test message";
            var expectedAction = new EmmaAction
            {
                Action = EmmaActionType.SendEmail,
                Payload = "Test payload"
            };
            
            var responseContent = new 
            {
                action = "sendemail",
                payload = expectedAction.Payload
            };
            
            var responseJson = JsonSerializer.Serialize(responseContent);
            
            // Create a mock response with the expected content
            var response = CreateMockResponse(responseJson);
            
            // Setup the mock to verify the request
            _openAIClientMock
                .Setup(x => x.GetChatCompletionsAsync(
                    It.Is<ChatCompletionsOptions>(o => 
                        o.Messages != null &&
                        o.Messages.Count == 2 && 
                        o.Messages[0].Role == "system" &&
                        o.Messages[1].Role == "user" &&
                        o.Messages[1].Content == message &&
                        o.Temperature == 0.3f &&
                        o.MaxTokens == 500 &&
                        o.ResponseFormat == ChatCompletionsResponseFormat.JsonObject),
                    default))
                .ReturnsAsync(response);

            // Act
            var result = await _service.ProcessMessageAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(expectedAction.Action, result.Action?.Action);
            Assert.Equal(expectedAction.Payload, result.Action?.Payload);
            
            // Verify the OpenAI client was called with the expected parameters
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.Is<ChatCompletionsOptions>(o => 
                        o.Messages != null &&
                        o.Messages.Count == 2 && 
                        o.Messages[0].Role == "system" &&
                        o.Messages[1].Role == "user" &&
                        o.Messages[1].Content == message),
                    default),
                Times.Once);

            // Verify logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing message")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessMessageAsync_WhenOpenAIFails_ReturnsErrorResponse()
        {
            // Arrange
            var errorMessage = "Test API error";
            
            _openAIClientMock
                .Setup(x => x.GetChatCompletionsAsync(
                    It.Is<ChatCompletionsOptions>(o => o != null), 
                    default))
                .ThrowsAsync(new RequestFailedException(errorMessage));

            // Act
            var result = await _service.ProcessMessageAsync("Test message");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.CorrelationId);
            Assert.NotNull(result.Error);
            Assert.Contains(errorMessage, result.Error);
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing message")),
                    It.IsAny<RequestFailedException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task ProcessMessageAsync_WithMalformedJsonResponse_ReturnsErrorResponse()
        {
            // Arrange
            var response = CreateMockResponse(new { choices = new[] { new { message = new { content = "{ invalid json }" } } } });
            
            _openAIClientMock
                .Setup(x => x.GetChatCompletionsAsync(
                    It.Is<ChatCompletionsOptions>(o => o != null), 
                    default))
                .ReturnsAsync(response);

            // Act
            var result = await _service.ProcessMessageAsync("Test message");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.CorrelationId);
            Assert.NotNull(result.Error);
            Assert.Contains("Failed to parse AI response", result.Error);
            
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to parse AI response")),
                    It.IsAny<JsonException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        private static Response<ChatCompletions> CreateMockResponse(string content)
        {
            // 1. Create mock for ChatResponseMessage
            var messageMock = new Mock<ChatResponseMessage>();
            messageMock.SetupGet(m => m.Content).Returns(content);
            messageMock.SetupGet(m => m.Role).Returns("assistant");

            // 2. Create mock for ChatChoice
            var choiceMock = new Mock<ChatChoice>();
            choiceMock.SetupGet(c => c.Message).Returns(messageMock.Object);
            choiceMock.SetupGet(c => c.Index).Returns(0);
            choiceMock.SetupGet(c => c.FinishReason).Returns("stop");

            // 3. Create mock for IReadOnlyList<ChatChoice>
            var choicesList = new List<ChatChoice> { choiceMock.Object };
            var choicesMock = new Mock<IReadOnlyList<ChatChoice>>();
            choicesMock.Setup(c => c.Count).Returns(1);
            choicesMock.Setup(c => c[0]).Returns(choiceMock.Object);
            choicesMock.Setup(c => c.GetEnumerator()).Returns(choicesList.GetEnumerator());
            choicesMock.As<IEnumerable<ChatChoice>>()
                      .Setup(c => c.GetEnumerator())
                      .Returns(choicesList.GetEnumerator());

            // 4. Create mock for CompletionsUsage
            var usageMock = new Mock<CompletionsUsage>();
            usageMock.SetupGet(u => u.CompletionTokens).Returns(1);
            usageMock.SetupGet(u => u.PromptTokens).Returns(1);
            usageMock.SetupGet(u => u.TotalTokens).Returns(2);

            // 5. Create mock for ChatCompletions
            var chatCompletionsMock = new Mock<ChatCompletions>();
            chatCompletionsMock.SetupGet(c => c.Id).Returns("test-completion-id");
            chatCompletionsMock.SetupGet(c => c.Created).Returns(DateTimeOffset.UtcNow);
            chatCompletionsMock.SetupGet(c => c.Choices).Returns(choicesMock.Object);
            chatCompletionsMock.SetupGet(c => c.Usage).Returns(usageMock.Object);

            // 6. Create mock for Response
            var responseMock = new Mock<Response>();
            responseMock.SetupGet(r => r.Status).Returns(200);

            return Response.FromValue(chatCompletionsMock.Object, responseMock.Object);
        }
    }
}
