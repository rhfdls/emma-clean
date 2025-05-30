using Emma.Api.IntegrationTests.Fixtures;
using Emma.Core.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Emma.Api.IntegrationTests.Services
{
    [Collection("AzureOpenAICollection")]
    public class EmmaAgentServiceIntegrationTests : IClassFixture<AzureOpenAIFixture>
    {
        private readonly AzureOpenAIFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly EmmaAgentService _service;

        public EmmaAgentServiceIntegrationTests(AzureOpenAIFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            var services = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole()); // Removed AddXUnit(output) and debug level for compatibility
            
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<EmmaAgentService>>();
            
            _service = new EmmaAgentService(
                logger,
                null, // IHttpContextAccessor not needed for this test
                _fixture.Client,
                new OptionsWrapper<AzureOpenAIConfig>(_fixture.Config));
        }

        [SkippableFact]
        public async Task ProcessMessageAsync_WithRealService_ReturnsValidResponse()
        {
            // Skip if the fixture indicates Azure OpenAI is not available
            Skip.If(!_fixture.IsAvailable, "Azure OpenAI is not configured for integration testing");

            // Arrange
            var testMessage = "Hello, EMMA! Can you help me find a house?";

            // Act
            var result = await _service.ProcessMessageAsync(testMessage);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success, $"Request failed: {result.Error}");
            Assert.NotNull(result.Action);
            Assert.NotNull(result.CorrelationId);
            
            _output.WriteLine($"Action: {result.Action.Action}");
            _output.WriteLine($"Payload: {result.Action.Payload}");
            // _output.WriteLine($"Raw Response: {result.RawResponse}"); // RawResponse property does not exist
            
            // Verify the response is a valid action
            Assert.Contains(result.Action.Action, new[] { EmmaActionType.SendEmail, EmmaActionType.ScheduleFollowup, EmmaActionType.None });
        }
    }
    
    [CollectionDefinition("AzureOpenAICollection")]
    public class AzureOpenAICollection : ICollectionFixture<AzureOpenAIFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
