using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Emma.Core.Agents;
using Emma.Core.Configuration;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emma.Core.Extensions;

/// <summary>
/// Extension methods for setting up agent orchestration services in an <see cref="IServiceCollection"/>
/// </summary>
public static class AgentOrchestratorExtensions
{
    /// <summary>
    /// Adds the agent orchestration services to the specified <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure the agent orchestration options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAgentOrchestration(
        this IServiceCollection services,
        Action<AgentOrchestratorOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register options
        if (configure != null)
        {
            services.Configure(configure);
        }
        
        // Register core services
        services.TryAddSingleton<IOrchestrationLogger, OrchestrationLogger>();
        services.TryAddSingleton<IAgentCapabilityRegistry, AgentCapabilityRegistry>();
        services.TryAddSingleton<IAgentRegistry, AgentRegistry>();
        services.TryAddScoped<IAgentOrchestrator, EnhancedAgentOrchestrator>();
        
        // Register built-in agents
        services.AddAgentsFromAssembly(Assembly.GetExecutingAssembly());
        
        return services;
    }
    
    /// <summary>
    /// Scans the specified assembly for classes that implement <see cref="IAgent"/> and registers them with the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the agents to.</param>
    /// <param name="assembly">The assembly to scan for agent implementations.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAgentsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
            
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));
        
        var agentTypes = assembly.GetTypes()
            .Where(t => typeof(IAgent).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
            
        foreach (var type in agentTypes)
        {
            services.AddAgent(type);
        }
        
        return services;
    }
    
    /// <summary>
    /// Registers a specific agent type with the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the agent to.</param>
    /// <typeparam name="TAgent">The type of the agent to register.</typeparam>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAgent<TAgent>(this IServiceCollection services) 
        where TAgent : class, IAgent
    {
        return services.AddAgent(typeof(TAgent));
    }
    
    /// <summary>
    /// Registers a specific agent type with the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the agent to.</param>
    /// <param name="agentType">The type of the agent to register.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddAgent(this IServiceCollection services, Type agentType)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
            
        if (agentType == null)
            throw new ArgumentNullException(nameof(agentType));
            
        if (!typeof(IAgent).IsAssignableFrom(agentType))
            throw new ArgumentException($"Type {agentType.Name} does not implement {nameof(IAgent)}", nameof(agentType));
            
        // Register the agent as a scoped service
        services.AddScoped(agentType);
        
        // Also register it as IAgent for discovery
        services.AddScoped<IAgent>(sp => (IAgent)sp.GetRequiredService(agentType));
        
        return services;
    }
    
    /// <summary>
    /// Configures the agent options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigureAgentOrchestrator(
        this IServiceCollection services,
        Action<AgentOrchestratorOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
            
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));
            
        services.Configure(configureOptions);
        return services;
    }
    
    /// <summary>
    /// Configures the agent options for a specific agent type.
    /// </summary>
    /// <typeparam name="TAgent">The type of the agent to configure.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configureOptions">The action used to configure the agent options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigureAgent<TAgent>(
        this IServiceCollection services,
        Action<AgentOptions> configureOptions)
        where TAgent : IAgent
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
            
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));
            
        services.Configure(configureOptions);
        return services;
    }
    
    /// <summary>
    /// Gets all registered agent types from the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to search.</param>
    /// <returns>A collection of registered agent types.</returns>
    public static IEnumerable<Type> GetRegisteredAgentTypes(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
            
        return services
            .Where(s => typeof(IAgent).IsAssignableFrom(s.ServiceType) && s.ServiceType.IsClass && !s.ServiceType.IsAbstract)
            .Select(s => s.ServiceType)
            .Distinct();
    }
}
