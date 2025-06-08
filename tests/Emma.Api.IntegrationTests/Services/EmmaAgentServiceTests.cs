namespace Emma.Api.IntegrationTests.Services
{
    public class EmmaAgentServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<ILogger<EmmaAgentService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IOptions<AzureOpenAIConfig>> _configOptionsMock;
        private readonly Mock<OpenAIClient> _openAIClientMock;
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
            _openAIClientMock = CreateOpenAIClientMock();
            
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
            var expectedResponse = new EmmaAction { Action = EmmaActionType.None, Payload = "" };
            
            // Setup successful chat completion using test helper
            _openAIClientMock.SetupSuccessfulChatCompletion(
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
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.Is<ChatCompletionsOptions>(o => 
                        o.DeploymentName == _testDeployment &&
                        o.Messages.Count == 2 &&
                        o.Messages[0].Role == ChatRole.System &&
                        o.Messages[1].Role == ChatRole.User
                    ),
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
            _openAIClientMock.SetupFailedChatCompletion(
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
            _openAIClientMock.Verify(
                x => x.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }
    }
}
