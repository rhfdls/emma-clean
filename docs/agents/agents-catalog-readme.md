# EMMA AI Agent Catalog

This directory contains A2A (Agent-to-Agent) compliant agent cards following Microsoft AI Foundry best practices and the open A2A protocol specification.

## Directory Structure

```
agents/
├── catalog/                    # A2A Agent Card definitions
│   ├── emma-orchestrator.json  # Master orchestrator agent
│   ├── contact-management.json # Contact CRUD operations
│   ├── interaction-analysis.json # AI-powered interaction insights
│   ├── scheduling-tasks.json   # Calendar and task management
│   ├── communication.json      # Multi-channel messaging
│   ├── market-intelligence.json # Data-driven insights
│   └── templates/              # Agent card templates
├── registry/                   # Agent registration and discovery
└── health/                     # Agent health monitoring
```

## A2A Agent Card Specification

Each agent card follows the A2A protocol specification and includes:

- **Agent Metadata**: ID, name, description, version, publisher
- **Capabilities**: Input/output schemas, supported operations
- **Endpoints**: Primary, health, metrics endpoints
- **Configuration**: Runtime parameters and limits
- **Security**: Required permissions and access controls
- **Compatibility**: Supported industries and orchestration methods

## Usage

1. **Agent Registration**: Agents are automatically registered on startup by scanning this catalog
2. **Discovery**: The `IAgentCommunicationBus` uses these cards for routing decisions
3. **Validation**: Input/output schemas ensure type safety and compatibility
4. **Migration**: Version metadata enables seamless Azure AI Foundry migration

## Microsoft AI Foundry Compatibility

These agent cards are designed to be:
- **Connected Agents** compatible (public preview)
- **Multi-Agent Workflows** ready (coming soon)
- **Agent Catalog** compliant for ecosystem integration
- **Hot-swappable** between custom and native orchestration

## Best Practices

- Keep agent cards versioned and backward compatible
- Include comprehensive input/output schemas
- Document required permissions and security constraints
- Maintain health and metrics endpoints for observability
- Use semantic versioning for capability evolution
