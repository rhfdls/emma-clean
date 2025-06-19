using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Xunit;
using Emma.Core.Interfaces;
using Emma.Core.Services;
using Emma.Core.Models;
using Emma.Core.Configuration;
using Emma.Core.Extensions;

namespace Emma.Tests.Integration;

/// <summary>
/// Integration tests for Sprint 1 EMMA Agent Factory implementation.
/// Validates dynamic agent registry, routing, feature flags, and context provider.
/// </summary>
public class Sprint1IntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public Sprint1IntegrationTests()
    {
        // Setup test configuration
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:DynamicAgentRouting"] = "true",
                ["FeatureManagement:AgentRegistryEnabled"] = "true",
                ["FeatureManagement:LifecycleHooksEnabled"] = "true",
                ["FeatureManagement:ExplainabilityFramework"] = "true",
                ["Logging:LogLevel:Default"] = "Information"
            });
        
        _configuration = configBuilder.Build();

        // Setup DI container with Sprint 1 services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add configuration
        services.AddSingleton(_configuration);
        
        // Add mock implementations for dependencies
        services.AddScoped<ITenantContextService, MockTenantContextService>();
        services.AddScoped<IContextIntelligenceService, MockContextIntelligenceService>();
        services.AddScoped<INbaAgent, MockNbaAgent>();
        services.AddScoped<IContextIntelligenceAgent, MockContextIntelligenceAgent>();
        services.AddScoped<IIntentClassificationAgent, MockIntentClassificationAgent>();
        services.AddScoped<IResourceAgent, MockResourceAgent>();
        
        // Add Sprint 1 services
        services.AddEmmaSprint1Services();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task AgentRegistry_Should_RegisterAndRetrieveAgents()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IAgentRegistry>();
        var mockAgent = _serviceProvider.GetRequiredService<INbaAgent>();
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Test NBA Agent",
            Description = "Test agent for integration testing",
            Version = "1.0.0",
            Capabilities = new List<string> { "test", "integration" },
            IsFactoryCreated = false,
            Reason = "Integration test agent registration"
        };

        // Act
        await registry.RegisterAgentAsync("test-nba", mockAgent, metadata);
        var isRegistered = await registry.IsAgentRegisteredAsync("test-nba");
        var retrievedAgent = await registry.GetAgentAsync("test-nba");
        var retrievedMetadata = await registry.GetAgentMetadataAsync("test-nba");
        var healthStatus = await registry.GetAgentHealthStatusAsync("test-nba");

        // Assert
        Assert.True(isRegistered);
        Assert.NotNull(retrievedAgent);
        Assert.NotNull(retrievedMetadata);
        Assert.Equal("Test NBA Agent", retrievedMetadata.Name);
        Assert.Equal("1.0.0", retrievedMetadata.Version);
        Assert.Contains("test", retrievedMetadata.Capabilities);
        Assert.Equal(AgentHealthStatus.Healthy, healthStatus);
    }

    [Fact]
    public async Task FeatureFlagService_Should_EvaluateFlags()
    {
        // Arrange
        var featureFlagService = _serviceProvider.GetRequiredService<IFeatureFlagService>();

        // Act
        var isDynamicRoutingEnabled = await featureFlagService.IsEnabledAsync(FeatureFlags.DYNAMIC_AGENT_ROUTING);
        var isRegistryEnabled = await featureFlagService.IsEnabledAsync(FeatureFlags.AGENT_REGISTRY_ENABLED);
        var isLifecycleEnabled = await featureFlagService.IsEnabledAsync(FeatureFlags.LIFECYCLE_HOOKS_ENABLED);
        var isExplainabilityEnabled = await featureFlagService.IsEnabledAsync(FeatureFlags.EXPLAINABILITY_FRAMEWORK);

        // Assert
        Assert.True(isDynamicRoutingEnabled);
        Assert.True(isRegistryEnabled);
        Assert.True(isLifecycleEnabled);
        Assert.True(isExplainabilityEnabled);
    }

    [Fact]
    public async Task ContextProvider_Should_ManageConversationContext()
    {
        // Arrange
        var contextProvider = _serviceProvider.GetRequiredService<IContextProvider>();
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Act
        var initialContext = await contextProvider.GetConversationContextAsync(conversationId, traceId);
        
        // Modify context
        initialContext.State = ConversationState.Active;
        initialContext.Messages.Add("Test message");

        await contextProvider.UpdateConversationContextAsync(conversationId, initialContext, traceId);
        var updatedContext = await contextProvider.GetConversationContextAsync(conversationId, traceId);

        // Assert
        Assert.NotNull(initialContext);
        Assert.Equal(conversationId, initialContext.ConversationId);
        Assert.NotEqual(Guid.Empty, initialContext.AuditId);
        Assert.NotEmpty(initialContext.Reason);
        
        Assert.NotNull(updatedContext);
        Assert.Equal(ConversationState.Active, updatedContext.State);
        Assert.Single(updatedContext.Messages);
        Assert.Equal("Test message", updatedContext.Messages[0]);
    }

    [Fact]
    public async Task ContextProvider_Should_GetTenantContext()
    {
        // Arrange
        var contextProvider = _serviceProvider.GetRequiredService<IContextProvider>();
        var traceId = Guid.NewGuid().ToString();

        // Act
        var tenantContext = await contextProvider.GetTenantContextAsync(traceId);

        // Assert
        Assert.NotNull(tenantContext);
        Assert.NotEmpty(tenantContext.TenantId);
        Assert.NotNull(tenantContext.IndustryProfile);
        Assert.NotEmpty(tenantContext.EnabledFeatures);
        Assert.Contains("dynamic-agent-routing", tenantContext.EnabledFeatures);
        Assert.Contains("agent-registry", tenantContext.EnabledFeatures);
        Assert.NotEqual(Guid.Empty, tenantContext.AuditId);
        Assert.NotEmpty(tenantContext.Reason);
    }

    [Fact]
    public async Task ContextProvider_Should_GetAgentContext()
    {
        // Arrange
        var contextProvider = _serviceProvider.GetRequiredService<IContextProvider>();
        var registry = _serviceProvider.GetRequiredService<IAgentRegistry>();
        var mockAgent = _serviceProvider.GetRequiredService<INbaAgent>();
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Register an agent first
        await registry.RegisterAgentAsync("test-agent", mockAgent, new AgentRegistrationMetadata
        {
            Name = "Test Agent",
            Version = "1.0.0",
            Capabilities = new List<string> { "test-capability" },
            IsFactoryCreated = false,
            Reason = "Test agent for context validation"
        });

        // Act
        var agentContext = await contextProvider.GetAgentContextAsync("test-agent", conversationId, traceId);

        // Assert
        Assert.NotNull(agentContext);
        Assert.Equal("test-agent", agentContext.AgentType);
        Assert.Contains("test-capability", agentContext.Capabilities);
        Assert.Equal(AgentState.Ready, agentContext.State);
        Assert.NotNull(agentContext.Metrics);
        Assert.NotEqual(Guid.Empty, agentContext.AuditId);
        Assert.NotEmpty(agentContext.Reason);
    }

    [Fact]
    public async Task ContextProvider_Should_GetContextIntelligence()
    {
        // Arrange
        var contextProvider = _serviceProvider.GetRequiredService<IContextProvider>();
        var conversationId = Guid.NewGuid();
        var traceId = Guid.NewGuid().ToString();

        // Act
        var intelligence = await contextProvider.GetContextIntelligenceAsync(conversationId, traceId);

        // Assert
        Assert.NotNull(intelligence);
        Assert.NotNull(intelligence.Sentiment);
        Assert.NotNull(intelligence.BuyingSignals);
        Assert.NotNull(intelligence.Recommendations);
        Assert.NotNull(intelligence.Insights);
        Assert.True(intelligence.Confidence > 0);
        Assert.NotEqual(Guid.Empty, intelligence.AuditId);
        Assert.NotEmpty(intelligence.Reason);
    }

    [Fact]
    public async Task AgentRegistry_Should_HandleUnregistration()
    {
        // Arrange
        var registry = _serviceProvider.GetRequiredService<IAgentRegistry>();
        var mockAgent = _serviceProvider.GetRequiredService<INbaAgent>();
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Temporary Agent",
            Version = "1.0.0",
            Capabilities = new List<string> { "temporary" },
            IsFactoryCreated = false,
            Reason = "Temporary agent for unregistration test"
        };

        // Act
        await registry.RegisterAgentAsync("temp-agent", mockAgent, metadata);
        var isRegisteredBefore = await registry.IsAgentRegisteredAsync("temp-agent");
        
        await registry.UnregisterAgentAsync("temp-agent", "Integration test cleanup");
        var isRegisteredAfter = await registry.IsAgentRegisteredAsync("temp-agent");

        // Assert
        Assert.True(isRegisteredBefore);
        Assert.False(isRegisteredAfter);
    }

    [Fact]
    public void ApiVersioning_Should_ValidateCompatibility()
    {
        // Arrange & Act
        var isV1Compatible = ApiVersioning.Compatibility.IsCompatible("1.0", "1.1");
        var isV2Compatible = ApiVersioning.Compatibility.IsCompatible("2.0", "1.1");
        var isInvalidCompatible = ApiVersioning.Compatibility.IsCompatible("invalid", "1.0");

        // Assert
        Assert.True(isV1Compatible);
        Assert.False(isV2Compatible);
        Assert.False(isInvalidCompatible);
    }

    [Fact]
    public void ApiVersioning_Should_CheckFeatureAvailability()
    {
        // Arrange & Act
        var isDynamicRoutingInV1 = ApiVersioning.VersionFeatures.IsFeatureAvailable("dynamic-agent-routing", "1.0");
        var isAdvancedMonitoringInV1 = ApiVersioning.VersionFeatures.IsFeatureAvailable("advanced-monitoring", "1.0");
        var isAdvancedMonitoringInV11 = ApiVersioning.VersionFeatures.IsFeatureAvailable("advanced-monitoring", "1.1");

        // Assert
        Assert.True(isDynamicRoutingInV1);
        Assert.False(isAdvancedMonitoringInV1);
        Assert.True(isAdvancedMonitoringInV11);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

#region Mock Implementations

public class MockTenantContextService : ITenantContextService
{
    public Task<Tenant?> GetCurrentTenantAsync()
    {
        return Task.FromResult<Tenant?>(new Tenant { Id = "test-tenant", Name = "Test Tenant" });
    }

    public Task<IndustryProfile> GetIndustryProfileAsync()
    {
        return Task.FromResult(new IndustryProfile
        {
            Industry = "Technology",
            Specialization = "AI/ML",
            ComplianceRequirements = new List<string> { "SOC2", "GDPR" }
        });
    }
}

public class MockContextIntelligenceService : IContextIntelligenceService
{
    public Task<SentimentAnalysis> AnalyzeSentimentAsync(string text, string traceId)
    {
        return Task.FromResult(new SentimentAnalysis
        {
            Score = 0.7,
            Label = "Positive",
            Confidence = 0.85
        });
    }
}

public class MockNbaAgent : INbaAgent
{
    public Task<AgentResponse<List<ScheduledAction>>> GenerateActionsAsync(
        Contact contact, List<Interaction> recentInteractions, string traceId)
    {
        return Task.FromResult(new AgentResponse<List<ScheduledAction>>
        {
            Success = true,
            Data = new List<ScheduledAction>(),
            AgentType = "MockNbaAgent",
            TraceId = traceId,
            AuditId = Guid.NewGuid(),
            Reason = "Mock NBA agent response for testing"
        });
    }
}

public class MockContextIntelligenceAgent : IContextIntelligenceAgent
{
    // Mock implementation
}

public class MockIntentClassificationAgent : IIntentClassificationAgent
{
    public Task<IntentClassificationResult> ClassifyIntentAsync(string userInput, Guid conversationId, string traceId)
    {
        return Task.FromResult(new IntentClassificationResult
        {
            Intent = AgentIntent.ContactManagement,
            Confidence = 0.9,
            AuditId = Guid.NewGuid(),
            Reason = "Mock intent classification for testing"
        });
    }
}

public class MockResourceAgent : IResourceAgent
{
    // Mock implementation
}

#endregion
