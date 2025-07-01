using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Extension methods for setting up YAML capability services in an <see cref="IServiceCollection"/>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds YAML-based agent capability services to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configure">A delegate to configure the <see cref="YamlAgentCapabilitySourceOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddYamlAgentCapabilities(
            this IServiceCollection services,
            Action<YamlAgentCapabilitySourceOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            // Register the options
            if (configure != null)
            {
                services.Configure(configure);
            }
            
            // Register the YAML capability source as a singleton
            services.TryAddSingleton<IYamlAgentCapabilitySource>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<YamlAgentCapabilitySourceOptions>>();
                var logger = serviceProvider.GetRequiredService<ILogger<YamlHotReloadCapabilitySource>>();
                
                // Use the configured file provider or fall back to PhysicalFileProvider
                var fileProvider = serviceProvider.GetService<IFileProvider>() ?? 
                    new PhysicalFileProvider(Directory.GetCurrentDirectory());
                
                return new YamlHotReloadCapabilitySource(
                    fileProvider,
                    serviceProvider.GetRequiredService<IOptionsMonitor<YamlAgentCapabilitySourceOptions>>(),
                    logger);
            });
            
            // Register the capability registry that uses the YAML source
            services.TryAddSingleton<IAgentCapabilityRegistry, AgentCapabilityRegistry>();
            
            return services;
        }
        
        /// <summary>
        /// Adds YAML-based agent capability services to the specified <see cref="IServiceCollection"/>
        /// with a specific configuration file path.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="filePath">The path to the YAML configuration file.</param>
        /// <param name="throwOnFileNotFound">Whether to throw an exception if the file is not found.</param>
        /// <param name="validateSchema">Whether to validate the YAML schema.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddYamlAgentCapabilities(
            this IServiceCollection services,
            string filePath,
            bool throwOnFileNotFound = true,
            bool validateSchema = true)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                
            return services.AddYamlAgentCapabilities(options =>
            {
                options.FilePath = filePath;
                options.ThrowOnFileNotFound = throwOnFileNotFound;
                options.ValidateSchema = validateSchema;
            });
        }
    }
}
