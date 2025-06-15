using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Data.Models;
using Emma.Data.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emma.Core.Industry;
using Emma.Core.Exceptions;
using System.Collections.Concurrent;

namespace Emma.Core.Tests.Services
{
    public class ResourceAgentTestsV2
    {
        private readonly Mock<IContactService> _mockContactService;
        private readonly Mock<ITenantContextService> _mockTenantContext;
        private readonly Mock<IAIFoundryService> _mockAIFoundryService;
        private readonly Mock<IPromptProvider> _mockPromptProvider;
        private readonly Mock<ILogger<ResourceAgent>> _mockLogger;
        private readonly ResourceAgent _agent;
        private readonly Mock<IIndustryProfile> _mockIndustryProfile;

        public ResourceAgentTestsV2()
        {
            _mockContactService = new Mock<IContactService>();
            _mockTenantContext = new Mock<ITenantContextService>();
            _mockAIFoundryService = new Mock<IAIFoundryService>();
            _mockPromptProvider = new Mock<IPromptProvider>();
            _mockLogger = new Mock<ILogger<ResourceAgent>>();
            _mockIndustryProfile = new Mock<IIndustryProfile>();

            _agent = new ResourceAgent(
                _mockContactService.Object,
                _mockTenantContext.Object,
                _mockAIFoundryService.Object,
                _mockPromptProvider.Object,
                _mockLogger.Object);

            // Setup default industry profile
            _mockIndustryProfile.Setup(p => p.IndustryCode).Returns("RealEstate");
            _mockIndustryProfile.Setup(p => p.DisplayName).Returns("Real Estate");
            _mockIndustryProfile.Setup(p => p.ResourceTypes).Returns(new List<string> { "Agent", "Lender", "Inspector" });
            _mockIndustryProfile.Setup(p => p.DefaultResourceCategories).Returns(new List<string> { "Buying", "Selling", "Renting" });
            _mockTenantContext.Setup(t => t.GetIndustryProfileAsync()).ReturnsAsync(_mockIndustryProfile.Object);
        }

        #region ProcessRequestAsync Tests

        [Fact]
        public async Task ProcessRequestAsync_NullRequest_ReturnsError()
        {
            // Act
            var result = await _agent.ProcessRequestAsync(null!);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Request is null", result.ErrorMessage);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ProcessRequestAsync: request is null")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessRequestAsync_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ServiceProviderRecommendation,
                Context = new Dictionary<string, object>
                {
                    ["Parameters"] = new Dictionary<string, object>
                    {
                        ["specialty"] = "Real Estate Agent",
                        ["serviceArea"] = "Seattle"
                    },
                    ["OrganizationId"] = Guid.NewGuid().ToString()
                }
            };

            var mockContacts = new List<Contact>
            {
                CreateMockContact("John", "Doe", "John's Realty", "john@example.com", "555-1234",
                    new[] { "Real Estate Agent" }, new[] { "Seattle" }, 4.8m, 25, true)
            };

            _mockContactService.Setup(x => x.GetContactsByRelationshipStateAsync(
                It.IsAny<Guid>(), 
                It.Is<IEnumerable<RelationshipState>>(states => 
                    states.Contains(RelationshipState.ServiceProvider) && 
                    states.Contains(RelationshipState.Agent))))
                .ReturnsAsync(mockContacts);

