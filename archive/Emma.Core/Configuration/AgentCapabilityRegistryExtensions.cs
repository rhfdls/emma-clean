using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Extension methods for registering agent capability services
    /// </summary>
    public static class AgentCapabilityRegistryExtensions
    {
        /// <summary>
        /// Adds YAML-based agent capability configuration to the service collection
        /// </summary>
        public static IServiceCollection AddYamlAgentCapabilities(
            this IServiceCollection services,
            string configPath = "agent_capabilities.yaml",
            bool validateSchema = true,
            bool throwOnFileNotFound = false)
        {
            if (string.IsNullOrEmpty(configPath))
                throw new ArgumentException("Configuration path cannot be null or empty", nameof(configPath));

            // Register the YAML source
            services.TryAddSingleton<IYamlAgentCapabilitySource>(sp =>
            {
                var fileProvider = sp.GetRequiredService<IFileProvider>();
                var logger = sp.GetRequiredService<ILogger<YamlAgentCapabilitySource>>();
                var options = Options.Create(new YamlAgentCapabilitySourceOptions
                {
                    FilePath = configPath,
                    ValidateSchema = validateSchema,
                    ThrowOnFileNotFound = throwOnFileNotFound
                });
                
                return new YamlAgentCapabilitySource(fileProvider, options, logger);
            });

            // Register the enhanced capability registry
            services.TryAddSingleton<IAgentCapabilityRegistry, YamlAgentCapabilityRegistry>();
            
            return services;
        }
        
        /// <summary>
        /// Adds programmatic agent capabilities to the registry
        /// </summary>
        public static IServiceCollection AddAgentCapability(
            this IServiceCollection services,
            string agentType,
            string capabilityName,
            bool isEnabled = true,
            string? description = null,
            Dictionary<string, object>? validationRules = null)
        {
            if (string.IsNullOrEmpty(agentType))
                throw new ArgumentException("Agent type is required", nameof(agentType));
                
            if (string.IsNullOrEmpty(capabilityName))
                throw new ArgumentException("Capability name is required", nameof(capabilityName));
            
            // Register the capability as a singleton so it can be injected into the registry
            services.Configure<AgentCapabilityRegistryOptions>(options =>
            {
                options.ProgrammaticCapabilities ??= new();
                if (!options.ProgrammaticCapabilities.ContainsKey(agentType))
                {
                    options.ProgrammaticCapabilities[agentType] = new();
                }
                
                options.ProgrammaticCapabilities[agentType].Add(new AgentCapabilityYamlConfig
                {
                    Name = capabilityName,
                    Description = description,
                    Enabled = isEnabled,
                    ValidationRules = validationRules
                });
            });
            
            return services;
        }
        
        /// <summary>
        /// Configures the agent capability registry with additional options
        /// </summary>
        public static IServiceCollection ConfigureAgentCapabilities(
            this IServiceCollection services,
            Action<AgentCapabilityRegistryOptions> configureOptions)
        {
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));
                
            return services.Configure(configureOptions);
        }
    }
}
