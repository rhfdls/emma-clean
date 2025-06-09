using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Services
{
    public class AgentOrchestratorTests
    {
        private readonly Mock<INbaAgent> _mockNbaAgent;
        private readonly Mock<IContextIntelligenceAgent> _mockContextAgent;
        private readonly Mock<IIntentClassificationAgent> _mockIntentAgent;
        private readonly Mock<IResourceAgent> _mockResourceAgent;
        private readonly Mock<IAiFoundryService> _mockAiFoundry;
        private readonly Mock<ILogger<AgentOrchestrator>> _mockLogger;
        private readonly AgentOrchestrator _orchestrator;

        public AgentOrchestratorTests()
        {
            _mockNbaAgent = new Mock<INbaAgent>();
            _mockContextAgent = new Mock<IContextIntelligenceAgent>();
            _mockIntentAgent = new Mock<IIntentClassificationAgent>();
            _mockResourceAgent = new Mock<IResourceAgent>();
            _mockAiFoundry = new Mock<IAiFoundryService>();
            _mockLogger = new Mock<ILogger<AgentOrchestrator>>();

            _orchestrator = new AgentOrchestrator(
                _mockNbaAgent.Object,
                _mockContextAgent.Object,
                _mockIntentAgent.Object,
                _mockResourceAgent.Object,
                _mockAiFoundry.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAvailableAgentsAsync_ReturnsAllAgentCapabilities()
        {
            // Arrange
            var nbaCapability = new AgentCapability
            {
                AgentType = "NbaAgent",
                DisplayName = "NBA Agent",
                IsAvailable = true,
                SupportedTasks = new List<string> { "Next Best Actions" }
            };

            var contextCapability = new AgentCapability
            {
                AgentType = "ContextIntelligenceAgent",
                DisplayName = "Context Intelligence Agent",
                IsAvailable = true,
                SupportedTasks = new List<string> { "Interaction Analysis" }
            };

            var intentCapability = new AgentCapability
            {
                AgentType = "IntentClassificationAgent",
                DisplayName = "Intent Classification Agent",
                IsAvailable = true,
                SupportedTasks = new List<string> { "Intent Classification" }
            };

            var resourceCapability = new AgentCapability
            {
                AgentType = "ResourceAgent",
                DisplayName = "Resource Management Agent",
                IsAvailable = true,
                SupportedTasks = new List<string> { "Resource Discovery" }
            };

            _mockNbaAgent.Setup(x => x.GetCapabilityAsync()).ReturnsAsync(nbaCapability);
            _mockContextAgent.Setup(x => x.GetCapabilityAsync()).ReturnsAsync(contextCapability);
            _mockIntentAgent.Setup(x => x.GetCapabilityAsync()).ReturnsAsync(intentCapability);
            _mockResourceAgent.Setup(x => x.GetCapabilityAsync()).ReturnsAsync(resourceCapability);

            // Act
            var capabilities = await _orchestrator.GetAvailableAgentsAsync();

            // Assert
            Assert.Equal(4, capabilities.Count);
            Assert.Contains(capabilities, c => c.AgentType == "NbaAgent");
            Assert.Contains(capabilities, c => c.AgentType == "ContextIntelligenceAgent");
            Assert.Contains(capabilities, c => c.AgentType == "IntentClassificationAgent");
            Assert.Contains(capabilities, c => c.AgentType == "ResourceAgent");
        }

        [Fact]
        public async Task ProcessRequestAsync_WithNbaKeywords_RoutesToNbaAgent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.DataAnalysis,
                OriginalUserInput = "What should I do next with this client?",
                Context = new Dictionary<string, object>()
            };

            var expectedResponse = new AgentResponse
            {
                Success = true,
                AgentId = "NbaAgent",
                Data = new Dictionary<string, object> { ["recommendations"] = "test" }
            };

            _mockNbaAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("NbaAgent", response.AgentId);
            _mockNbaAgent.Verify(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithInteractionAnalysisIntent_RoutesToContextAgent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.InteractionAnalysis,
                OriginalUserInput = "Analyze this client interaction",
                Context = new Dictionary<string, object>
                {
                    ["contactId"] = Guid.NewGuid().ToString()
                }
            };

            var expectedResponse = new AgentResponse
            {
                Success = true,
                AgentId = "ContextIntelligenceAgent",
                Data = new Dictionary<string, object> { ["analysis"] = "test" }
            };

            _mockContextAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("ContextIntelligenceAgent", response.AgentId);
            _mockContextAgent.Verify(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithIntentClassificationIntent_RoutesToIntentAgent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.IntentClassification,
                OriginalUserInput = "I need help with something",
                Context = new Dictionary<string, object>()
            };

            var expectedResponse = new AgentResponse
            {
                Success = true,
                AgentId = "IntentClassificationAgent",
                Data = new Dictionary<string, object> { ["classifiedIntent"] = "ResourceManagement" }
            };

            _mockIntentAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("IntentClassificationAgent", response.AgentId);
            _mockIntentAgent.Verify(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithResourceManagementIntent_RoutesToResourceAgent()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ResourceManagement,
                OriginalUserInput = "Find me a home inspector",
                Context = new Dictionary<string, object>
                {
                    ["specialty"] = "Home Inspector"
                }
            };

            var expectedResponse = new AgentResponse
            {
                Success = true,
                AgentId = "ResourceAgent",
                Data = new Dictionary<string, object> { ["resources"] = "test" }
            };

            _mockResourceAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("ResourceAgent", response.AgentId);
            _mockResourceAgent.Verify(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnknownIntent_AttemptsIntentClassification()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = (AgentIntent)999, // Unknown intent
                OriginalUserInput = "Help me find a mortgage lender",
                Context = new Dictionary<string, object>()
            };

            var classificationResponse = new AgentResponse
            {
                Success = true,
                AgentId = "IntentClassificationAgent",
                Data = new Dictionary<string, object> 
                { 
                    ["ClassifiedIntent"] = "ResourceManagement" 
                }
            };

            var resourceResponse = new AgentResponse
            {
                Success = true,
                AgentId = "ResourceAgent",
                Data = new Dictionary<string, object> { ["resources"] = "test" }
            };

            _mockIntentAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ReturnsAsync(classificationResponse);
            _mockResourceAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ReturnsAsync(resourceResponse);

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("ResourceAgent", response.AgentId);
            _mockIntentAgent.Verify(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()), Times.Once);
            _mockResourceAgent.Verify(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnknownIntentAndNoUserInput_ReturnsFallbackResponse()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = (AgentIntent)999, // Unknown intent
                OriginalUserInput = "", // No user input
                Context = new Dictionary<string, object>()
            };

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("Unable to process intent", response.Message);
            Assert.Contains("SuggestedActions", response.Data.Keys);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithException_ReturnsErrorResponse()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.InteractionAnalysis,
                OriginalUserInput = "test",
                Context = new Dictionary<string, object>()
            };

            _mockContextAgent.Setup(x => x.ProcessRequestAsync(It.IsAny<AgentRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var response = await _orchestrator.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("Request processing failed", response.Message);
            Assert.Equal("AgentOrchestrator", response.AgentId);
        }
    }
}
