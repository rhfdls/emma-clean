using Emma.Core.Models;
using Emma.Models.Interfaces;
using Emma.Core.Industry;
using Emma.Models.Models;
using Emma.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Services;

/// <summary>
/// AI-powered agent for intelligent resource recommendations and management.
/// Resources are contacts with ServiceProvider or Agent relationship states.
/// </summary>
public class ResourceAgent : IResourceAgent
{
    private readonly IContactService _contactService;
    private readonly ITenantContextService _tenantContextService;
    private readonly IAIFoundryService _aiFoundryService;
    private readonly IPromptProvider _promptProvider;
    private readonly ILogger<ResourceAgent> _logger;

    public ResourceAgent(
        IContactService contactService,
        ITenantContextService tenantContextService,
        IAIFoundryService aiFoundryService,
        IPromptProvider promptProvider,
        ILogger<ResourceAgent> logger)
    {
        _contactService = contactService;
        _tenantContextService = tenantContextService;
        _aiFoundryService = aiFoundryService;
        _promptProvider = promptProvider;
        _logger = logger;
    }

    public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, string? traceId = null)
    {
        traceId ??= Guid.NewGuid().ToString();
        
        if (request == null)
        {
            _logger.LogWarning("ProcessRequestAsync: request is null. traceId={TraceId}", traceId);
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = "Request is null",
                TraceId = traceId,
                AgentId = "ResourceAgent"
            };
        }

        try
        {
            _logger.LogInformation("Processing ResourceAgent request, TraceId: {TraceId}", traceId);

            // Extract resource criteria from request
            var resourceCriteria = ExtractResourceCriteria(request);
            
            // Extract OrganizationId from context
            var organizationId = request.Context.TryGetValue("OrganizationId", out var orgIdObj) && orgIdObj is Guid orgId 
                ? orgId 
                : Guid.Empty;
                
            // Validate extracted organization ID
            if (organizationId == Guid.Empty)
            {
                _logger.LogWarning("OrganizationId not provided or invalid, TraceId: {TraceId}", traceId);
            }

            // Extract parameters from context
            var parameters = request.Context.TryGetValue("Parameters", out var paramsObj) && paramsObj is Dictionary<string, object> paramDict
                ? paramDict
                : new Dictionary<string, object>();
                
            var maxResults = parameters.ContainsKey("maxResults") 
                ? Convert.ToInt32(parameters["maxResults"]) 
                : 10;

            // Validate maxResults
            if (maxResults <= 0)
            {
                _logger.LogWarning("Invalid maxResults value: {MaxResults}, defaulting to 10, TraceId: {TraceId}", maxResults, traceId);
                maxResults = 10;
            }

            return await RecommendResourcesAsync(organizationId, resourceCriteria, maxResults, traceId);
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found. traceId={TraceId} agent={Agent}", traceId, "ResourceAgent");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = "Resource not found",
                TraceId = traceId,
                AgentId = "ResourceAgent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ProcessRequestAsync. traceId={TraceId} agent={Agent}", traceId, "ResourceAgent");
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = "Unexpected error occurred",
                TraceId = traceId,
                AgentId = "ResourceAgent"
            };
        }
    }

    /// <summary>
    /// Asynchronously retrieves the capabilities of the ResourceAgent.
    /// </summary>
    /// <returns>An AgentCapability object detailing the supported tasks and configuration of the ResourceAgent.</returns>
    public async Task<AgentCapability> GetCapabilityAsync()
    {
        /// <summary>
        /// Provides the capabilities of the ResourceAgent, including supported tasks and configuration parameters.
        /// </summary>
        /// <remarks>
        /// The ResourceAgent is responsible for recommending service provider contacts based on client needs, specialties, location, and performance history.
        /// </remarks>
        var capability = new AgentCapability
        {
            AgentId = "ResourceAgent",
            Name = "Resource Recommendation Agent",
            Description = "AI-powered agent that finds and recommends service provider contacts based on client needs, specialties, location, and performance history.",
            Version = "2.0.0",
            SupportedTasks = new[]
            {
                "recommend_resources",
                "find_specialists",
                "match_service_providers",
                "analyze_provider_performance"
            }.ToList(),
            Configuration = new Dictionary<string, object>
            {
                ["RequiredParameters"] = new[] { "organizationId" },
                ["OptionalParameters"] = new[] { "specialty", "serviceArea", "minRating", "maxResults", "clientLocation" }
            }
        };

        // Validate capability completeness
        if (string.IsNullOrWhiteSpace(capability.AgentId) || string.IsNullOrWhiteSpace(capability.Name))
        {
            _logger.LogWarning("Agent capability details are incomplete. AgentId or Name is missing.");
            return new AgentCapability
            {
                AgentId = capability.AgentId ?? "Unknown",
                Name = capability.Name ?? "Unknown",
                Description = capability.Description,
                Version = capability.Version,
                SupportedTasks = capability.SupportedTasks,
                Configuration = capability.Configuration
            };
        }

        if (!capability.SupportedTasks.Any())
        {
            _logger.LogWarning("No supported tasks defined for ResourceAgent.");
        }

        if (!capability.Configuration.ContainsKey("RequiredParameters") || !capability.Configuration.ContainsKey("OptionalParameters"))
        {
            _logger.LogWarning("Configuration parameters are not properly defined.");
        }

        return capability;
    }

    public async Task<AgentResponse> RecommendResourcesAsync(Guid organizationId, Dictionary<string, object> resourceCriteria, int maxResults = 10, string? traceId = null, Dictionary<string, object>? userOverrides = null)
    {
        traceId ??= Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Recommending resources, TraceId: {TraceId}", traceId);

            // Convert Guid to Dictionary if needed
            var organizationIdDict = ConvertGuidToDictionary(organizationId);
            // Merge with existing resourceCriteria
            foreach (var kvp in organizationIdDict)
            {
                resourceCriteria[kvp.Key] = kvp.Value;
            }
            
            // Get industry profile
            var industryProfile = await _tenantContextService.GetIndustryProfileAsync();

            if (industryProfile == null)
            {
                throw new InvalidOperationException("Industry profile is not configured for this tenant.");
            }

            // Use ResourceTypes and DefaultResourceCategories in recommendations
            var resourceTypes = industryProfile.ResourceTypes;
            var defaultCategories = industryProfile.DefaultResourceCategories;

            // Implement logic to use these properties in resource recommendation
            var serviceProviders = await FindServiceProviderContactsAsync(organizationId, resourceCriteria, maxResults * 2);
            
            if (!serviceProviders.Any())
            {
                _logger.LogWarning("No service provider contacts found for criteria, OrganizationId: {OrganizationId}, TraceId: {TraceId}", 
                    organizationId, traceId);
                
                return new AgentResponse
                {
                    Success = true,
                    Message = "No service providers found matching the specified criteria",
                    Data = new Dictionary<string, object>
                    {
                        ["ResourceRecommendations"] = new List<object>(),
                        ["SearchCriteria"] = resourceCriteria,
                        ["TotalFound"] = 0,
                        ["AnalysisMethod"] = "Contact-Based Search"
                    },
                    TraceId = traceId,
                    AgentId = "ResourceAgent"
                };
            }

            // Generate AI-powered recommendations
            try
            {
                var aiContext = BuildResourceRecommendationContext(resourceCriteria, serviceProviders, industryProfile, resourceTypes, defaultCategories);
                var recommendations = await GenerateAIResourceRecommendationsAsync(aiContext, maxResults, industryProfile, traceId);
                
                if (recommendations.Any())
                {
                    _logger.LogInformation("AI-powered resource recommendations generated: {Count} recommendations, TraceId: {TraceId}, Context: {Context}", 
                        recommendations.Count, traceId, aiContext);

                    return new AgentResponse
                    {
                        Success = true,
                        Message = $"Generated {recommendations.Count} AI-powered resource recommendations",
                        Data = new Dictionary<string, object>
                        {
                            ["ResourceRecommendations"] = recommendations,
                            ["SearchCriteria"] = resourceCriteria,
                            ["TotalFound"] = recommendations.Count,
                            ["AnalysisMethod"] = "AI-Powered",
                            ["IndustryContext"] = industryProfile.IndustryCode
                        },
                        TraceId = traceId,
                        AgentId = "ResourceAgent"
                    };
                }
            }
            catch (Exception aiEx)
            {
                _logger.LogWarning(aiEx, "AI recommendation failed, falling back to rule-based recommendations, TraceId: {TraceId}", traceId);
            }

            // Fallback to rule-based recommendations
            var fallbackRecommendations = GenerateRuleBasedRecommendations(serviceProviders, resourceCriteria, maxResults, resourceTypes, defaultCategories);
            
            _logger.LogInformation("Rule-based resource recommendations generated: {Count} recommendations, TraceId: {TraceId}", 
                fallbackRecommendations.Count, traceId);

            return new AgentResponse
            {
                Success = true,
                Message = $"Generated {fallbackRecommendations.Count} rule-based resource recommendations",
                Data = new Dictionary<string, object>
                {
                    ["ResourceRecommendations"] = fallbackRecommendations,
                    ["SearchCriteria"] = resourceCriteria,
                    ["TotalFound"] = fallbackRecommendations.Count,
                    ["AnalysisMethod"] = "Rule-Based Fallback"
                },
                TraceId = traceId,
                AgentId = "ResourceAgent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RecommendResourcesAsync, TraceId: {TraceId}, Exception: {Exception}", traceId, ex);
            return new AgentResponse
            {
                Success = false,
                ErrorMessage = $"Failed to generate resource recommendations: {ex.Message}",
                TraceId = traceId,
                AgentId = "ResourceAgent"
            };
        }
    }

    private async Task<List<Contact>> FindServiceProviderContactsAsync(Guid organizationId, Dictionary<string, object> criteria, int maxResults)
    {
        try
        {
            // Convert Guid to Dictionary if needed
            var organizationIdDict = ConvertGuidToDictionary(organizationId);
            // Merge with existing criteria
            foreach (var kvp in organizationIdDict)
            {
                criteria[kvp.Key] = kvp.Value;
            }
            
            // Get all service provider contacts (ServiceProvider and Agent relationship states)
            var serviceProviders = await _contactService.GetContactsByRelationshipStateAsync(
                organizationId, 
                new[] { RelationshipState.ServiceProvider, RelationshipState.Agent });

            // Apply filtering based on criteria
            var filteredProviders = serviceProviders.AsQueryable();

            // Filter by specialty
            if (criteria.TryGetValue("specialty", out var specialtyObj) && specialtyObj?.ToString() is string specialty && !string.IsNullOrEmpty(specialty))
            {
                filteredProviders = filteredProviders.Where(c => 
                    c.Specialties.Any(s => s.Equals(specialty, StringComparison.OrdinalIgnoreCase)));
            }

            // Filter by service area
            if (criteria.TryGetValue("serviceArea", out var serviceAreaObj) && serviceAreaObj?.ToString() is string serviceArea && !string.IsNullOrEmpty(serviceArea))
            {
                filteredProviders = filteredProviders.Where(c => 
                    c.ServiceAreas.Any(sa => sa.Equals(serviceArea, StringComparison.OrdinalIgnoreCase)));
            }

            // Filter by minimum rating
            if (criteria.TryGetValue("minRating", out var minRatingObj) && decimal.TryParse(minRatingObj?.ToString(), out var minRating))
            {
                filteredProviders = filteredProviders.Where(c => c.Rating >= minRating);
            }

            // Filter by preferred status
            if (criteria.TryGetValue("preferredOnly", out var preferredObj) && bool.TryParse(preferredObj?.ToString(), out var preferredOnly) && preferredOnly)
            {
                filteredProviders = filteredProviders.Where(c => c.IsPreferred);
            }

            // Order by rating (descending), then by preferred status, then by review count
            return filteredProviders
                .OrderByDescending(c => c.IsPreferred)
                .ThenByDescending(c => c.Rating ?? 0)
                .ThenByDescending(c => c.ReviewCount)
                .Take(maxResults)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding service provider contacts. OrganizationId={OrganizationId}", organizationId);
            return new List<Contact>();
        }
    }

    private Dictionary<string, object> ConvertGuidToDictionary(Guid guid)
    {
        return new Dictionary<string, object>
        {
            { "GuidValue", guid }
        };
    }

    private object BuildResourceRecommendationContext(Dictionary<string, object> criteria, List<Contact> serviceProviders, IIndustryProfile industryProfile, List<string> resourceTypes, List<string> defaultCategories)
    {
        return new
        {
            RequestCriteria = new
            {
                Specialty = criteria.GetValueOrDefault("specialty", "")?.ToString(),
                ServiceArea = criteria.GetValueOrDefault("serviceArea", "")?.ToString(),
                MinRating = criteria.GetValueOrDefault("minRating"),
                PreferredOnly = criteria.GetValueOrDefault("preferredOnly"),
                ClientLocation = criteria.GetValueOrDefault("clientLocation", "")?.ToString(),
                UrgencyLevel = criteria.GetValueOrDefault("urgencyLevel", "normal")?.ToString()
            },
            AvailableServiceProviders = serviceProviders.Select(sp => new
            {
                Id = sp.Id,
                Name = $"{sp.FirstName} {sp.LastName}".Trim(),
                CompanyName = sp.CompanyName,
                PrimaryEmail = sp.Emails.FirstOrDefault(e => e.Type == "primary")?.Address ?? sp.Emails.FirstOrDefault()?.Address,
                PrimaryPhone = sp.Phones.FirstOrDefault(p => p.Type == "mobile")?.Number ?? sp.Phones.FirstOrDefault()?.Number,
                Specialties = sp.Specialties,
                ServiceAreas = sp.ServiceAreas,
                Rating = sp.Rating,
                ReviewCount = sp.ReviewCount,
                IsPreferred = sp.IsPreferred,
                LicenseNumber = sp.LicenseNumber,
                Website = sp.Website,
                RelationshipState = sp.RelationshipState.ToString(),
                Tags = sp.Tags
            }).ToList(),
            IndustryContext = new
            {
                IndustryCode = industryProfile.IndustryCode,
                DisplayName = industryProfile.DisplayName,
                ResourceTypes = resourceTypes,
                DefaultResourceCategories = defaultCategories
            },
            RecommendationContext = new
            {
                PriorityFactors = new[] { "expertise_match", "rating", "availability", "location_proximity", "cost_effectiveness", "past_performance" },
                MaxRecommendations = 10,
                IncludeAlternatives = true,
                RequireExplanation = true
            }
        };
    }

    private async Task<List<ResourceRecommendationResult>> GenerateAIResourceRecommendationsAsync(object aiContext, int maxResults, IIndustryProfile industryProfile, string traceId)
    {
        try
        {
            // Ensure AI service is properly configured
            if (_aiFoundryService == null)
            {
                throw new InvalidOperationException("AI service is not configured.");
            }

            // Validate context
            if (aiContext == null)
            {
                throw new ArgumentNullException(nameof(aiContext));
            }
            
            // Get system prompt for resource recommendations
            var systemPrompt = await _promptProvider.GetSystemPromptAsync("ResourceRecommendation", industryProfile);
            
            // Build user prompt with context
            var contextJson = JsonSerializer.Serialize(aiContext, new JsonSerializerOptions { WriteIndented = true });
            var userPrompt = $"Based on the following context, recommend the best service provider contacts for this request:\n\n{contextJson}\n\nProvide recommendations as a JSON array with the following structure:\n[{{\n  \"contactId\": \"guid\",\n  \"recommendationReason\": \"explanation\",\n  \"matchScore\": 0.95,\n  \"strengths\": [\"strength1\", \"strength2\"],\n  \"considerations\": [\"consideration1\"]\n}}]";

            _logger.LogInformation("Invoking AI for resource recommendations, TraceId: {TraceId}", traceId);

            // Call AI service
            var aiResponse = await _aiFoundryService.ProcessAgentRequestAsync(systemPrompt, userPrompt, traceId);

            _logger.LogInformation("AI response received for resource recommendations, TraceId: {TraceId}", traceId);

            // Parse AI response
            return ParseAIResourceRecommendations(aiResponse, traceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI resource recommendations, TraceId: {TraceId}", traceId);
            throw;
        }
    }

    private List<ResourceRecommendationResult> ParseAIResourceRecommendations(string aiResponse, string traceId)
    {
        try
        {
            // Clean and parse JSON response
            var cleanedResponse = aiResponse.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }

            // Parse JSON
            var recommendations = JsonSerializer.Deserialize<List<ResourceRecommendationResult>>(cleanedResponse);
            if (recommendations == null)
            {
                throw new JsonException("Failed to parse AI recommendations.");
            }

            _logger.LogInformation("Parsed {Count} AI recommendations, TraceId: {TraceId}", recommendations.Count, traceId);
            return recommendations;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON parsing error for AI recommendations, TraceId: {TraceId}", traceId);
            return new List<ResourceRecommendationResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing AI recommendations, TraceId: {TraceId}", traceId);
            return new List<ResourceRecommendationResult>();
        }
    }

    private List<ResourceRecommendationResult> GenerateRuleBasedRecommendations(List<Contact> serviceProviders, Dictionary<string, object> criteria, int maxResults, List<string> resourceTypes, List<string> defaultCategories)
    {
        var recommendations = new List<ResourceRecommendationResult>();

        foreach (var provider in serviceProviders.Take(maxResults))
        {
            var matchScore = CalculateMatchScore(provider, criteria);
            var strengths = IdentifyProviderStrengths(provider, resourceTypes, defaultCategories);
            var considerations = IdentifyProviderConsiderations(provider);

            recommendations.Add(new ResourceRecommendationResult
            {
                ContactId = provider.Id,
                RecommendationReason = $"Recommended based on {(provider.IsPreferred ? "preferred status, " : "")}" +
                                     $"rating of {provider.Rating:F1}/5.0 ({provider.ReviewCount} reviews)" +
                                     (provider.Specialties.Any() ? $", specializing in {string.Join(", ", provider.Specialties.Take(2))}" : ""),
                MatchScore = matchScore,
                Strengths = strengths,
                Considerations = considerations
            });
        }

        return recommendations;
    }

    private decimal CalculateMatchScore(Contact provider, Dictionary<string, object> criteria)
    {
        decimal score = 0.5m; // Base score

        // Rating boost
        if (provider.Rating.HasValue)
        {
            score += (provider.Rating.Value - 3) * 0.1m; // +/- 0.2 for rating above/below 3
        }

        // Preferred provider boost
        if (provider.IsPreferred)
        {
            score += 0.15m;
        }

        // Review count boost (more reviews = more reliable)
        if (provider.ReviewCount > 10)
        {
            score += 0.1m;
        }

        // Specialty match boost
        if (criteria.TryGetValue("specialty", out var specialtyObj) && specialtyObj?.ToString() is string specialty)
        {
            if (provider.Specialties.Any(s => s.Equals(specialty, StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.2m;
            }
        }

        // Service area match boost
        if (criteria.TryGetValue("serviceArea", out var serviceAreaObj) && serviceAreaObj?.ToString() is string serviceArea)
        {
            if (provider.ServiceAreas.Any(sa => sa.Equals(serviceArea, StringComparison.OrdinalIgnoreCase)))
            {
                score += 0.15m;
            }
        }

        return Math.Min(1.0m, Math.Max(0.0m, score)); // Clamp between 0 and 1
    }

    private List<string> IdentifyProviderStrengths(Contact provider, List<string> resourceTypes, List<string> defaultCategories)
    {
        var strengths = new List<string>();

        if (provider.IsPreferred)
            strengths.Add("Preferred service provider");

        if (provider.Rating >= 4.5m)
            strengths.Add("Excellent rating (4.5+ stars)");
        else if (provider.Rating >= 4.0m)
            strengths.Add("High rating (4.0+ stars)");

        if (provider.ReviewCount > 20)
            strengths.Add("Extensive client feedback");

        if (provider.Specialties.Count > 3)
            strengths.Add("Multiple specialties");

        if (provider.ServiceAreas.Count > 2)
            strengths.Add("Wide service coverage");

        if (!string.IsNullOrEmpty(provider.LicenseNumber))
            strengths.Add("Licensed professional");

        if (resourceTypes.Any(rt => provider.Specialties.Any(s => s.Equals(rt, StringComparison.OrdinalIgnoreCase))))
            strengths.Add("Matches industry-specific resource type");

        if (defaultCategories.Any(dc => provider.ServiceAreas.Any(sa => sa.Equals(dc, StringComparison.OrdinalIgnoreCase))))
            strengths.Add("Matches industry-specific default category");

        return strengths;
    }

    private List<string> IdentifyProviderConsiderations(Contact provider)
    {
        var considerations = new List<string>();

        if (provider.Rating < 3.5m)
            considerations.Add("Below average rating");

        if (provider.ReviewCount < 5)
            considerations.Add("Limited client feedback");

        if (!provider.Specialties.Any())
            considerations.Add("No specified specialties");

        if (!provider.ServiceAreas.Any())
            considerations.Add("Service area not specified");

        return considerations;
    }

    private Dictionary<string, object> ExtractResourceCriteria(AgentRequest request)
    {
        var criteria = new Dictionary<string, object>();

        if (request.Context.TryGetValue("Parameters", out var paramsObj) && paramsObj is Dictionary<string, object> paramDict)
        {
            foreach (var param in paramDict)
            {
                criteria[param.Key] = param.Value;
            }
        }

        // Extract from message content if available
        if (request.Context.TryGetValue("Message", out var messageObj) && messageObj?.ToString() is string message)
        {
            // Simple keyword extraction - could be enhanced with NLP
            var messageLower = message.ToLowerInvariant();
            
            if (messageLower.Contains("lender") || messageLower.Contains("mortgage"))
                criteria.TryAdd("specialty", "Mortgage Lending");
            if (messageLower.Contains("inspector") || messageLower.Contains("inspection"))
                criteria.TryAdd("specialty", "Property Inspection");
            if (messageLower.Contains("contractor") || messageLower.Contains("repair"))
                criteria.TryAdd("specialty", "General Contracting");
            if (messageLower.Contains("preferred"))
                criteria.TryAdd("preferredOnly", true);
        }

        return criteria;
    }

    public class ResourceRecommendationResult
    {
        public Guid ContactId { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
        public decimal MatchScore { get; set; }
        public List<string> Strengths { get; set; } = new();
        public List<string> Considerations { get; set; } = new();
    }
}
