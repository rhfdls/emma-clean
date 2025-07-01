using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Agents;

/// <summary>
/// Resource Agent implementation that provides intelligent resource management and recommendations.
/// </summary>
public class ResourceAgent : AgentBase, IResourceAgent
{
    private const string AgentIdValue = "resource-agent";
    private const string DisplayNameValue = "Resource Agent";
    private const string DescriptionValue = "Manages and recommends resources based on context and requirements";
    private const string VersionValue = "1.0.0";
    
    private readonly ILogger<ResourceAgent> _logger;
    private readonly IResourceRepository _resourceRepository;
    private readonly IEnumProvider _enumProvider;
    private readonly IRecommendationEngine _recommendationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAgent"/> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="resourceRepository">The resource repository</param>
    /// <param name="enumProvider">The enum provider for resource types</param>
    /// <param name="recommendationEngine">The recommendation engine</param>
    public ResourceAgent(
        ILogger<ResourceAgent> logger,
        IResourceRepository resourceRepository,
        IEnumProvider enumProvider,
        IRecommendationEngine recommendationEngine)
        : base(AgentIdValue, DisplayNameValue, DescriptionValue, VersionValue, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _enumProvider = enumProvider ?? throw new ArgumentNullException(nameof(enumProvider));
        _recommendationEngine = recommendationEngine ?? throw new ArgumentNullException(nameof(recommendationEngine));
        
        // Initialize capability with resource management information
        Capability = new AgentCapability
        {
            AgentId = AgentIdValue,
            AgentName = DisplayNameValue,
            Description = DescriptionValue,
            Version = VersionValue,
            SupportedTasks = new List<string>
            {
                "recommend-resources",
                "find-resources",
                "get-resource-details",
                "suggest-resource-tags"
            },
            RequiredPermissions = new List<string>
            {
                "resources:read",
                "resources:recommend"
            },
            Configuration = new Dictionary<string, object>()
        };
    }
    
    /// <inheritdoc />
    public override async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
            
        _logger.LogInformation("[{TraceId}] Processing resource agent request: {RequestType}", 
            traceId ?? "N/A", request.RequestType);
            
        try
        {
            // Route the request to the appropriate handler based on request type
            return request.RequestType?.ToLowerInvariant() switch
            {
                "recommend-resources" => await HandleRecommendResourcesAsync(request, traceId),
                "find-resources" => await HandleFindResourcesAsync(request, traceId),
                "get-resource-details" => await HandleGetResourceDetailsAsync(request, traceId),
                "suggest-resource-tags" => await HandleSuggestResourceTagsAsync(request, traceId),
                _ => new AgentResponse
                {
                    Success = false,
                    Message = $"Unsupported request type: {request.RequestType}",
                    StatusCode = 400
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error processing resource request: {ErrorMessage}", 
                traceId ?? "N/A", ex.Message);
                
            return new AgentResponse
            {
                Success = false,
                Message = $"Error processing request: {ex.Message}",
                StatusCode = 500,
                ErrorDetails = ex.ToString()
            };
        }
    }
    
    /// <inheritdoc />
    public async Task<AgentResponse> RecommendResourcesAsync(
        Guid organizationId, 
        Dictionary<string, object> resourceCriteria, 
        int maxResults = 10, 
        string? traceId = null, 
        Dictionary<string, object>? userOverrides = null)
    {
        _logger.LogInformation("[{TraceId}] Recommending resources for organization {OrganizationId}", 
            traceId ?? "N/A", organizationId);
            
        try
        {
            // Validate input parameters
            if (resourceCriteria == null || resourceCriteria.Count == 0)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = "Resource criteria cannot be empty",
                    StatusCode = 400
                };
            }
            
            // Apply any user overrides to the criteria
            var effectiveCriteria = ApplyUserOverrides(resourceCriteria, userOverrides);
            
            // Get resources matching the criteria
            var resources = await _resourceRepository.FindResourcesAsync(organizationId, effectiveCriteria, maxResults);
            
            // If no direct matches, try to find similar resources
            if (!resources.Any() && effectiveCriteria.Count > 0)
            {
                var similarCriteria = CreateRelaxedCriteria(effectiveCriteria);
                resources = await _resourceRepository.FindResourcesAsync(organizationId, similarCriteria, maxResults);
                
                if (resources.Any())
                {
                    _logger.LogInformation("[{TraceId}] Found {Count} similar resources after relaxing criteria", 
                        traceId ?? "N/A", resources.Count);
                }
            }
            
            // Generate recommendations based on the resources found
            var recommendations = await _recommendationEngine.GenerateRecommendationsAsync(
                resources, 
                effectiveCriteria, 
                maxResults);
                
