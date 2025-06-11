using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Core.Agents;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;

namespace Emma.Core.Services
{
    /// <summary>
    /// Agent registry service for managing agent catalog and discovery
    /// Supports A2A Agent Card format and dynamic registration
    /// </summary>
    public class AgentRegistryService : IAgentRegistryService
    {
        private readonly ILogger<AgentRegistryService> _logger;
        private readonly ConcurrentDictionary<string, ISpecializedAgent> _agents;
        private readonly ConcurrentDictionary<string, AgentCapability> _capabilities;
        private readonly ConcurrentDictionary<string, AgentHealthInfo> _healthStatuses;
        private readonly ConcurrentDictionary<string, Models.AgentPerformanceMetrics> _performanceMetrics;

        public AgentRegistryService(ILogger<AgentRegistryService> logger)
        {
            _logger = logger;
            _agents = new ConcurrentDictionary<string, ISpecializedAgent>();
            _capabilities = new ConcurrentDictionary<string, AgentCapability>();
            _healthStatuses = new ConcurrentDictionary<string, AgentHealthInfo>();
            _performanceMetrics = new ConcurrentDictionary<string, Models.AgentPerformanceMetrics>();
        }

        public async Task<bool> RegisterAgentAsync(string agentId, ISpecializedAgent agent, AgentCapability capability)
        {
            try
            {
                _logger.LogInformation("Registering agent {AgentId} with capabilities: {Capabilities}",
                    agentId, string.Join(", ", capability.SupportedIntents));

                // Validate the agent capability
                var validationResult = await ValidateAgentCapabilityAsync(capability);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Agent {AgentId} registration failed validation: {Errors}",
                        agentId, string.Join(", ", validationResult.Errors));
                    return false;
                }

                // Register the agent
                _agents.TryAdd(agentId, agent);
                _capabilities.TryAdd(agentId, capability);
                
                // Initialize health status
                var healthStatus = new AgentHealthInfo
                {
                    AgentId = agentId,
                    IsHealthy = true,
                    Status = "Registered",
                    LastChecked = DateTime.UtcNow
                };
                _healthStatuses.TryAdd(agentId, healthStatus);

                // Initialize performance metrics
                var metrics = new Models.AgentPerformanceMetrics
                {
                    AgentId = agentId,
                    TotalRequests = 0,
                    SuccessfulRequests = 0,
                    AverageResponseTimeMs = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _performanceMetrics.TryAdd(agentId, metrics);

                _logger.LogInformation("Agent {AgentId} registered successfully", agentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering agent {AgentId}", agentId);
                return false;
            }
        }

