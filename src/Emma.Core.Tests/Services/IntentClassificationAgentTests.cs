using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Services
{
    public class IntentClassificationAgentTests
    {
        private readonly Mock<IIntentClassificationService> _mockIntentService;
        private readonly Mock<ITenantContextService> _mockTenantContext;
        private readonly Mock<ILogger<IntentClassificationAgent>> _mockLogger;
        private readonly IntentClassificationAgent _agent;

        public IntentClassificationAgentTests()
        {
            _mockIntentService = new Mock<IIntentClassificationService>();
            _mockTenantContext = new Mock<ITenantContextService>();
            _mockLogger = new Mock<ILogger<IntentClassificationAgent>>();
            
            _agent = new IntentClassificationAgent(
                _mockIntentService.Object,
                _mockTenantContext.Object,
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
            _mockIntentService.Setup(x => x.GetConfidenceThreshold()).Returns(0.7);

            // Act
            var capability = await _agent.GetCapabilityAsync();

            // Assert
            Assert.Equal("IntentClassificationAgent", capability.AgentType);
            Assert.Equal("Intent Classification Agent", capability.DisplayName);
            Assert.True(capability.IsAvailable);
            Assert.Contains("Intent Classification", capability.SupportedTasks);
            Assert.Contains("Entity Extraction", capability.SupportedTasks);
            Assert.Equal(0.7, capability.Capabilities["MinConfidenceThreshold"]);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithValidInput_ReturnsClassification()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.IntentClassification,
                OriginalUserInput = "I need help finding a mortgage lender",
                Context = new Dictionary<string, object>
                {
                    ["requestType"] = "intent_classification"
                }
            };

            var classificationResult = new IntentClassificationResult
            {
                Intent = AgentIntent.ResourceManagement,
                Confidence = 0.85,
                Reasoning = "User is looking for a service provider",
                ExtractedEntities = new Dictionary<string, object> { ["serviceType"] = "mortgage lender" },
                Urgency = UrgencyLevel.Medium
            };

            _mockIntentService.Setup(x => x.ClassifyIntentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
                .ReturnsAsync(classificationResult);
            _mockIntentService.Setup(x => x.GetConfidenceThreshold()).Returns(0.7);

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("IntentClassificationAgent", response.AgentId);
            Assert.Contains("ClassifiedIntent", response.Data.Keys);
            Assert.Equal("ResourceManagement", response.Data["ClassifiedIntent"]);
            Assert.Equal(0.85, response.Data["Confidence"]);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithoutUserInput_ReturnsError()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.IntentClassification,
                OriginalUserInput = "",
                Context = new Dictionary<string, object>()
            };

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("User input is required", response.Message);
        }

        [Fact]
        public async Task ClassifyIntentAsync_WithIndustryContext_IncludesIndustryInfo()
        {
            // Arrange
            var userInput = "Schedule a property showing";
            var industryProfile = new Mock<Emma.Core.Industry.IIndustryProfile>();
            industryProfile.Setup(x => x.IndustryCode).Returns("RealEstate");
            _mockTenantContext.Setup(x => x.GetIndustryProfileAsync())
                .ReturnsAsync(industryProfile.Object);

            var classificationResult = new IntentClassificationResult
            {
                Intent = AgentIntent.SchedulingAndTasks,
                Confidence = 0.9,
                Reasoning = "User wants to schedule a meeting",
                Urgency = UrgencyLevel.High
            };

            _mockIntentService.Setup(x => x.ClassifyIntentAsync(
                It.IsAny<string>(), 
                It.Is<Dictionary<string, object>>(d => d.ContainsKey("industry") && d["industry"].ToString() == "RealEstate"), 
                It.IsAny<string>()))
                .ReturnsAsync(classificationResult);
            _mockIntentService.Setup(x => x.GetConfidenceThreshold()).Returns(0.7);

            // Act
            var response = await _agent.ClassifyIntentAsync(userInput);

            // Assert
            Assert.True(response.Success);
            Assert.Contains("SchedulingAndTasks", response.Data["ClassifiedIntent"].ToString());
            Assert.True((bool)response.Data["IsHighConfidence"]);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnsupportedIntent_ReturnsError()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ResourceManagement,
                OriginalUserInput = "test input",
                Context = new Dictionary<string, object>()
            };

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("cannot handle intent", response.Message);
        }
    }
}
