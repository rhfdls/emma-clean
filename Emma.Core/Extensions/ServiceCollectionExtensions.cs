using Emma.Core.Interfaces;
using Emma.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Emma.Core.Extensions;

/// <summary>
/// Extension methods for registering Emma Core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Emma Core privacy and access control services.
    /// </summary>
    public static IServiceCollection AddEmmaPrivacyServices(this IServiceCollection services)
    {
        // Register privacy enforcement services
        services.AddScoped<IContactAccessService, ContactAccessService>();
        services.AddScoped<IInteractionAccessService, InteractionAccessService>();
        
        // Register data masking service for privacy-aware debugging
        services.AddScoped<IDataMaskingService, DataMaskingService>();
        
        return services;
    }
    
    /// <summary>
    /// Registers Emma Core services for development with enhanced debugging capabilities.
    /// </summary>
    public static IServiceCollection AddEmmaPrivacyServicesForDevelopment(this IServiceCollection services)
    {
        // Add all privacy services
        services.AddEmmaPrivacyServices();
        
        // Additional development-specific configurations can be added here
        // For example: enhanced logging, debug middleware, etc.
        
        return services;
    }
}
