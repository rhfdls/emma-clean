using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Emma.Core.Services;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Configuration;

namespace Emma.Tests.Unit;

/// <summary>
/// Unit tests for ContextProvider implementation.
/// Tests context management, caching, and intelligence integration.
/// </summary>
public class ContextProviderTests : IDisposable
{
    private readonly Mock<ITenantContextService> _mockTenantService;
    private readonly Mock<IContextIntelligenceService> _mockIntelligenceService;
    private readonly Mock<IFeatureFlagService> _mockFeatureFlagService;
    private readonly Mock<IAgentRegistry> _mockAgentRegistry;
    private readonly Mock<ILogger<ContextProvider>> _mockLogger;
    private readonly ContextProvider _contextProvider;

    public ContextProviderTests()
    {
        _mockTenantService = new Mock<ITenantContextService>();
        _mockIntelligenceService = new Mock<IContextIntelligenceService>();
        _mockFeatureFlagService = new Mock<IFeatureFlagService>();
        _mockAgentRegistry = new Mock<IAgentRegistry>();
        _mockLogger = new Mock<ILogger<ContextProvider>>();

        _contextProvider = new ContextProvider(
            _mockTenantService.Object,
            _mockIntelligenceService.Object,
            _mockFeatureFlagService.Object,
            _mockAgentRegistry.Object,
            _mockLogger.Object);

        // Setup default feature flag responses
        _mockFeatureFlagService.Setup(x => x.IsEnabledAsync(FeatureFlags.DYNAMIC_AGENT_ROUTING))
            .ReturnsAsync(true);
        _mockFeatureFlagService.Setup(x => x.IsEnabledAsync(FeatureFlags.AGENT_REGISTRY_ENABLED))
            .ReturnsAsync(true);
        _mockFeatureFlagService.Setup(x => x.IsEnabledAsync(FeatureFlags.LIFECYCLE_HOOKS_ENABLED))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task GetConversationContextAsync_Should_CreateNewContext_WhenNotCached()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Act
        var context = await _contextProvider.GetConversationContextAsync(conversationId, traceId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(conversationId, context.ConversationId);
        Assert.Equal(ConversationState.Started, context.State);
        Assert.NotEqual(Guid.Empty, context.AuditId);
        Assert.NotEmpty(context.Reason);
        Assert.True(context.LastUpdated <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetConversationContextAsync_Should_ReturnCachedContext_WhenExists()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Act - First call creates context
        var firstContext = await _contextProvider.GetConversationContextAsync(conversationId, traceId);
        var firstAuditId = firstContext.AuditId;

        // Act - Second call should return cached context with new audit ID
        var secondContext = await _contextProvider.GetConversationContextAsync(conversationId, traceId);

        // Assert
        Assert.NotNull(secondContext);
        Assert.Equal(conversationId, secondContext.ConversationId);
        Assert.Equal(ConversationState.Started, secondContext.State);
        Assert.NotEqual(firstAuditId, secondContext.AuditId); // New audit ID for each call
        Assert.Contains("cache", secondContext.Reason.ToLower());
    }

    [Fact]
    public async Task GetTenantContextAsync_Should_ReturnTenantWithFeatures()
    {
        // Arrange
        var traceId = Guid.NewGuid().ToString();
        var mockTenant = new Tenant { Id = "test-tenant", Name = "Test Tenant" };
        var mockIndustryProfile = new IndustryProfile
        {
            Industry = "Technology",
            Specialization = "AI/ML",
            ComplianceRequirements = new List<string> { "SOC2", "GDPR" }
        };

        _mockTenantService.Setup(x => x.GetCurrentTenantAsync())
            .ReturnsAsync(mockTenant);
        _mockTenantService.Setup(x => x.GetIndustryProfileAsync())
            .ReturnsAsync(mockIndustryProfile);

        // Act
        var tenantContext = await _contextProvider.GetTenantContextAsync(traceId);

        // Assert
        Assert.NotNull(tenantContext);
        Assert.Equal("test-tenant", tenantContext.TenantId);
        Assert.Equal(mockIndustryProfile, tenantContext.IndustryProfile);
        Assert.Contains("dynamic-agent-routing", tenantContext.EnabledFeatures);
        Assert.Contains("agent-registry", tenantContext.EnabledFeatures);
        Assert.Contains("lifecycle-hooks", tenantContext.EnabledFeatures);
        Assert.NotEqual(Guid.Empty, tenantContext.AuditId);
        Assert.NotEmpty(tenantContext.Reason);
    }

    [Fact]
    public async Task GetTenantContextAsync_Should_HandleNullTenant()
    {
        // Arrange
        var traceId = Guid.NewGuid().ToString();
        _mockTenantService.Setup(x => x.GetCurrentTenantAsync())
            .ReturnsAsync((Tenant?)null);
        _mockTenantService.Setup(x => x.GetIndustryProfileAsync())
            .ReturnsAsync(new IndustryProfile());

        // Act
        var tenantContext = await _contextProvider.GetTenantContextAsync(traceId);

        // Assert
        Assert.NotNull(tenantContext);
        Assert.Equal("default", tenantContext.TenantId);
        Assert.NotEmpty(tenantContext.EnabledFeatures);
    }

    [Fact]
    public async Task GetAgentContextAsync_Should_ReturnContextFromRegistry_WhenRegistered()
    {
        // Arrange
        var agentType = "test-agent";
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();
        var mockMetadata = new AgentRegistrationMetadata
        {
            Name = "Test Agent",
            Version = "1.0.0",
            Capabilities = new List<string> { "test-capability" },
            IsFactoryCreated = true,
            Reason = "Test agent metadata"
        };

        _mockAgentRegistry.Setup(x => x.IsAgentRegisteredAsync(agentType))
            .ReturnsAsync(true);
        _mockAgentRegistry.Setup(x => x.GetAgentMetadataAsync(agentType))
            .ReturnsAsync(mockMetadata);
        _mockAgentRegistry.Setup(x => x.GetAgentHealthStatusAsync(agentType))
            .ReturnsAsync(AgentHealthStatus.Healthy);

        // Act
        var agentContext = await _contextProvider.GetAgentContextAsync(agentType, conversationId, traceId);

        // Assert
        Assert.NotNull(agentContext);
        Assert.Equal(agentType, agentContext.AgentType);
        Assert.Contains("test-capability", agentContext.Capabilities);
        Assert.Equal(AgentState.Ready, agentContext.State);
        Assert.Equal("1.0.0", agentContext.Configuration["version"]);
        Assert.Equal(true, agentContext.Configuration["isFactoryCreated"]);
        Assert.NotNull(agentContext.Metrics);
        Assert.NotEqual(Guid.Empty, agentContext.AuditId);
    }

    [Fact]
    public async Task GetAgentContextAsync_Should_ReturnFallbackContext_WhenNotRegistered()
    {
        // Arrange
        var agentType = "nba";
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        _mockFeatureFlagService.Setup(x => x.IsEnabledAsync(FeatureFlags.AGENT_REGISTRY_ENABLED))
            .ReturnsAsync(false);

        // Act
        var agentContext = await _contextProvider.GetAgentContextAsync(agentType, conversationId, traceId);

        // Assert
        Assert.NotNull(agentContext);
        Assert.Equal(agentType, agentContext.AgentType);
        Assert.Contains("next-best-action", agentContext.Capabilities);
        Assert.Contains("recommendations", agentContext.Capabilities);
        Assert.Contains("automation", agentContext.Capabilities);
        Assert.Equal(AgentState.Ready, agentContext.State);
    }

    [Fact]
    public async Task UpdateConversationContextAsync_Should_UpdateCachedContext()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Get initial context
        var initialContext = await _contextProvider.GetConversationContextAsync(conversationId, traceId);
        var initialTimestamp = initialContext.LastUpdated;

        // Modify context
        initialContext.State = ConversationState.Active;
        initialContext.Messages.Add("Test message");

        // Small delay to ensure timestamp difference
        await Task.Delay(10);

        // Act
        await _contextProvider.UpdateConversationContextAsync(conversationId, initialContext, traceId);

        // Get updated context
        var updatedContext = await _contextProvider.GetConversationContextAsync(conversationId, traceId);

        // Assert
        Assert.Equal(ConversationState.Active, updatedContext.State);
        Assert.Single(updatedContext.Messages);
        Assert.Equal("Test message", updatedContext.Messages[0]);
        Assert.True(updatedContext.LastUpdated > initialTimestamp);
    }

    [Fact]
    public async Task GetContextIntelligenceAsync_Should_ReturnIntelligence()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Act
        var intelligence = await _contextProvider.GetContextIntelligenceAsync(conversationId, traceId);

        // Assert
        Assert.NotNull(intelligence);
        Assert.NotNull(intelligence.Sentiment);
        Assert.Equal("Positive", intelligence.Sentiment.Label);
        Assert.True(intelligence.Sentiment.Score > 0);
        Assert.True(intelligence.Sentiment.Confidence > 0);
        Assert.NotNull(intelligence.BuyingSignals);
        Assert.NotNull(intelligence.Recommendations);
        Assert.NotNull(intelligence.Insights);
        Assert.Equal("General Inquiry", intelligence.Insights.Intent);
        Assert.Equal("Medium", intelligence.Insights.Urgency);
        Assert.True(intelligence.Confidence > 0);
        Assert.NotEqual(Guid.Empty, intelligence.AuditId);
        Assert.NotEmpty(intelligence.Reason);
    }

