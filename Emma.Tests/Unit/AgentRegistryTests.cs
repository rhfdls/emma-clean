using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Emma.Core.Services;
using Emma.Core.Interfaces;
using Emma.Core.Models;

namespace Emma.Tests.Unit;

/// <summary>
/// Unit tests for AgentRegistry implementation.
/// Tests thread safety, lifecycle management, and health monitoring.
/// </summary>
public class AgentRegistryTests : IDisposable
{
    private readonly Mock<ILogger<AgentRegistry>> _mockLogger;
    private readonly AgentRegistry _agentRegistry;
    private readonly Mock<INbaAgent> _mockAgent;

    public AgentRegistryTests()
    {
        _mockLogger = new Mock<ILogger<AgentRegistry>>();
        _agentRegistry = new AgentRegistry(_mockLogger.Object);
        _mockAgent = new Mock<INbaAgent>();
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_RegisterAgent_Successfully()
    {
        // Arrange
        var agentType = "test-agent";
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Test Agent",
            Description = "Test agent for unit testing",
            Version = "1.0.0",
            Capabilities = new List<string> { "test" },
            IsFactoryCreated = false,
            Reason = "Unit test registration"
        };

        // Act
        await _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata);

        // Assert
        var isRegistered = await _agentRegistry.IsAgentRegisteredAsync(agentType);
        Assert.True(isRegistered);
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_ThrowException_WhenAgentAlreadyRegistered()
    {
        // Arrange
        var agentType = "duplicate-agent";
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Duplicate Agent",
            Version = "1.0.0",
            Capabilities = new List<string>(),
            IsFactoryCreated = false,
            Reason = "First registration"
        };

        await _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata));
        
        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public async Task GetAgentAsync_Should_ReturnAgent_WhenRegistered()
    {
        // Arrange
        var agentType = "get-agent-test";
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Get Agent Test",
            Version = "1.0.0",
            Capabilities = new List<string>(),
            IsFactoryCreated = false,
            Reason = "Get agent test registration"
        };

        await _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata);

        // Act
        var retrievedAgent = await _agentRegistry.GetAgentAsync(agentType);

        // Assert
        Assert.NotNull(retrievedAgent);
        Assert.Equal(_mockAgent.Object, retrievedAgent);
    }

    [Fact]
    public async Task GetAgentAsync_Should_ReturnNull_WhenNotRegistered()
    {
        // Act
        var retrievedAgent = await _agentRegistry.GetAgentAsync("non-existent-agent");

        // Assert
        Assert.Null(retrievedAgent);
    }

    [Fact]
    public async Task GetAgentMetadataAsync_Should_ReturnMetadata_WhenRegistered()
    {
        // Arrange
        var agentType = "metadata-test";
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Metadata Test Agent",
            Description = "Agent for metadata testing",
            Version = "2.1.0",
            Capabilities = new List<string> { "metadata", "testing" },
            IsFactoryCreated = true,
            Reason = "Metadata test registration"
        };

        await _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata);

        // Act
        var retrievedMetadata = await _agentRegistry.GetAgentMetadataAsync(agentType);

        // Assert
        Assert.NotNull(retrievedMetadata);
        Assert.Equal("Metadata Test Agent", retrievedMetadata.Name);
        Assert.Equal("Agent for metadata testing", retrievedMetadata.Description);
        Assert.Equal("2.1.0", retrievedMetadata.Version);
        Assert.Contains("metadata", retrievedMetadata.Capabilities);
        Assert.Contains("testing", retrievedMetadata.Capabilities);
        Assert.True(retrievedMetadata.IsFactoryCreated);
    }

    [Fact]
    public async Task GetAgentHealthStatusAsync_Should_ReturnHealthy_ForHealthyAgent()
    {
        // Arrange
        var agentType = "healthy-agent";
        var mockLifecycleAgent = new Mock<IAgentLifecycle>();
        mockLifecycleAgent.Setup(x => x.OnHealthCheckAsync(It.IsAny<string>()))
            .ReturnsAsync(AgentHealthCheckResult.Healthy("Agent is operating normally"));

        var metadata = new AgentRegistrationMetadata
        {
            Name = "Healthy Agent",
            Version = "1.0.0",
            Capabilities = new List<string>(),
            IsFactoryCreated = false,
            Reason = "Health check test registration"
        };

        await _agentRegistry.RegisterAgentAsync(agentType, mockLifecycleAgent.Object, metadata);

        // Act
        var healthStatus = await _agentRegistry.GetAgentHealthStatusAsync(agentType);

        // Assert
        Assert.Equal(AgentHealthStatus.Healthy, healthStatus);
    }

    [Fact]
    public async Task UnregisterAgentAsync_Should_RemoveAgent_Successfully()
    {
        // Arrange
        var agentType = "unregister-test";
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Unregister Test Agent",
            Version = "1.0.0",
            Capabilities = new List<string>(),
            IsFactoryCreated = false,
            Reason = "Unregister test registration"
        };

        await _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata);
        var isRegisteredBefore = await _agentRegistry.IsAgentRegisteredAsync(agentType);

        // Act
        await _agentRegistry.UnregisterAgentAsync(agentType, "Unit test cleanup");
        var isRegisteredAfter = await _agentRegistry.IsAgentRegisteredAsync(agentType);

        // Assert
        Assert.True(isRegisteredBefore);
        Assert.False(isRegisteredAfter);
    }

    [Fact]
    public async Task GetRegisteredAgentTypesAsync_Should_ReturnAllRegisteredTypes()
    {
        // Arrange
        var agentTypes = new[] { "agent1", "agent2", "agent3" };
        var metadata = new AgentRegistrationMetadata
        {
            Name = "Test Agent",
            Version = "1.0.0",
            Capabilities = new List<string>(),
            IsFactoryCreated = false,
            Reason = "Multiple agent registration test"
        };

        foreach (var agentType in agentTypes)
        {
            await _agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata);
        }

        // Act
        var registeredTypes = await _agentRegistry.GetRegisteredAgentTypesAsync();

        // Assert
        Assert.Equal(agentTypes.Length, registeredTypes.Count);
        foreach (var agentType in agentTypes)
        {
            Assert.Contains(agentType, registeredTypes);
        }
    }

    [Fact]
    public async Task ConcurrentOperations_Should_BeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var agentCount = 50; // Reduced for faster test execution

        // Act - Register agents concurrently
        for (int i = 0; i < agentCount; i++)
        {
            var agentType = $"concurrent-agent-{i}";
            var metadata = new AgentRegistrationMetadata
            {
                Name = $"Concurrent Agent {i}",
                Version = "1.0.0",
                Capabilities = new List<string>(),
                IsFactoryCreated = false,
                Reason = "Concurrent registration test"
            };

            tasks.Add(_agentRegistry.RegisterAgentAsync(agentType, _mockAgent.Object, metadata));
        }

        await Task.WhenAll(tasks);

        // Assert
        var registeredTypes = await _agentRegistry.GetRegisteredAgentTypesAsync();
        Assert.Equal(agentCount, registeredTypes.Count);
    }

    public void Dispose()
    {
        _agentRegistry?.Dispose();
    }
}
