using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Feature flag definitions for Agent Factory and AI-First platform capabilities.
    /// Follows Microsoft Azure App Configuration best practices for safe feature rollout.
    /// All new capabilities are off by default to ensure zero production impact.
    /// </summary>
    public static class FeatureFlags
    {
        // Sprint 1: Critical Agent Factory Hooks
        public const string AGENT_REGISTRY_ENABLED = "AgentFactory:AgentRegistry:Enabled";
        public const string DYNAMIC_AGENT_ROUTING = "AgentFactory:DynamicRouting:Enabled";
        public const string UNIVERSAL_EXPLAINABILITY = "AgentFactory:Explainability:Enabled";
        public const string AGENT_LIFECYCLE_HOOKS = "AgentFactory:LifecycleHooks:Enabled";
        public const string API_VERSIONING = "AgentFactory:ApiVersioning:Enabled";
        public const string CONTEXT_PROVIDER_ABSTRACTION = "AgentFactory:ContextProvider:Enabled";

        // Sprint 2: Service Stubs & Monitoring
        public const string AGENT_METADATA_REGISTRY = "AgentFactory:MetadataRegistry:Enabled";
        public const string BLUEPRINT_SERVICE = "AgentFactory:BlueprintService:Enabled";
        public const string MONITORING_FRAMEWORK = "AgentFactory:Monitoring:Enabled";
        public const string SECURITY_VALIDATION_EXTENSIONS = "AgentFactory:SecurityValidation:Enabled";
        public const string APPROVAL_QUEUE = "AgentFactory:ApprovalQueue:Enabled";

        // Sprint 3: Validation Optimization
        public const string SCOPE_CLASSIFICATION = "AgentFactory:ScopeClassification:Enabled";
        public const string VALIDATION_METRICS = "AgentFactory:ValidationMetrics:Enabled";
        public const string PERFORMANCE_OPTIMIZATION = "AgentFactory:PerformanceOptimization:Enabled";

        // Sprint 4: Advanced Features
        public const string AGENT_COMPILER = "AgentFactory:AgentCompiler:Enabled";
        public const string HOT_RELOAD = "AgentFactory:HotReload:Enabled";
        public const string ADVANCED_MONITORING = "AgentFactory:AdvancedMonitoring:Enabled";
        public const string SECURITY_HARDENING = "AgentFactory:SecurityHardening:Enabled";

        // Experimental Features (Always off by default)
        public const string EXPERIMENTAL_FEATURES = "AgentFactory:Experimental:Enabled";
        public const string DEBUG_MODE = "AgentFactory:Debug:Enabled";
        public const string TELEMETRY_COLLECTION = "AgentFactory:Telemetry:Enabled";
    }

    /// <summary>
    /// Feature flag service interface for runtime feature evaluation.
    /// Follows Microsoft DI best practices - inject this interface, not the implementation.
    /// </summary>
    public interface IFeatureFlagService
    {
        /// <summary>
        /// Check if a feature flag is enabled.
        /// </summary>
        /// <param name="flagName">Feature flag name (use FeatureFlags constants)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if feature is enabled, false otherwise</returns>
        Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a feature flag is enabled with user context.
        /// Supports targeting filters for gradual rollout.
        /// </summary>
        /// <param name="flagName">Feature flag name</param>
        /// <param name="userId">User identifier for targeting</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if feature is enabled for the user, false otherwise</returns>
        Task<bool> IsEnabledForUserAsync(string flagName, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get feature flag value with type safety.
        /// </summary>
        /// <typeparam name="T">Expected value type</typeparam>
        /// <param name="flagName">Feature flag name</param>
        /// <param name="defaultValue">Default value if flag not found</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Feature flag value or default</returns>
        Task<T> GetValueAsync<T>(string flagName, T defaultValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all enabled feature flags for debugging and monitoring.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of enabled feature flags</returns>
        Task<IDictionary<string, bool>> GetEnabledFlagsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Configuration-based feature flag service implementation.
    /// Uses IConfiguration for local development and Azure App Configuration for production.
    /// Thread-safe and follows Microsoft best practices.
    /// </summary>
    public class FeatureFlagService : IFeatureFlagService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FeatureFlagService> _logger;

        public FeatureFlagService(IConfiguration configuration, ILogger<FeatureFlagService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(flagName))
            {
                _logger.LogWarning("Feature flag name is null or empty");
                return Task.FromResult(false);
            }

            try
            {
                var isEnabled = _configuration.GetValue<bool>(flagName, defaultValue: false);
                _logger.LogDebug("Feature flag {FlagName} is {Status}", flagName, isEnabled ? "enabled" : "disabled");
                return Task.FromResult(isEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feature flag {FlagName}", flagName);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        public Task<bool> IsEnabledForUserAsync(string flagName, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(flagName) || string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Feature flag name or user ID is null or empty");
                return Task.FromResult(false);
            }

            try
            {
                // For now, use simple configuration-based approach
                // TODO: Implement Azure App Configuration targeting filters in Sprint 2
                var isEnabled = _configuration.GetValue<bool>(flagName, defaultValue: false);
                
                // Check for user-specific override
                var userSpecificFlag = $"{flagName}:Users:{userId}";
                var userOverride = _configuration.GetValue<bool?>(userSpecificFlag);
                
                var finalResult = userOverride ?? isEnabled;
                
                _logger.LogDebug("Feature flag {FlagName} for user {UserId} is {Status}", 
                    flagName, userId, finalResult ? "enabled" : "disabled");
                
                return Task.FromResult(finalResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feature flag {FlagName} for user {UserId}", flagName, userId);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        public Task<T> GetValueAsync<T>(string flagName, T defaultValue, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(flagName))
            {
                _logger.LogWarning("Feature flag name is null or empty, returning default value");
                return Task.FromResult(defaultValue);
            }

            try
            {
                var value = _configuration.GetValue<T>(flagName, defaultValue);
                _logger.LogDebug("Feature flag {FlagName} value: {Value}", flagName, value);
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feature flag value {FlagName}, returning default", flagName);
                return Task.FromResult(defaultValue);
            }
        }

        /// <inheritdoc />
        public Task<IDictionary<string, bool>> GetEnabledFlagsAsync(CancellationToken cancellationToken = default)
        {
            var enabledFlags = new Dictionary<string, bool>();

            try
            {
                // Get all feature flags from FeatureFlags class
                var flagFields = typeof(FeatureFlags).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                foreach (var field in flagFields)
                {
                    if (field.FieldType == typeof(string) && field.GetValue(null) is string flagName)
                    {
                        var isEnabled = _configuration.GetValue<bool>(flagName, defaultValue: false);
                        enabledFlags[flagName] = isEnabled;
                    }
                }

                _logger.LogDebug("Retrieved {Count} feature flags", enabledFlags.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving enabled feature flags");
            }

            return Task.FromResult<IDictionary<string, bool>>(enabledFlags);
        }
    }

    /// <summary>
    /// Extension methods for registering feature flag services.
    /// Follows Microsoft DI registration patterns.
    /// </summary>
    public static class FeatureFlagServiceExtensions
    {
        /// <summary>
        /// Register feature flag services with dependency injection.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddFeatureFlags(this IServiceCollection services)
        {
            services.AddSingleton<IFeatureFlagService, FeatureFlagService>();
            return services;
        }

        /// <summary>
        /// Register feature flag services with Azure App Configuration.
        /// TODO: Implement in Sprint 2 when Azure integration is added.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="connectionString">Azure App Configuration connection string</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddAzureFeatureFlags(this IServiceCollection services, string connectionString)
        {
            // TODO: Implement Azure App Configuration integration
            // services.AddAzureAppConfiguration();
            // services.AddFeatureManagement();
            
            // For now, fall back to basic implementation
            return services.AddFeatureFlags();
        }
    }
}
