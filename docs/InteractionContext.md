# Interaction Context System

## Overview
The Interaction Context System manages the state and context of user interactions within the Emma platform. It provides a unified way to access and manage interaction data, agent context, tenant settings, and intelligence across the application.

## Key Components

### 1. Core Models

#### InteractionContext
Represents the complete context of an interaction between users and agents.
- Tracks interaction state, messages, and metadata
- Maintains references to users, contacts, and organizations
- Includes audit information for tracking changes

#### AgentContext
Manages agent-specific state within an interaction.
- Tracks agent capabilities and state
- Maintains agent activity timestamps
- Links to the parent interaction and organization

#### TenantContext
Provides access to tenant/organization settings.
- Manages feature flags and configuration
- Stores industry-specific settings
- Handles timezone and localization

#### ContextIntelligence
Stores analysis and insights about interactions.
- Sentiment analysis
- Buying signals
- Recommendations and insights

### 2. InteractionContextProvider

The main service that provides access to interaction context data with built-in caching and orchestration integration.

### 3. Configuration

Configure the system in `appsettings.json`:

```json
{
  "InteractionContext": {
    "CacheExpiration": "00:15:00",
    "IntelligenceCacheExpiration": "00:05:00",
    "EnableCaching": true
  }
}
```

## Usage

### 1. Register Services

In your `Startup.cs` or service configuration:

```csharp
// Register interaction context services
services.AddInteractionContextServices(Configuration);
```

### 2. Inject and Use

```csharp
public class MyService
{
    private readonly IInteractionContextProvider _contextProvider;
    
    public MyService(IInteractionContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }
    
    public async Task<InteractionContext> GetInteractionAsync(Guid interactionId)
    {
        return await _contextProvider.GetInteractionContextAsync(interactionId);
    }
    
    public async Task<AgentContext> GetAgentContextAsync(string agentType, Guid interactionId)
    {
        return await _contextProvider.GetAgentContextAsync(agentType, interactionId);
    }
}
```

## Integration with Other Systems

### NBA Context Integration
- The system integrates with the NBA context service to provide contact-specific data
- Caches NBA context to improve performance
- Handles errors and fallbacks gracefully

### Agent Orchestration
- Provides agent context to the orchestration system
- Supports agent handoffs and state management
- Integrates with the agent registry

## Error Handling
- All methods include trace IDs for correlation
- Logs important operations and errors
- Implements proper disposal patterns

## Performance Considerations
- Uses in-memory caching with configurable expiration
- Lazy loading of context data
- Efficient serialization of context objects

## Testing
Unit tests should verify:
- Context retrieval and updates
- Caching behavior
- Error conditions
- Integration with other services

## Best Practices
1. Always provide a trace ID for operations
2. Use the context provider as the single source of truth for interaction state
3. Keep context objects lean and focused
4. Update context state through the provider methods
5. Handle disposal properly in consuming services