            // Act
            var result = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.ContainsKey("ResourceRecommendations"));
            Assert.True(result.Data.ContainsKey("SearchCriteria"));
            Assert.True(result.Data.ContainsKey("TotalFound"));
            Assert.True(result.Data.ContainsKey("AnalysisMethod"));
        }

        #endregion

        #region GetCapabilityAsync Tests

        [Fact]
        public async Task GetCapabilityAsync_ReturnsValidCapability()
        {
            // Act
            var capability = await _agent.GetCapabilityAsync();

            // Assert
            Assert.NotNull(capability);
            Assert.Equal("ResourceAgent", capability.AgentId);
            Assert.Equal("Resource Recommendation Agent", capability.Name);
            Assert.Contains("recommend_resources", capability.SupportedTasks);
            Assert.Contains("find_specialists", capability.SupportedTasks);
            Assert.True(capability.Configuration.ContainsKey("RequiredParameters"));
            Assert.True(capability.Configuration.ContainsKey("OptionalParameters"));
        }

        #endregion

        #region RecommendResourcesAsync Tests

        [Fact]
        public async Task RecommendResourcesAsync_NoServiceProviders_ReturnsEmptyResult()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var criteria = new Dictionary<string, object> { ["specialty"] = "Real Estate Agent" };
            
            _mockContactService.Setup(x => x.GetContactsByRelationshipStateAsync(
                It.IsAny<Guid>(), 
                It.IsAny<IEnumerable<RelationshipState>>()))
                .ReturnsAsync(new List<Contact>());

            // Act
            var result = await _agent.RecommendResourcesAsync(organizationId, criteria);

            // Assert
            Assert.True(result.Success);
            var recommendations = result.Data["ResourceRecommendations"] as IEnumerable<object>;
            Assert.Empty(recommendations);
            Assert.Equal("No service providers found matching the specified criteria", result.Message);
        }

        [Fact]
        public async Task RecommendResourcesAsync_WithServiceProviders_ReturnsRecommendations()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var criteria = new Dictionary<string, object> { ["specialty"] = "Real Estate Agent" };
            
            var mockContacts = new List<Contact>
            {
                CreateMockContact("John", "Doe", "John's Realty", "john@example.com", "555-1234",
                    new[] { "Real Estate Agent" }, new[] { "Seattle" }, 4.8m, 25, true)
            };

            _mockContactService.Setup(x => x.GetContactsByRelationshipStateAsync(
                It.IsAny<Guid>(), 
                It.IsAny<IEnumerable<RelationshipState>>()))
                .ReturnsAsync(mockContacts);

            // Mock AI response
            var aiResponse = new List<ResourceAgent.ResourceRecommendationResult>
            {
                new ResourceAgent.ResourceRecommendationResult
                {
                    ContactId = mockContacts[0].Id,
                    RecommendationReason = "Top-rated agent in Seattle area",
                    MatchScore = 0.95m,
                    Strengths = new List<string> { "Top performer", "Excellent reviews" },
                    Considerations = new List<string> { "Limited availability next week" }
                }
            };

            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()))
                .ReturnsAsync(JsonSerializer.Serialize(aiResponse));

            // Act
            var result = await _agent.RecommendResourcesAsync(organizationId, criteria);

            // Assert
            Assert.True(result.Success);
            var recommendations = result.Data["ResourceRecommendations"] as IEnumerable<object>;
            Assert.Single(recommendations);
            Assert.Equal("AI-Powered", result.Data["AnalysisMethod"]);
        }

        [Fact]
        public async Task RecommendResourcesAsync_WhenAIFails_FallsBackToRuleBased()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var criteria = new Dictionary<string, object> { ["specialty"] = "Real Estate Agent" };
            
            var mockContacts = new List<Contact>
            {
                CreateMockContact("John", "Doe", "John's Realty", "john@example.com", "555-1234",
                    new[] { "Real Estate Agent" }, new[] { "Seattle" }, 4.8m, 25, true)
            };

            _mockContactService.Setup(x => x.GetContactsByRelationshipStateAsync(
                It.IsAny<Guid>(), 
                It.IsAny<IEnumerable<RelationshipState>>()))
                .ReturnsAsync(mockContacts);

            // Make AI fail
            _mockAIFoundryService.Setup(x => x.ProcessAgentRequestAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("AI service unavailable"));

            // Act
            var result = await _agent.RecommendResourcesAsync(organizationId, criteria);

            // Assert
            Assert.True(result.Success);
            var recommendations = result.Data["ResourceRecommendations"] as IEnumerable<object>;
            Assert.Single(recommendations);
            Assert.Equal("Rule-Based Fallback", result.Data["AnalysisMethod"]);
        }

        #endregion

        #region FindServiceProviderContactsAsync Tests

        [Fact]
        public async Task FindServiceProviderContactsAsync_AppliesFiltersCorrectly()
        {
            // Arrange
            var organizationId = Guid.NewGuid();
            var criteria = new Dictionary<string, object>
            {
                ["specialty"] = "Real Estate Agent",
                ["serviceArea"] = "Seattle",
                ["minRating"] = 4.0m,
                ["preferredOnly"] = true
            };

            var mockContacts = new List<Contact>
            {
                CreateMockContact("John", "Doe", "John's Realty", "john@example.com", "555-1234",
                    new[] { "Real Estate Agent" }, new[] { "Seattle" }, 4.8m, 25, true),
                CreateMockContact("Jane", "Smith", "Smith Properties", "jane@example.com", "555-5678",
                    new[] { "Real Estate Agent" }, new[] { "Bellevue" }, 3.8m, 15, false)
            };

            _mockContactService.Setup(x => x.GetContactsByRelationshipStateAsync(
                organizationId, 
                It.Is<IEnumerable<RelationshipState>>(states => 
                    states.Contains(RelationshipState.ServiceProvider) && 
                    states.Contains(RelationshipState.Agent))))
                .ReturnsAsync(mockContacts);

            // Act
            var result = await _agent.FindServiceProviderContactsAsync(organizationId, criteria, 10);

            // Assert
            Assert.Single(result);
            Assert.Equal("John Doe", $"{result[0].FirstName} {result[0].LastName}");
        }

        #endregion

        #region ExtractResourceCriteria Tests

        [Fact]
        public void ExtractResourceCriteria_FromMessageContent_ExtractsCorrectly()
        {
            // Arrange
            var request = new AgentRequest
            {
                Context = new Dictionary<string, object>
                {
                    ["Message"] = "I need a mortgage lender for my client in Seattle. Preferably someone with good reviews.",
                    ["Parameters"] = new Dictionary<string, object>
                    {
                        ["clientLocation"] = "Seattle"
                    }
                }
            };

            // Act
            var criteria = _agent.ExtractResourceCriteria(request);

            // Assert
            Assert.Equal("Mortgage Lending", criteria["specialty"]);
            Assert.Equal("Seattle", criteria["clientLocation"]);
        }

        #endregion

        #region Helper Methods

        private Contact CreateMockContact(
            string firstName, string lastName, string companyName, 
            string email, string phone, string[] specialties, string[] serviceAreas,
            decimal rating, int reviewCount, bool isPreferred)
        {
            return new Contact
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                CompanyName = companyName,
                Emails = new List<ContactEmail> { new ContactEmail { Address = email, Type = "work" } },
                Phones = new List<ContactPhone> { new ContactPhone { Number = phone, Type = "mobile" } },
                Specialties = specialties.ToList(),
                ServiceAreas = serviceAreas.ToList(),
                Rating = rating,
                ReviewCount = reviewCount,
                IsPreferred = isPreferred,
                RelationshipState = RelationshipState.ServiceProvider
            };
        }

        #endregion
    }
}