    [Fact]
    public async Task ClearContextAsync_Should_RemoveFromCache()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Create context first
        var initialContext = await _contextProvider.GetConversationContextAsync(conversationId, traceId);
        Assert.Contains("cache", (await _contextProvider.GetConversationContextAsync(conversationId, traceId)).Reason.ToLower());

        // Act
        await _contextProvider.ClearContextAsync(conversationId, "Test cleanup", traceId);

        // Get context again - should create new one
        var newContext = await _contextProvider.GetConversationContextAsync(conversationId, traceId);

        // Assert
        Assert.NotEqual(initialContext.AuditId, newContext.AuditId);
        Assert.Contains("new conversation", newContext.Reason.ToLower());
    }

    [Fact]
    public async Task GetAgentContextAsync_Should_MapHealthStatusToAgentState()
    {
        // Arrange
        var agentType = "health-test-agent";
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        _mockAgentRegistry.Setup(x => x.IsAgentRegisteredAsync(agentType))
            .ReturnsAsync(true);
        _mockAgentRegistry.Setup(x => x.GetAgentMetadataAsync(agentType))
            .ReturnsAsync(new AgentRegistrationMetadata
            {
                Name = "Health Test Agent",
                Version = "1.0.0",
                Capabilities = new List<string>(),
                IsFactoryCreated = false,
                Reason = "Health mapping test"
            });

        // Test different health statuses
        var healthStatusMappings = new Dictionary<AgentHealthStatus, AgentState>
        {
            { AgentHealthStatus.Healthy, AgentState.Ready },
            { AgentHealthStatus.Degraded, AgentState.Busy },
            { AgentHealthStatus.Unhealthy, AgentState.Error },
            { AgentHealthStatus.Unknown, AgentState.Offline }
        };

        foreach (var mapping in healthStatusMappings)
        {
            // Arrange
            _mockAgentRegistry.Setup(x => x.GetAgentHealthStatusAsync(agentType))
                .ReturnsAsync(mapping.Key);

            // Act
            var agentContext = await _contextProvider.GetAgentContextAsync(agentType, conversationId, traceId);

            // Assert
            Assert.Equal(mapping.Value, agentContext.State);
        }
    }

    public void Dispose()
    {
        // ContextProvider doesn't implement IDisposable, but included for consistency
    }
}
