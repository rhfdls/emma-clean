using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Emma.Core.Models;
using Emma.Core.Industry;
using Emma.Core.Interfaces;
using Emma.Core.Compliance;
using Emma.Core.Services;
using Emma.Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Emma.Core.Enums;

namespace Emma.Core.Tests.Services
{
    public class NbaAgentTestsV2
    {
        private readonly Mock<INbaContextService> _mockNbaContextService;
        private readonly Mock<IAIFoundryService> _mockAIFoundryService;
        private readonly Mock<IPromptProvider> _mockPromptProvider;
        private readonly Mock<IActionRelevanceValidator> _mockActionRelevanceValidator;
        private readonly Mock<IAgentActionValidator> _mockAgentActionValidator;
        private readonly Mock<IAgentComplianceChecker> _mockComplianceChecker;
        private readonly Mock<ILogger<NbaAgent>> _mockLogger;
        private readonly Mock<ITenantContextService> _mockTenantContextService;
        private readonly NbaAgent _nbaAgent;
        private readonly Mock<IIndustryProfile> _mockIndustryProfile;
        private readonly Guid _testContactId = Guid.NewGuid();
        private readonly Guid _testOrgId = Guid.NewGuid();
        private readonly Guid _testAgentId = Guid.NewGuid();
        private const string TestTraceId = "test-trace-123";

        public NbaAgentTestsV2()
        {
            _mockNbaContextService = new Mock<INbaContextService>();
            _mockAIFoundryService = new Mock<IAIFoundryService>();
            _mockPromptProvider = new Mock<IPromptProvider>();
            _mockActionRelevanceValidator = new Mock<IActionRelevanceValidator>();
            _mockAgentActionValidator = new Mock<IAgentActionValidator>();
            _mockComplianceChecker = new Mock<IAgentComplianceChecker>();
            _mockLogger = new Mock<ILogger<NbaAgent>>();
            _mockTenantContextService = new Mock<ITenantContextService>();
            _mockIndustryProfile = new Mock<IIndustryProfile>();

            // Setup default industry profile
            _mockIndustryProfile.Setup(p => p.IndustryCode).Returns("RealEstate");
            _mockIndustryProfile.Setup(p => p.DisplayName).Returns("Real Estate");
            _mockIndustryProfile.Setup(p => p.NbaActionTypes).Returns(new List<string> 
                { "follow_up", "schedule_showing", "send_contract", "request_documents" });
            
            _mockTenantContextService.Setup(t => t.GetIndustryProfileAsync())
                .ReturnsAsync(_mockIndustryProfile.Object);

            _nbaAgent = new NbaAgent(
                _mockNbaContextService.Object,
                _mockAIFoundryService.Object,
                _mockPromptProvider.Object,
                _mockActionRelevanceValidator.Object,
                _mockAgentActionValidator.Object,
                _mockComplianceChecker.Object,
                _mockLogger.Object,
                _mockTenantContextService.Object);

            // Setup default mock behaviors
            SetupDefaultMocks();
        }


        #region RecommendNextBestActionsAsync Tests

        [Fact]
        public async Task RecommendNextBestActionsAsync_ValidInput_ReturnsRecommendations()
        {
            // Arrange
            var nbaContext = CreateTestNbaContext();
            var contactState = CreateTestContactState();
            
            _mockNbaContextService.Setup(x => x.GetNbaContextAsync(
                    _testContactId, _testOrgId, _testAgentId, It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(nbaContext);
                
            _mockNbaContextService.Setup(x => x.GetContactStateAsync(_testContactId, _testOrgId))
                .ReturnsAsync(contactState);

            // Setup AI response with JSON format
            var aiResponse = new
            {
                Recommendations = new[]
                {
                    new
                    {
                        ActionType = "follow_up",
                        Description = "Follow up about the property inquiry",
                        Priority = 1,
                        ConfidenceScore = 0.95,
                        Reasoning = "Client showed interest in properties yesterday",
                        ExpectedOutcome = "Schedule a showing",
                        Timing = "Within 24 hours"
                    }
                },
                Summary = "Recommendation based on recent property inquiry"
            };

            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(JsonSerializer.Serialize(aiResponse));

            // Act
            var result = await _nbaAgent.RecommendNextBestActionsAsync(
                _testContactId, _testOrgId, _testAgentId, 3, TestTraceId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("recommendations"));
            var recommendations = result.Data["recommendations"] as IEnumerable<object>;
            Assert.Single(recommendations);
            
            // Verify validation was called
            _mockAgentActionValidator.Verify(x => x.ValidateAgentActionsAsync(
                It.IsAny<IEnumerable<IAgentAction>>(), 
                It.IsAny<AgentActionValidationContext>(), 
                It.IsAny<Dictionary<string, object>>(), 
                TestTraceId), Times.Once);
        }

        [Fact]
        public async Task RecommendNextBestActionsAsync_WhenAIFails_FallsBackToRuleBased()
        {
            // Arrange - Make AI call fail
            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), It.IsAny<string>(), null))
                .ThrowsAsync(new Exception("AI service unavailable"));

