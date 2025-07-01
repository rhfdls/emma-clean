using Emma.Core.Services.Validation;
using Emma.Core.Services;
using Emma.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Emma.Core.Interfaces;

namespace Emma.Core.Extensions
{
    /// <summary>
    /// Extension methods for setting up validation services in an <see cref="IServiceCollection"/>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the core validation services to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddCoreValidationServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register validators
            services.TryAddScoped<IMessageValidator, MessageValidator>();
            
            // Register any additional validation services here
            
            return services;
        }

        /// <summary>
        /// Adds the interaction context services to the service collection.
        /// </summary>
        public static IServiceCollection AddInteractionContextServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Register options
            services.Configure<InteractionContextOptions>(
                configuration.GetSection(InteractionContextOptions.SectionName));

            // Register core services
            services.AddMemoryCache();
            
            // Register the context provider
            services.AddScoped<IInteractionContextProvider, InteractionContextProvider>();
            
            return services;
        }
    }
}
