using Emma.Core.Interfaces;
using Emma.Core.Extensions;
using Emma.Core.Models;
using Emma.Data.Models;
using Emma.Data.Enums;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services
{
    /// <summary>
    /// Resource Agent that provides intelligent resource management and recommendations
    /// Implements the layered agent pattern with ResourceService as the data layer
    /// </summary>
    public class ResourceAgent : IResourceAgent
    {
        private readonly IResourceService _resourceService;
        private readonly ITenantContextService _tenantContext;
        private readonly ILogger<ResourceAgent> _logger;

        public ResourceAgent(
            IResourceService resourceService,
            ITenantContextService tenantContext,
            ILogger<ResourceAgent> logger)
        {
            _resourceService = resourceService;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Processing Resource request: Intent={Intent}, TraceId={TraceId}", 
                    request.Intent, traceId);

                // Handle different types of resource requests
                return request.Intent switch
                {
                    AgentIntent.ResourceManagement => await HandleResourceManagementAsync(request, traceId),
                    AgentIntent.ServiceProviderRecommendation => await HandleServiceProviderRecommendationAsync(request, traceId),
                    _ => new AgentResponse
                    {
                        Success = false,
                        Message = $"Resource Agent cannot handle intent: {request.Intent}",
                        Data = new Dictionary<string, object>(),
                        RequestId = request.Id,
                        AgentId = "ResourceAgent",
                        Timestamp = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Resource request: {TraceId}", traceId);
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Resource processing failed: {ex.Message}",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<AgentCapability> GetCapabilityAsync()
        {
            try
            {
                var industryProfile = await _tenantContext.GetIndustryProfileAsync();
                
                return new AgentCapability
                {
                    AgentType = "ResourceAgent",
                    DisplayName = "Resource Management Agent",
                    Description = "Provides intelligent resource discovery, recommendations, assignment management, and performance tracking",
                    SupportedTasks = new List<string>
                    {
                        "find_resources",
                        "recommend_providers",
                        "match_criteria",
                        "rank_resources"
                    },
                    RequiredIndustries = new List<string> { "RealEstate", "Financial", "Legal" }, // Industries that commonly use service providers
                    IsAvailable = true,
                    Version = "1.0.0",
                    Configuration = new Dictionary<string, object>
                    {
                        ["SupportsResourceDiscovery"] = true,
                        ["SupportsPerformanceTracking"] = true,
                        ["SupportsComplianceMonitoring"] = true,
                        ["SupportsRatingManagement"] = true,
                        ["MaxResourcesPerRecommendation"] = 10,
                        ["SupportedSpecialties"] = GetSupportedSpecialties(industryProfile.IndustryCode)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Resource Agent capability");
                
                // Return basic capability even on error
                return new AgentCapability
                {
                    AgentType = "ResourceAgent",
                    DisplayName = "Resource Management Agent",
                    Description = "Resource management and recommendations agent",
                    SupportedTasks = new List<string> { "Resource Management" },
                    RequiredIndustries = new List<string>(),
                    IsAvailable = false
                };
            }
        }

        public async Task<AgentResponse> RecommendResourcesAsync(Guid organizationId, Dictionary<string, object> resourceCriteria, int maxResults = 10, string? traceId = null)
        {
            traceId ??= Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Recommending resources for Organization={OrganizationId}, TraceId={TraceId}", 
                    organizationId, traceId);

                // Extract criteria
                var specialty = resourceCriteria.GetValueOrDefault("specialty")?.ToString();
                var serviceArea = resourceCriteria.GetValueOrDefault("serviceArea")?.ToString();
                var minRating = resourceCriteria.GetValueOrDefault("minRating") as decimal?;
                var preferredOnly = resourceCriteria.GetValueOrDefault("preferredOnly") as bool?;

                if (string.IsNullOrEmpty(specialty))
                {
                    return new AgentResponse
                    {
                        Success = false,
                        Message = "Specialty is required for resource recommendations",
                        RequestId = Guid.NewGuid().ToString(),
                        AgentId = "ResourceAgent",
                        Timestamp = DateTime.UtcNow
                    };
                }

                // Get top performing resources first
                var topResources = await _resourceService.GetTopPerformingResourcesAsync(specialty, maxResults);
                
                // Get additional resources based on criteria
                var additionalResources = await _resourceService.DiscoverResourcesAsync(
                    specialty, serviceArea, minRating, preferredOnly);

                // Combine and deduplicate
                var allResources = topResources
                    .Concat(additionalResources)
                    .GroupBy(r => r.Id)
                    .Select(g => g.First())
                    .ToList();

                var recommendedResources = allResources.Select(resource => new
                {
                    ContactId = resource.Id,
                    Name = $"{resource.FirstName} {resource.LastName}",
                    Email = resource.Email(),
                    Phone = resource.Phone(),
                    CompanyName = resource.CompanyName,
                    Specialties = resource.Specialties,
                    ServiceAreas = resource.ServiceAreas,
                    Rating = resource.Rating,
                    PerformanceScore = resource.GetResourcePerformanceScore()
                })
                .OrderByDescending(r => r.PerformanceScore)
                .Take(10)
                .ToList();

                _logger.LogInformation("Found {Count} recommended resources for organization {OrganizationId}",
                    recommendedResources.Count, organizationId);

                return new AgentResponse
                {
                    Success = true,
                    Message = $"Found {recommendedResources.Count} recommended resources",
                    Data = new Dictionary<string, object>
                    {
                        ["RecommendedResources"] = recommendedResources.Select(r => new
                        {
                            r.ContactId,
                            r.Name,
                            r.Email,
                            r.Phone,
                            r.CompanyName,
                            r.Specialties,
                            r.ServiceAreas,
                            r.Rating,
                            r.PerformanceScore
                        }).ToList(),
                        ["Specialty"] = specialty,
                        ["ServiceArea"] = serviceArea ?? "All Areas",
                        ["OrganizationId"] = organizationId,
                        ["TotalFound"] = recommendedResources.Count
                    },
                    RequestId = Guid.NewGuid().ToString(),
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recommending resources, TraceId: {TraceId}", traceId);
                
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Resource recommendation failed: {ex.Message}",
                    RequestId = Guid.NewGuid().ToString(),
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        // Overload for backward compatibility
        public async Task<AgentResponse> RecommendResourcesAsync(string specialty, string? serviceArea = null, int maxResults = 10)
        {
            var criteria = new Dictionary<string, object> { ["specialty"] = specialty };
            if (!string.IsNullOrEmpty(serviceArea))
                criteria["serviceArea"] = serviceArea;
                
            return await RecommendResourcesAsync(Guid.Empty, criteria, maxResults);
        }

        private async Task<AgentResponse> HandleResourceManagementAsync(AgentRequest request, string traceId)
        {
            var action = request.Context.TryGetValue("action", out var actionValue) ? 
                actionValue.ToString()!.ToLowerInvariant() : "discover";

            return action switch
            {
                "discover" => await HandleResourceDiscoveryAsync(request, traceId),
                "assign" => await HandleResourceAssignmentAsync(request, traceId),
                "metrics" => await HandleResourceMetricsAsync(request, traceId),
                "rating" => await HandleResourceRatingAsync(request, traceId),
                _ => new AgentResponse
                {
                    Success = false,
                    Message = $"Unknown resource management action: {action}",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                }
            };
        }

        private async Task<AgentResponse> HandleServiceProviderRecommendationAsync(AgentRequest request, string traceId)
        {
            var organizationId = request.Context.TryGetValue("organizationId", out var orgId) && Guid.TryParse(orgId.ToString(), out var orgGuid) ? orgGuid : Guid.Empty;
            var resourceCriteria = request.Context.TryGetValue("resourceCriteria", out var criteria) ? (Dictionary<string, object>)criteria : new Dictionary<string, object>();
            var maxResults = request.Context.TryGetValue("maxResults", out var max) && int.TryParse(max.ToString(), out var maxInt) ? maxInt : 10;

            if (organizationId == Guid.Empty || resourceCriteria == null)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = "OrganizationId and resourceCriteria are required for service provider recommendations",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }

            return await RecommendResourcesAsync(organizationId, resourceCriteria, maxResults, traceId);
        }

        private async Task<AgentResponse> HandleResourceDiscoveryAsync(AgentRequest request, string traceId)
        {
            var specialty = request.Context.TryGetValue("specialty", out var spec) ? spec.ToString() : null;
            var serviceArea = request.Context.TryGetValue("serviceArea", out var area) ? area.ToString() : null;
            var minRating = request.Context.TryGetValue("minRating", out var rating) && decimal.TryParse(rating.ToString(), out var ratingDecimal) ? ratingDecimal : (decimal?)null;
            var isPreferred = request.Context.TryGetValue("isPreferred", out var pref) && bool.TryParse(pref.ToString(), out var prefBool) ? prefBool : (bool?)null;

            var resources = await _resourceService.DiscoverResourcesAsync(specialty, serviceArea, minRating, isPreferred);

            return new AgentResponse
            {
                Success = true,
                Message = $"Discovered {resources.Count} resources matching criteria",
                Data = new Dictionary<string, object>
                {
                    ["Resources"] = resources.Select(r => new
                    {
                        ResourceId = r.Id,
                        Name = $"{r.FirstName} {r.LastName}",
                        CompanyName = r.CompanyName,
                        Rating = r.Rating,
                        IsPreferred = r.IsPreferred,
                        Specialties = r.Specialties?.ToList() ?? new List<string>(),
                        ServiceAreas = r.ServiceAreas?.ToList() ?? new List<string>()
                    }).ToList(),
                    ["SearchCriteria"] = new
                    {
                        Specialty = specialty,
                        ServiceArea = serviceArea,
                        MinRating = minRating,
                        IsPreferred = isPreferred
                    }
                },
                RequestId = request.Id,
                AgentId = "ResourceAgent",
                Timestamp = DateTime.UtcNow
            };
        }

        private async Task<AgentResponse> HandleResourceAssignmentAsync(AgentRequest request, string traceId)
        {
            // Extract assignment parameters
            var clientContactId = request.Context.TryGetValue("clientContactId", out var clientId) && Guid.TryParse(clientId.ToString(), out var clientGuid) ? clientGuid : Guid.Empty;
            var serviceContactId = request.Context.TryGetValue("serviceContactId", out var serviceId) && Guid.TryParse(serviceId.ToString(), out var serviceGuid) ? serviceGuid : Guid.Empty;
            var assignedByAgentId = request.Context.TryGetValue("assignedByAgentId", out var agentId) && Guid.TryParse(agentId.ToString(), out var agentGuid) ? agentGuid : Guid.Empty;
            var organizationId = request.Context.TryGetValue("organizationId", out var orgId) && Guid.TryParse(orgId.ToString(), out var orgGuid) ? orgGuid : Guid.Empty;
            var purpose = request.Context.TryGetValue("purpose", out var purp) ? purp.ToString()! : "";

            if (clientContactId == Guid.Empty || serviceContactId == Guid.Empty || assignedByAgentId == Guid.Empty || organizationId == Guid.Empty || string.IsNullOrEmpty(purpose))
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = "ClientContactId, ServiceContactId, AssignedByAgentId, OrganizationId, and Purpose are required for resource assignment",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                var assignment = await _resourceService.AssignResourceAsync(
                    clientContactId, serviceContactId, assignedByAgentId, organizationId, purpose);

                return new AgentResponse
                {
                    Success = true,
                    Message = "Resource successfully assigned",
                    Data = new Dictionary<string, object>
                    {
                        ["AssignmentId"] = assignment.Id,
                        ["ClientContactId"] = assignment.ClientContactId,
                        ["ServiceContactId"] = assignment.ServiceContactId,
                        ["Purpose"] = assignment.Purpose,
                        ["Status"] = assignment.Status.ToString(),
                        ["CreatedAt"] = assignment.CreatedAt
                    },
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Resource assignment failed: {ex.Message}",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<AgentResponse> HandleResourceMetricsAsync(AgentRequest request, string traceId)
        {
            var resourceId = request.Context.TryGetValue("resourceId", out var resId) && Guid.TryParse(resId.ToString(), out var resGuid) ? resGuid : Guid.Empty;

            if (resourceId == Guid.Empty)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = "ResourceId is required for metrics retrieval",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                var metrics = await _resourceService.GetResourceMetricsAsync(resourceId);

                return new AgentResponse
                {
                    Success = true,
                    Message = "Resource metrics retrieved successfully",
                    Data = new Dictionary<string, object>
                    {
                        ["Metrics"] = metrics
                    },
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Failed to retrieve resource metrics: {ex.Message}",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private async Task<AgentResponse> HandleResourceRatingAsync(AgentRequest request, string traceId)
        {
            var resourceId = request.Context.TryGetValue("resourceId", out var resId) && Guid.TryParse(resId.ToString(), out var resGuid) ? resGuid : Guid.Empty;
            var rating = request.Context.TryGetValue("rating", out var rat) && decimal.TryParse(rat.ToString(), out var ratingDecimal) ? ratingDecimal : 0m;
            var feedback = request.Context.TryGetValue("feedback", out var fb) ? fb.ToString() : null;

            if (resourceId == Guid.Empty || rating <= 0)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = "ResourceId and valid rating are required for rating update",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }

            try
            {
                await _resourceService.UpdateResourceRatingAsync(resourceId, rating, feedback);

                return new AgentResponse
                {
                    Success = true,
                    Message = "Resource rating updated successfully",
                    Data = new Dictionary<string, object>
                    {
                        ["ResourceId"] = resourceId,
                        ["Rating"] = rating,
                        ["Feedback"] = feedback ?? ""
                    },
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new AgentResponse
                {
                    Success = false,
                    Message = $"Failed to update resource rating: {ex.Message}",
                    RequestId = request.Id,
                    AgentId = "ResourceAgent",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private List<string> GetSupportedSpecialties(string industryCode)
        {
            return industryCode switch
            {
                "RealEstate" => new List<string>
                {
                    "Home Inspector", "Mortgage Lender", "Real Estate Attorney", 
                    "Title Company", "Insurance Agent", "Contractor", "Appraiser"
                },
                "Financial" => new List<string>
                {
                    "Tax Advisor", "Investment Advisor", "Insurance Agent",
                    "Estate Planning Attorney", "Accountant", "Financial Planner"
                },
                "Legal" => new List<string>
                {
                    "Specialist Attorney", "Expert Witness", "Court Reporter",
                    "Private Investigator", "Process Server", "Paralegal"
                },
                _ => new List<string>
                {
                    "Consultant", "Specialist", "Service Provider", "Expert"
                }
            };
        }
    }
}