            return new AgentResponse
            {
                Success = true,
                Data = new 
                {
                    Recommendations = recommendations,
                    TotalResources = resources.Count,
                    CriteriaUsed = effectiveCriteria,
                    Timestamp = DateTime.UtcNow
                },
                StatusCode = 200
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] Error recommending resources: {ErrorMessage}", 
                traceId ?? "N/A", ex.Message);
                
            return new AgentResponse
            {
                Success = false,
                Message = $"Error recommending resources: {ex.Message}",
                StatusCode = 500,
                ErrorDetails = ex.ToString()
            };
        }
    }
    
    private async Task<AgentResponse> HandleRecommendResourcesAsync(AgentRequest request, string? traceId)
    {
        if (!request.Parameters.TryGetValue("organizationId", out var orgIdObj) || 
            !Guid.TryParse(orgIdObj?.ToString(), out var organizationId))
        {
            return new AgentResponse
            {
                Success = false,
                Message = "Missing or invalid organizationId parameter",
                StatusCode = 400
            };
        }
        
        if (!request.Parameters.TryGetValue("criteria", out var criteriaObj) || 
            !(criteriaObj is Dictionary<string, object> criteria))
        {
            return new AgentResponse
            {
                Success = false,
                Message = "Missing or invalid criteria parameter",
                StatusCode = 400
            };
        }
        
        var maxResults = 10;
        if (request.Parameters.TryGetValue("maxResults", out var maxResultsObj) && 
            int.TryParse(maxResultsObj?.ToString(), out var maxRes))
        {
            maxResults = Math.Clamp(maxRes, 1, 100);
        }
        
        Dictionary<string, object>? userOverrides = null;
        if (request.Parameters.TryGetValue("userOverrides", out var overridesObj) && 
            overridesObj is Dictionary<string, object> overrides)
        {
            userOverrides = overrides;
        }
        
        return await RecommendResourcesAsync(
            organizationId, 
            criteria, 
            maxResults, 
            traceId, 
            userOverrides);
    }
    
    private Task<AgentResponse> HandleFindResourcesAsync(AgentRequest request, string? traceId)
    {
        // Implementation for finding resources by query
        return Task.FromResult(new AgentResponse
        {
            Success = true,
            Message = "Resource search not yet implemented",
            StatusCode = 501 // Not Implemented
        });
    }
    
    private Task<AgentResponse> HandleGetResourceDetailsAsync(AgentRequest request, string? traceId)
    {
        // Implementation for getting resource details
        return Task.FromResult(new AgentResponse
        {
            Success = true,
            Message = "Resource details retrieval not yet implemented",
            StatusCode = 501 // Not Implemented
        });
    }
    
    private Task<AgentResponse> HandleSuggestResourceTagsAsync(AgentRequest request, string? traceId)
    {
        // Implementation for suggesting resource tags
        return Task.FromResult(new AgentResponse
        {
            Success = true,
            Message = "Resource tag suggestion not yet implemented",
            StatusCode = 501 // Not Implemented
        });
    }
    
    private Dictionary<string, object> ApplyUserOverrides(
        Dictionary<string, object> criteria, 
        Dictionary<string, object>? userOverrides)
    {
        if (userOverrides == null || userOverrides.Count == 0)
            return criteria;
            
        // Create a new dictionary to avoid modifying the original
        var result = new Dictionary<string, object>(criteria);
        
        // Apply overrides
        foreach (var (key, value) in userOverrides)
        {
            result[key] = value;
        }
        
        return result;
    }
    
    private Dictionary<string, object> CreateRelaxedCriteria(Dictionary<string, object> originalCriteria)
    {
        // Create a relaxed version of the criteria by removing or relaxing constraints
        // This is a simplified implementation - in a real system, this would be more sophisticated
        var relaxed = new Dictionary<string, object>();
        
        foreach (var (key, value) in originalCriteria)
        {
            // Skip certain fields that shouldn't be relaxed
            if (key.Equals("organizationId", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("isActive", StringComparison.OrdinalIgnoreCase))
            {
                relaxed[key] = value;
                continue;
            }
            
            // For numeric ranges, we might relax the range
            if (value is int intValue)
            {
                // Example: If we have a numeric value, we might expand the range
                relaxed[key] = new { Min = Math.Max(0, intValue - 1), Max = intValue + 1 };
            }
            // For strings, we might do partial matching
            else if (value is string strValue && !string.IsNullOrWhiteSpace(strValue) && strValue.Length > 3)
            {
                relaxed[key] = $"%{strValue}%"; // For SQL LIKE queries
            }
            // For other types, just include as-is
            else
            {
                relaxed[key] = value;
            }
        }
        
        return relaxed;
    }
    
    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        
        // Initialize any resource-specific data or caches
        try
        {
            // Preload resource types or other frequently used data
            var resourceTypes = _enumProvider.GetValues("ResourceType");
            _logger.LogInformation("Initialized with {Count} resource types", resourceTypes?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing ResourceAgent");
            throw;
        }
    }
}
