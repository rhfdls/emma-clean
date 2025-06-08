using Emma.Core.Services;
using Emma.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Emma.Core.Extensions;

/// <summary>
/// Single entry point for registering all privacy-related services.
/// Keeps privacy implementation centralized and configurable.
/// </summary>
public static class PrivacyServiceExtensions
{
    /// <summary>
    /// Registers privacy services for development environment.
    /// Uses relaxed masking but keeps all privacy hooks in place.
    /// </summary>
    public static IServiceCollection AddEmmaPrivacyServicesForDevelopment(this IServiceCollection services)
    {
        // Core privacy services
        services.AddScoped<IDataMaskingService, DataMaskingService>();
        services.AddScoped<IContactAccessService, ContactAccessService>();
        services.AddScoped<IInteractionAccessService, InteractionAccessService>();
        
        // Configure for development (can be overridden by appsettings)
        services.Configure<PrivacySettings>(options =>
        {
            options.DefaultMaskingLevel = MaskingLevel.Partial; // Relaxed for dev
            options.EnableAuditLogging = true;
            options.EnablePrivacyDebugMiddleware = true;
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers privacy services for production environment.
    /// Uses strict masking and full audit logging.
    /// </summary>
    public static IServiceCollection AddEmmaPrivacyServicesForProduction(this IServiceCollection services)
    {
        // Core privacy services
        services.AddScoped<IDataMaskingService, DataMaskingService>();
        services.AddScoped<IContactAccessService, ContactAccessService>();
        services.AddScoped<IInteractionAccessService, InteractionAccessService>();
        
        // Configure for production
        services.Configure<PrivacySettings>(options =>
        {
            options.DefaultMaskingLevel = MaskingLevel.Standard; // Strict for prod
            options.EnableAuditLogging = true;
            options.EnablePrivacyDebugMiddleware = false; // No debug info in prod
        });
        
        return services;
    }
    
    /// <summary>
    /// Registers privacy services with custom configuration.
    /// Allows fine-tuned control for specific environments.
    /// </summary>
    public static IServiceCollection AddEmmaPrivacyServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core privacy services
        services.AddScoped<IDataMaskingService, DataMaskingService>();
        services.AddScoped<IContactAccessService, ContactAccessService>();
        services.AddScoped<IInteractionAccessService, InteractionAccessService>();
        
        // Bind configuration from appsettings
        services.Configure<PrivacySettings>(configuration.GetSection("Privacy"));
        
        return services;
    }
}

/// <summary>
/// Configuration settings for privacy services.
/// Centralized control over all privacy behavior.
/// </summary>
public class PrivacySettings
{
    public MaskingLevel DefaultMaskingLevel { get; set; } = MaskingLevel.Standard;
    public bool EnableAuditLogging { get; set; } = true;
    public bool EnablePrivacyDebugMiddleware { get; set; } = false;
    public bool EnableJwtAuthentication { get; set; } = false;
    public string? JwtSecretKey { get; set; }
    public int JwtExpirationMinutes { get; set; } = 60;
}
