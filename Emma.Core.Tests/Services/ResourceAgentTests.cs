using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Services;
using Emma.Data.Models;
using Emma.Data.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Services
{
    public class ResourceAgentTests
    {
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<ITenantContextService> _mockTenantContext;
        private readonly Mock<ILogger<ResourceAgent>> _mockLogger;
        private readonly ResourceAgent _agent;

        public ResourceAgentTests()
        {
            _mockResourceService = new Mock<IResourceService>();
            _mockTenantContext = new Mock<ITenantContextService>();
            _mockLogger = new Mock<ILogger<ResourceAgent>>();
            
            _agent = new ResourceAgent(
                _mockResourceService.Object,
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

            // Act
            var capability = await _agent.GetCapabilityAsync();

            // Assert
            Assert.Equal("ResourceAgent", capability.AgentType);
            Assert.Equal("Resource Management Agent", capability.DisplayName);
            Assert.True(capability.IsAvailable);
            Assert.Contains("Resource Discovery", capability.SupportedTasks);
            Assert.Contains("Service Provider Recommendations", capability.SupportedTasks);
            Assert.Contains("RealEstate", capability.RequiredIndustries);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithResourceManagement_ReturnsSuccess()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ResourceManagement,
                Context = new Dictionary<string, object>
                {
                    ["action"] = "discover",
                    ["specialty"] = "Home Inspector"
                }
            };

            var mockResources = new List<Contact>
            {
                new Contact 
                { 
                    Id = Guid.NewGuid(), 
                    FirstName = "John", 
                    LastName = "Smith",
                    CompanyName = "Smith Inspections",
                    Rating = 4.5m,
                    IsPreferred = true
                }
            };

            _mockResourceService.Setup(x => x.DiscoverResourcesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<bool?>()))
                .ReturnsAsync(mockResources);

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("ResourceAgent", response.AgentId);
            Assert.Contains("Resources", response.Data.Keys);
        }

        [Fact]
        public async Task RecommendResourcesAsync_WithValidSpecialty_ReturnsRecommendations()
        {
            // Arrange
            var specialty = "Mortgage Lender";
            var serviceArea = "Downtown";
            var mockResources = new List<Contact>
            {
                new Contact 
                { 
                    Id = Guid.NewGuid(), 
                    FirstName = "Jane", 
                    LastName = "Doe",
                    CompanyName = "Doe Lending",
                    Email = "jane@doelending.com",
                    Phone = "555-1234",
                    Rating = 4.8m,
                    ReviewCount = 25,
                    IsPreferred = true
                }
            };

            _mockResourceService.Setup(x => x.GetTopPerformingResourcesAsync(specialty, It.IsAny<int>()))
                .ReturnsAsync(mockResources);
            _mockResourceService.Setup(x => x.DiscoverResourcesAsync(specialty, serviceArea, It.IsAny<decimal?>(), It.IsAny<bool?>()))
                .ReturnsAsync(mockResources);

            // Act
            var response = await _agent.RecommendResourcesAsync(specialty, serviceArea);

            // Assert
            Assert.True(response.Success);
            Assert.Contains("RecommendedResources", response.Data.Keys);
            var recommendations = response.Data["RecommendedResources"] as List<object>;
            Assert.NotNull(recommendations);
            Assert.Single(recommendations);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithServiceProviderRecommendation_RequiresSpecialty()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ServiceProviderRecommendation,
                Context = new Dictionary<string, object>() // Missing specialty
            };

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("Specialty is required", response.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithResourceAssignment_ValidatesRequiredFields()
        {
            // Arrange
            var request = new AgentRequest
            {
                Intent = AgentIntent.ResourceManagement,
                Context = new Dictionary<string, object>
                {
                    ["action"] = "assign",
                    ["clientContactId"] = Guid.NewGuid().ToString(),
                    // Missing other required fields
                }
            };

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("required for resource assignment", response.Message);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithValidAssignment_CreatesAssignment()
        {
            // Arrange
            var clientContactId = Guid.NewGuid();
            var serviceContactId = Guid.NewGuid();
            var assignedByAgentId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var purpose = "Home inspection";

            var request = new AgentRequest
            {
                Intent = AgentIntent.ResourceManagement,
                Context = new Dictionary<string, object>
                {
                    ["action"] = "assign",
                    ["clientContactId"] = clientContactId.ToString(),
                    ["serviceContactId"] = serviceContactId.ToString(),
                    ["assignedByAgentId"] = assignedByAgentId.ToString(),
                    ["organizationId"] = organizationId.ToString(),
                    ["purpose"] = purpose
                }
            };

            var mockAssignment = new ContactAssignment
            {
                Id = Guid.NewGuid(),
                ClientContactId = clientContactId,
                ServiceContactId = serviceContactId,
                Purpose = purpose,
                Status = ResourceAssignmentStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _mockResourceService.Setup(x => x.AssignResourceAsync(
                clientContactId, serviceContactId, assignedByAgentId, organizationId, purpose,
                It.IsAny<ResourceAssignmentStatus>(), It.IsAny<Priority>()))
                .ReturnsAsync(mockAssignment);

            // Act
            var response = await _agent.ProcessRequestAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Contains("AssignmentId", response.Data.Keys);
            Assert.Equal(mockAssignment.Id, response.Data["AssignmentId"]);
        }

        [Fact]
        public async Task ProcessRequestAsync_WithUnsupportedIntent_ReturnsError()
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
            Assert.Contains("cannot handle intent", response.Message);
        }
    }
}
