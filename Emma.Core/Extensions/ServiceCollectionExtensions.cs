using Emma.Core.Services;
using Emma.Core.Extensions;
using Emma.Models.Interfaces;
using Emma.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Emma.Core.Extensions;

/// <summary>
/// Extension methods for registering Emma Core services.
/// Enhanced with Sprint 1 Agent Factory services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Sprint 1 Agent Factory services including dynamic agent registry and feature flags.
    /// </summary>
    public static IServiceCollection AddEmmaSprint1Services(this IServiceCollection services)
    {
        // Register Agent Registry for dynamic agent management
        services.AddSingleton<IAgentRegistry, AgentRegistry>();
        
        // Register Feature Flag service for runtime configuration
        services.AddFeatureFlags();
        
        // Register API versioning for scalable evolution
        services.AddEmmaApiVersioning();
        
        // Register Context Provider for unified context access
        services.AddScoped<IContextProvider, ContextProvider>();
        
        return services;
    }

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
    /// Registers interaction-related services and their dependencies.
    /// </summary>
    public static IServiceCollection AddInteractionServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register options
        services.Configure<InteractionOptions>(configuration.GetSection("Interaction"));
        services.Configure<AiOptions>(configuration.GetSection("AI"));
        
        // Register core services
        services.AddScoped<IInteractionService, InteractionService>();
        
        // Register repositories
        services.AddScoped<IInteractionRepository, InteractionRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        
        return services;
    }
    
    /// <summary>
    /// Registers all Emma Core AI agent services for Azure AI Foundry integration.
    /// Enhanced with Sprint 1 dynamic routing capabilities.
    /// </summary>
    public static IServiceCollection AddEmmaAgentServices(this IServiceCollection services)
    {
        // Register Sprint 1 services first
        services.AddEmmaSprint1Services();
        
        // Register core services
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IContextIntelligenceService, ContextIntelligenceService>();
        services.AddScoped<IIntentClassificationService, IntentClassificationService>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        
        // Register layered agents (first-class AI agents)
        services.AddScoped<INbaAgent, NbaAgent>();
        services.AddScoped<IContextIntelligenceAgent, ContextIntelligenceAgent>();
        services.AddScoped<IIntentClassificationAgent, IntentClassificationAgent>();
        services.AddScoped<IResourceAgent, ResourceAgent>();
        
        // Register the AgentOrchestrator that manages all agents
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        
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