        public async Task<bool> UnregisterAgentAsync(string agentId)
        {
            try
            {
                _logger.LogInformation("Unregistering agent {AgentId}", agentId);

                var removed = _agents.TryRemove(agentId, out _) &&
                             _capabilities.TryRemove(agentId, out _) &&
                             _healthStatuses.TryRemove(agentId, out _) &&
                             _performanceMetrics.TryRemove(agentId, out _);

                if (removed)
                {
                    _logger.LogInformation("Agent {AgentId} unregistered successfully", agentId);
                }
                else
                {
                    _logger.LogWarning("Agent {AgentId} was not found for unregistration", agentId);
                }

                return await Task.FromResult(removed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering agent {AgentId}", agentId);
                return false;
            }
        }

        public async Task<ISpecializedAgent?> GetAgentAsync(string agentId)
        {
            _agents.TryGetValue(agentId, out var agent);
            return await Task.FromResult(agent);
        }

        public async Task<AgentCapability?> GetAgentCapabilityAsync(string agentId)
        {
            _capabilities.TryGetValue(agentId, out var capability);
            return await Task.FromResult(capability);
        }

        public async Task<Dictionary<string, AgentCapability>> GetAllAgentCapabilitiesAsync()
        {
            var result = new Dictionary<string, AgentCapability>();
            
            foreach (var kvp in _capabilities)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            return await Task.FromResult(result);
        }

        public async Task<List<AgentCapability>> FindAgentsForIntentAsync(AgentIntent intent, string? industry = null)
        {
            var matchingAgents = new List<AgentCapability>();

            foreach (var capability in _capabilities.Values)
            {
                // Check if agent supports the intent
                if (!capability.SupportedIntents.Contains(intent))
                    continue;

                // Check if agent is active
                if (!capability.IsActive)
                    continue;

                // Check industry filter if provided
                if (!string.IsNullOrEmpty(industry) && 
                    capability.SupportedIndustries.Any() && 
                    !capability.SupportedIndustries.Contains(industry, StringComparer.OrdinalIgnoreCase))
                    continue;

                matchingAgents.Add(capability);
            }

            _logger.LogDebug("Found {Count} agents for intent {Intent} and industry {Industry}",
                matchingAgents.Count, intent, industry ?? "any");

            return await Task.FromResult(matchingAgents);
        }

        public async Task<AgentHealthInfo> GetAgentHealthAsync(string agentId)
        {
            if (_healthStatuses.TryGetValue(agentId, out var status))
            {
                // Update health check if it's been too long
                if (DateTime.UtcNow - status.LastChecked > TimeSpan.FromMinutes(5))
                {
                    await UpdateAgentHealthAsync(agentId);
                    _healthStatuses.TryGetValue(agentId, out status);
                }
            }
            else
            {
                status = new AgentHealthInfo
                {
                    AgentId = agentId,
                    IsHealthy = false,
                    Status = "Not Found",
                    LastChecked = DateTime.UtcNow
                };
            }

            return status!;
        }

        public async Task<Dictionary<string, AgentHealthInfo>> GetAllAgentHealthAsync()
        {
            var result = new Dictionary<string, AgentHealthInfo>();
            
            foreach (var kvp in _healthStatuses)
            {
                result[kvp.Key] = await GetAgentHealthAsync(kvp.Key);
            }
            
            return result;
        }

        public async Task UpdateAgentMetricsAsync(string agentId, long responseTimeMs, bool success, double confidence)
        {
            try
            {
                if (_performanceMetrics.TryGetValue(agentId, out var metrics))
                {
                    metrics.TotalRequests++;
                    if (success)
                    {
                        metrics.SuccessfulRequests++;
                    }

                    // Update average response time (rolling average)
                    metrics.AverageResponseTimeMs = (metrics.AverageResponseTimeMs + responseTimeMs) / 2;
                    metrics.LastUpdated = DateTime.UtcNow;

                    // Update success rate
                    metrics.SuccessRate = (double)metrics.SuccessfulRequests / metrics.TotalRequests;

                    // Update confidence score (rolling average)
                    metrics.AverageConfidenceScore = (metrics.AverageConfidenceScore + confidence) / 2;

                    _logger.LogDebug("Updated metrics for agent {AgentId}: Success Rate: {SuccessRate:P2}, Avg Response Time: {ResponseTime}ms",
                        agentId, metrics.SuccessRate, metrics.AverageResponseTimeMs);

                    // Update agent capability with performance metrics
                    if (_capabilities.TryGetValue(agentId, out var capability))
                    {
                        capability.PerformanceMetrics = metrics;
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metrics for agent {AgentId}", agentId);
            }
        }

        public async Task<int> LoadAgentCatalogAsync(string catalogPath)
        {
            var loadedCount = 0;

            try
            {
                _logger.LogInformation("Loading agent catalog from path: {CatalogPath}", catalogPath);

                if (!Directory.Exists(catalogPath))
                {
                    _logger.LogWarning("Agent catalog directory does not exist: {CatalogPath}", catalogPath);
                    return 0;
                }

                var agentCardFiles = Directory.GetFiles(catalogPath, "*.json", SearchOption.AllDirectories);
                
                foreach (var file in agentCardFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var agentCard = JsonSerializer.Deserialize<AgentCard>(content);
                        
                        if (agentCard != null)
                        {
                            var capability = ConvertAgentCardToCapability(agentCard);
                            
                            // Note: We can't register the actual agent instance from just the card
                            // This would typically be done by the agent implementations themselves
                            // For now, we just store the capability information
                            _capabilities.TryAdd(agentCard.AgentId, capability);
                            
                            _logger.LogDebug("Loaded agent card for {AgentId} from {File}", 
                                agentCard.AgentId, Path.GetFileName(file));
                            
                            loadedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading agent card from file: {File}", file);
                    }
                }

                _logger.LogInformation("Loaded {Count} agent cards from catalog", loadedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading agent catalog from {CatalogPath}", catalogPath);
            }

            return loadedCount;
        }

        public async Task<AgentValidationResult> ValidateAgentCapabilityAsync(AgentCapability capability)
        {
            var result = new AgentValidationResult();

            try
            {
                // Validate required fields
                if (string.IsNullOrEmpty(capability.AgentId))
                {
                    result.Errors.Add("AgentId is required");
                }

                if (string.IsNullOrEmpty(capability.Name))
                {
                    result.Errors.Add("Agent name is required");
                }

                if (!capability.SupportedIntents.Any())
                {
                    result.Errors.Add("At least one supported intent is required");
                }

                if (string.IsNullOrEmpty(capability.Version))
                {
                    result.Warnings.Add("Agent version is not specified");
                }

                // Validate intent values
                foreach (var intent in capability.SupportedIntents)
                {
                    if (!Enum.IsDefined(typeof(AgentIntent), intent))
                    {
                        result.Errors.Add($"Invalid intent: {intent}");
                    }
                }

                // Check for duplicate registration
                if (_capabilities.ContainsKey(capability.AgentId))
                {
                    result.Warnings.Add($"Agent {capability.AgentId} is already registered");
                }

                result.IsValid = !result.Errors.Any();
                result.ValidationSummary = result.IsValid 
                    ? "Agent capability validation passed" 
                    : $"Validation failed with {result.Errors.Count} errors";

                _logger.LogDebug("Validated agent capability for {AgentId}: {IsValid}", 
                    capability.AgentId, result.IsValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating agent capability for {AgentId}", capability.AgentId);
                result.Errors.Add($"Validation error: {ex.Message}");
                result.IsValid = false;
            }

            return await Task.FromResult(result);
        }

        private async Task UpdateAgentHealthAsync(string agentId)
        {
            try
            {
                var agent = await GetAgentAsync(agentId);
                var healthStatus = new AgentHealthInfo
                {
                    AgentId = agentId,
                    LastChecked = DateTime.UtcNow
                };

                if (agent != null)
                {
                    // Perform basic health check
                    var startTime = DateTime.UtcNow;
                    
                    // Simple health check - could be extended to call agent's health endpoint
                    var isHealthy = true; // Assume healthy if agent exists
                    
                    var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    
                    healthStatus.IsHealthy = isHealthy;
                    healthStatus.Status = isHealthy ? "Healthy" : "Unhealthy";
                    healthStatus.ResponseTimeMs = (long)responseTime;
                }
                else
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Status = "Agent Not Found";
                    healthStatus.ErrorMessage = "Agent instance not found in registry";
                }

                _healthStatuses.AddOrUpdate(agentId, healthStatus, (key, oldValue) => healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating health status for agent {AgentId}", agentId);
                
                var errorStatus = new AgentHealthInfo
                {
                    AgentId = agentId,
                    IsHealthy = false,
                    Status = "Health Check Failed",
                    ErrorMessage = ex.Message,
                    LastChecked = DateTime.UtcNow
                };
                
                _healthStatuses.AddOrUpdate(agentId, errorStatus, (key, oldValue) => errorStatus);
            }
        }

        private AgentCapability ConvertAgentCardToCapability(AgentCard agentCard)
        {
            var capability = new AgentCapability
            {
                AgentId = agentCard.AgentId,
                Name = agentCard.Name,
                Description = agentCard.Description,
                Version = agentCard.Version,
                IsActive = true,
                SupportedIntents = new List<AgentIntent>(),
                SupportedIndustries = agentCard.SupportedIndustries ?? new List<string>()
            };

            // Convert capabilities to intents
            foreach (var cap in agentCard.Capabilities)
            {
                if (Enum.TryParse<AgentIntent>(cap.Name, out var intent))
                {
                    capability.SupportedIntents.Add(intent);
                }
            }

            return capability;
        }
    }

    /// <summary>
    /// A2A Agent Card model for JSON deserialization
    /// </summary>
    public class AgentCard
    {
        public string AgentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<AgentCardCapability> Capabilities { get; set; } = new();
        public List<string>? SupportedIndustries { get; set; }
        public AgentCardMetadata? Metadata { get; set; }
    }

    public class AgentCardCapability
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> InputTypes { get; set; } = new();
        public List<string> OutputTypes { get; set; } = new();
    }

    public class AgentCardMetadata
    {
        public string? Author { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string>? Tags { get; set; }
    }
}
