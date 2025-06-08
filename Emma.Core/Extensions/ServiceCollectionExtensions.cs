using Emma.Core.Interfaces;
using Emma.Core.Services;
using Emma.Core.Extensions;
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
    /// Registers all Emma Core AI agent services for Azure AI Foundry integration.
    /// </summary>
    public static IServiceCollection AddEmmaAgentServices(this IServiceCollection services)
    {
        // Register core agent services
        services.AddSingleton<IAgentRegistryService, AgentRegistryService>();
        services.AddScoped<IIntentClassificationService, IntentClassificationService>();
        services.AddScoped<IAgentCommunicationBus, AgentCommunicationBus>();
        services.AddScoped<IContextIntelligenceService, ContextIntelligenceService>();
        
        return services;
    }

    /// <summary>
    /// Registers Emma Core agent services for development with enhanced debugging.
    /// </summary>
    public static IServiceCollection AddEmmaAgentServicesForDevelopment(this IServiceCollection services)
    {
        // Add all agent services
        services.AddEmmaAgentServices();
        
        // Additional development-specific configurations
        // Enhanced logging, debug middleware, agent monitoring, etc.
        
        return services;
    }

    /// <summary>
    /// Registers all Emma Core services (privacy + agents).
    /// </summary>
    public static IServiceCollection AddEmmaCoreServices(this IServiceCollection services)
    {
        services.AddEmmaPrivacyServices();
        services.AddEmmaAgentServices();
        
        return services;
    }

    /// <summary>
    /// Registers all Emma Core services for development environment.
    /// </summary>
    public static IServiceCollection AddEmmaCoreServicesForDevelopment(this IServiceCollection services)
    {
        services.AddEmmaPrivacyServicesForDevelopment();
        services.AddEmmaAgentServicesForDevelopment();
        
        return services;
    }
}
