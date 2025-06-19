using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Emma.Core.Interfaces.Services;

namespace Emma.Core.Tests.Services
{
    public class ContextIntelligenceAgentTests
    {
        private readonly Mock<IContextIntelligenceService> _mockContextService;
        private readonly Mock<ITenantContextService> _mockTenantContext;
        private readonly Mock<IPromptProvider> _mockPromptProvider;
        private readonly Mock<IAIFoundryService> _mockAiFoundryService;
        private readonly Mock<ILogger<ContextIntelligenceAgent>> _mockLogger;
        private readonly ContextIntelligenceAgent _agent;

        public ContextIntelligenceAgentTests()
        {
            _mockContextService = new Mock<IContextIntelligenceService>();
            _mockTenantContext = new Mock<ITenantContextService>();
            _mockPromptProvider = new Mock<IPromptProvider>();
            _mockAiFoundryService = new Mock<IAIFoundryService>();
            _mockLogger = new Mock<ILogger<ContextIntelligenceAgent>>();
            
            _agent = new ContextIntelligenceAgent(
                _mockContextService.Object,
                _mockTenantContext.Object,
                _mockPromptProvider.Object,
                _mockAiFoundryService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetCapabilityAsync_ReturnsValidCapability()
        {
            // Arrange
            var industryProfile = new Mock<Emma.Core.Industry.IIndustryProfile>();
            industryProfile.Setup(x => x.IndustryCode).Returns("RealEstate");
            _mockTenantContext.Setup(x => x.GetIndustryProfileAsync())
                .ReturnsAsync(industryProfile.Object);

            // Act
            var capability = await _agent.GetCapabilityAsync();

            // Assert
            Assert.Equal("ContextIntelligenceAgent", capability.AgentType);
            Assert.Equal("Context Intelligence Agent", capability.DisplayName);
            Assert.True(capability.IsAvailable);
            Assert.Contains("Interaction Analysis", capability.SupportedTasks);
            Assert.Contains("Sentiment Analysis", capability.SupportedTasks);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithValidContactId_ReturnsSuccess()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var request = new AgentRequest
            {
                Intent = AgentIntent.InteractionAnalysis,
                Context = new Dictionary<string, object>
                {
                    ["contactId"] = contactId.ToString(),
                    ["analysisType"] = "interaction"
                }
            };

            _mockContextService.Setup(x => x.AnalyzeInteractionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { ["result"] = "analysis complete" });

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("ContextIntelligenceAgent", response.AgentId);
            Assert.Contains("ContactId", response.Data.Keys);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithoutContactId_ReturnsError()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.InteractionAnalysis,
                Context = new Dictionary<string, object>()
            };

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("ContactId is required", response.Message);
        }

        [Fact]
        public async Task AnalyzeContextAsync_WithValidParameters_ReturnsAnalysis()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            
            _mockContextService.Setup(x => x.GetContactContextAsync(contactId, It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, object> { ["context"] = "test context" });
            _mockContextService.Setup(x => x.GenerateRecommendedActionsAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<string> { "action1", "action2" });
            _mockContextService.Setup(x => x.PredictCloseProbabilityAsync(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
                .ReturnsAsync(0.75);

            // Act
            var response = await _agent.AnalyzeContextAsync(contactId, organizationId, "comprehensive");

            // Assert
            Assert.True(response.Success);
            Assert.Contains("Results", response.Data.Keys);
            Assert.Equal(contactId.ToString(), response.Data["ContactId"]);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnsupportedIntent_ReturnsError()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ResourceManagement,
                Context = new Dictionary<string, object>
                {
                    ["contactId"] = Guid.NewGuid().ToString()
                }
            };

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("cannot handle intent", response.Message);
        }
    }
}
