using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;
using Emma.Core.Interfaces;
using Emma.Core.Services;
using Emma.Core.Models;
using Emma.Api.Services;
using Emma.Models.Models;

namespace Emma.Core.Tests;

public class NbaAgentTests
{
    private readonly Mock<INbaContextService> _mockNbaContextService;
    private readonly Mock<ITenantContextService> _mockTenantContextService;
    private readonly Mock<ILogger<NbaAgent>> _mockLogger;
    private readonly NbaAgent _nbaAgent;

    public NbaAgentTests()
    {
        _mockNbaContextService = new Mock<INbaContextService>();
        _mockTenantContextService = new Mock<ITenantContextService>();
        _mockLogger = new Mock<ILogger<NbaAgent>>();
        
        _nbaAgent = new NbaAgent(
            _mockNbaContextService.Object,
            _mockTenantContextService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetCapabilityAsync_ReturnsNbaAgentCapability()
    {
        // Arrange
        var mockIndustryProfile = new Mock<IIndustryProfile>();
        mockIndustryProfile.Setup(x => x.DisplayName).Returns("Real Estate");
        mockIndustryProfile.Setup(x => x.IndustryCode).Returns("RealEstate");
        
        _mockTenantContextService
            .Setup(x => x.GetIndustryProfileAsync())
            .ReturnsAsync(mockIndustryProfile.Object);

        // Act
        var capability = await _nbaAgent.GetCapabilityAsync();

        // Assert
        Assert.NotNull(capability);
        Assert.Equal("NbaAgent", capability.AgentType);
        Assert.Equal("Next Best Action Advisor", capability.DisplayName);
        Assert.Contains("recommend", capability.SupportedTasks);
        Assert.Contains("next_best_action", capability.SupportedTasks);
        Assert.True(capability.IsAvailable);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var requestingAgentId = Guid.NewGuid();

        var request = new AgentRequest
        {
            RequestType = "recommend",
            Message = "What should I do next for this contact?",
            Parameters = new Dictionary<string, object>
            {
                ["contactId"] = contactId,
                ["organizationId"] = organizationId,
                ["requestingAgentId"] = requestingAgentId
            }
        };

        var mockNbaContext = new NbaContext
        {
            ContactId = contactId,
            RecentInteractions = new List<InteractionSummary>(),
            ActiveContactAssignments = new List<ContactAssignmentSummary>(),
            Metadata = new Dictionary<string, object>()
        };

        var mockIndustryProfile = new Mock<IIndustryProfile>();
        mockIndustryProfile.Setup(x => x.DisplayName).Returns("Real Estate");
        mockIndustryProfile.Setup(x => x.NbaActionTypes).Returns(new List<string> { "follow_up", "schedule_showing" });

        _mockNbaContextService
            .Setup(x => x.GetNbaContextAsync(contactId, organizationId, requestingAgentId, 10, 15, true))
            .ReturnsAsync(mockNbaContext);

        _mockTenantContextService
            .Setup(x => x.GetIndustryProfileAsync())
            .ReturnsAsync(mockIndustryProfile.Object);

        // Act
        var response = await _nbaAgent.ProcessRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.Equal("NbaAgent", response.AgentType);
        Assert.Contains("recommendations", response.Data.Keys);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithMissingParameters_ReturnsErrorResponse()
    {
        // Arrange
        var request = new AgentRequest
        {
            RequestType = "recommend",
            Message = "What should I do next?",
            Parameters = new Dictionary<string, object>() // Missing required parameters
        };

        // Act
        var response = await _nbaAgent.ProcessRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("Missing required parameters", response.Message);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithUnsupportedRequestType_ReturnsErrorResponse()
    {
        // Arrange
        var request = new AgentRequest
        {
            RequestType = "unsupported_action",
            Message = "Do something unsupported",
            Parameters = new Dictionary<string, object>
            {
                ["contactId"] = Guid.NewGuid(),
                ["organizationId"] = Guid.NewGuid(),
                ["requestingAgentId"] = Guid.NewGuid()
            }
        };

        // Act
        var response = await _nbaAgent.ProcessRequestAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("Unsupported request type", response.Message);
    }
}