            // Act
            var result = await _nbaAgent.RecommendNextBestActionsAsync(
                _testContactId, _testOrgId, _testAgentId, 3, TestTraceId);

            // Assert - Should still succeed with fallback recommendations
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("recommendations"));
            var recommendations = result.Data["recommendations"] as IEnumerable<object>;
            Assert.NotEmpty(recommendations);
            
            // Verify fallback was used
            _mockLogger.Verify(
                x => x.LogWarning(
                    It.Is<string>(s => s.Contains("falling back to rule-based")),
                    It.IsAny<object[]>()),
                Times.Once);
        }

        [Fact]
        public async Task RecommendNextBestActionsAsync_WithUserOverrides_AppliesOverrides()
        {
            // Arrange
            var userOverrides = new Dictionary<string, object>
            {
                ["excluded_actions"] = "follow_up,send_email",
                ["min_confidence"] = 0.8
            };

            var nbaContext = CreateTestNbaContext();
            var contactState = CreateTestContactState();
            
            _mockNbaContextService.Setup(x => x.GetNbaContextAsync(
                    _testContactId, _testOrgId, _testAgentId, It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(nbaContext);
                
            _mockNbaContextService.Setup(x => x.GetContactStateAsync(_testContactId, _testOrgId))
                .ReturnsAsync(contactState);

            // Setup AI response
            var aiResponse = new List<NbaRecommendation>
            {
                new NbaRecommendation
                {
                    ActionType = "follow_up",
                    Description = "Follow up call",
                    Priority = 1,
                    ConfidenceScore = 0.9,
                    Timing = "ASAP"
                },
                new NbaRecommendation
                {
                    ActionType = "send_document",
                    Description = "Send contract",
                    Priority = 2,
                    ConfidenceScore = 0.85,
                    Timing = "Today"
                }
            };

            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(JsonSerializer.Serialize(new { Recommendations = aiResponse }));

            // Setup validator to filter out follow_up action based on overrides
            _mockAgentActionValidator.Setup(x => x.ValidateAgentActionsAsync(
                    It.IsAny<IEnumerable<IAgentAction>>(), 
                    It.IsAny<AgentActionValidationContext>(), 
                    It.IsAny<Dictionary<string, object>>(), 
                    TestTraceId))
                .ReturnsAsync(aiResponse.Where(r => r.ActionType != "follow_up").ToList());

            // Act
            var result = await _nbaAgent.RecommendNextBestActionsAsync(
                _testContactId, _testOrgId, _testAgentId, 3, TestTraceId, userOverrides);

            // Assert
            Assert.True(result.Success);
            var recommendations = (result.Data["recommendations"] as IEnumerable<object>)?.ToList();
            Assert.Single(recommendations);
            Assert.DoesNotContain(recommendations, r => 
                r.GetType().GetProperty("ActionType")?.GetValue(r)?.ToString() == "follow_up");
        }

        [Fact]
        public async Task RecommendNextBestActionsAsync_RateLimitExceeded_ReturnsError()
        {
            // Arrange - Create a new instance to test rate limiting
            var rateLimitedAgent = new NbaAgent(
                _mockNbaContextService.Object,
                _mockAIFoundryService.Object,
                _mockPromptProvider.Object,
                _mockActionRelevanceValidator.Object,
                _mockAgentActionValidator.Object,
                _mockComplianceChecker.Object,
                _mockLogger.Object,
                _mockTenantContextService.Object);

            // Act - Call the method multiple times to exceed rate limit
            for (int i = 0; i < 15; i++)
            {
                var result = await rateLimitedAgent.RecommendNextBestActionsAsync(
                    _testContactId, _testOrgId, _testAgentId, 3, $"trace-{i}");
                
                // After 10 calls, we should hit the rate limit
                if (i >= 10)
                {
                    Assert.False(result.Success);
                    Assert.Contains("Rate limit exceeded", result.Message);
                }
            }
        }

        #endregion

        #region ProcessRequestAsync Tests

        [Fact]
        public async Task ProcessRequestAsync_WithNbaIntent_ProcessesCorrectly()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.DataAnalysis,
                Context = new Dictionary<string, object>
                {
                    ["requestType"] = "nba_recommendation",
                    ["contactId"] = _testContactId.ToString(),
                    ["organizationId"] = _testOrgId.ToString(),
                    ["agentId"] = _testAgentId.ToString(),
                    ["userId"] = "test-user"
                }
            };

            // Setup mock to return a successful recommendation
            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(JsonSerializer.Serialize(new 
                { 
                    Recommendations = new[] 
                    { 
                        new { ActionType = "follow_up", Description = "Test", Priority = 1 } 
                    } 
                }));

            // Act
            var result = await _nbaAgent.ProcessRequestAsync(request, TestTraceId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("recommendations"));
        }

        [Fact]
        public async Task ProcessRequestAsync_WithMissingRequiredParams_ReturnsError()
        {
            // Arrange - Missing contactId
            var request = new AgentRequest
            {
                Intent = AgentIntent.DataAnalysis,
                Context = new Dictionary<string, object>
                {
                    ["requestType"] = "nba_recommendation",
                    ["organizationId"] = _testOrgId.ToString(),
                    ["agentId"] = _testAgentId.ToString()
                    // Missing contactId
                }
            };

            // Act
            var result = await _nbaAgent.ProcessRequestAsync(request, TestTraceId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("UserId is required", result.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnsupportedIntent_ReturnsError()
        {
            // Arrange - Use an unsupported intent
            var request = new AgentRequest
            {
                Intent = AgentIntent.Unknown,
                Context = new Dictionary<string, object>
                {
                    ["userId"] = "test-user"
                }
            };

            // Act
            var result = await _nbaAgent.ProcessRequestAsync(request, TestTraceId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cannot handle intent", result.Message);
        }

        #endregion

        #region GetCapabilityAsync Tests

        [Fact]
        public async Task GetCapabilityAsync_ReturnsValidCapability()
        {
            // Act
            var capability = await _nbaAgent.GetCapabilityAsync();

            // Assert
            Assert.NotNull(capability);
            Assert.Equal("NbaAgent", capability.AgentType);
            Assert.Equal("Next Best Action Advisor", capability.DisplayName);
            Assert.Contains("recommend", capability.SupportedTasks);
            Assert.Contains("next_best_action", capability.SupportedTasks);
            Assert.True(capability.IsAvailable);
            Assert.Equal("RealEstate", capability.RequiredIndustries.First());
        }

        #endregion

        #region Helper Methods

        private void SetupDefaultMocks()
        {
            // Default NbaContext
            var nbaContext = CreateTestNbaContext();
            _mockNbaContextService.Setup(x => x.GetNbaContextAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), true))
                .ReturnsAsync(nbaContext);

            // Default ContactState
            var contactState = CreateTestContactState();
            _mockNbaContextService.Setup(x => x.GetContactStateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .ReturnsAsync(contactState);

            // Default prompt provider
            _mockPromptProvider.Setup(x => x.GetSystemPromptAsync(It.IsAny<string>(), It.IsAny<IIndustryProfile>()))
                .ReturnsAsync("System prompt for testing");
            _mockPromptProvider.Setup(x => x.BuildPromptAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .ReturnsAsync("User prompt for testing");

            // Default AI response
            var defaultResponse = new List<NbaRecommendation>
            {
                new NbaRecommendation
                {
                    ActionType = "follow_up",
                    Description = "Default test recommendation",
                    Priority = 1,
                    ConfidenceScore = 0.9
                }
            };

            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), It.IsAny<string>(), null))
                .ReturnsAsync(JsonSerializer.Serialize(new { Recommendations = defaultResponse }));

            // Default validator - pass through all actions
            _mockAgentActionValidator.Setup(x => x.ValidateAgentActionsAsync(
                    It.IsAny<IEnumerable<IAgentAction>>(), 
                    It.IsAny<AgentActionValidationContext>(), 
                    It.IsAny<Dictionary<string, object>>(), 
                    It.IsAny<string>()))
                .ReturnsAsync((IEnumerable<IAgentAction> actions, AgentActionValidationContext _, Dictionary<string, object> _, string _) => 
                    actions.ToList());
        }

        private NbaContext CreateTestNbaContext()
        {
            return new NbaContext
            {
                ContactId = _testContactId,
                OrganizationId = _testOrgId,
                RecentInteractions = new List<Interaction>
                {
                    new Interaction
                    {
                        Id = Guid.NewGuid(),
                        ContactId = _testContactId,
                        Type = InteractionType.Call,
                        Direction = InteractionDirection.Inbound,
                        Timestamp = DateTime.UtcNow.AddHours(-1),
                        Content = "Customer inquired about property listing"
                    }
                },
                ActiveContactAssignments = new List<ContactAssignmentSummary>(),
                RollingSummary = new ContactSummary
                {
                    SummaryText = "Customer is interested in 3-bedroom homes in Seattle"
                },
                Metadata = new Dictionary<string, object>
                {
                    ["last_contacted"] = DateTime.UtcNow.AddDays(-2).ToString("o")
                }
            };
        }

        private ContactState CreateTestContactState()
        {
            return new ContactState
            {
                ContactId = _testContactId,
                OrganizationId = _testOrgId,
                LastContacted = DateTime.UtcNow.AddDays(-2),
                NextFollowUp = DateTime.UtcNow.AddDays(1),
                Status = "Active",
                Tags = new List<string> { "potential_buyer", "seattle_area" },
                CustomFields = new Dictionary<string, object>
                {
                    ["budget"] = "500000-750000",
                    ["preferred_location"] = "Seattle"
                }
            };
        }

        #endregion
    }
}
