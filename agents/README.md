# EMMA AI-First CRM Agent Orchestration System

## Overview

The EMMA AI-First CRM Agent Orchestration System is a comprehensive, enterprise-grade platform that implements Microsoft Azure AI Foundry best practices with A2A (Agent-to-Agent) protocol compliance. This system provides hot-swappable orchestration, dynamic agent registration, and intelligent workflow management for multi-industry CRM applications.

## 🏗️ Architecture

### Core Components

1. **Agent Registry Service** (`IAgentRegistryService`)
   - Dynamic agent discovery and registration
   - A2A Agent Card manifest loading
   - Health monitoring and performance metrics
   - Capability validation and routing

2. **Intent Classification Service** (`IIntentClassificationService`)
   - AI-powered intent recognition using Azure OpenAI
   - Confidence scoring and urgency assessment
   - Feedback learning and model improvement
   - Industry-specific intent handling

3. **Agent Communication Bus** (`IAgentCommunicationBus`)
   - Hot-swappable orchestration (custom ↔ Azure Foundry)
   - Request routing based on intent and capabilities
   - Workflow execution and state management
   - Performance tracking and fallback handling

4. **Context Intelligence Service** (`IContextIntelligenceService`)
   - Interaction sentiment analysis
   - Buying signal detection
   - Close probability prediction
   - Recommended action generation

### Agent Catalog Structure

```
agents/
├── catalog/
│   ├── orchestrators/
│   │   └── emma-orchestrator.json
│   └── specialized/
│       ├── contact-management-agent.json
│       └── interaction-analysis-agent.json
└── README.md
```

## 🚀 Quick Start

### 1. Prerequisites

- .NET 8.0 or later
- Azure OpenAI service endpoint
- Azure AI Content Safety service (optional)
- CosmosDB account (for state persistence)

### 2. Environment Configuration

```bash
# Azure AI Services
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4

# Azure AI Content Safety (optional)
AZURE_CONTENT_SAFETY_ENDPOINT=https://your-content-safety.cognitiveservices.azure.com/
AZURE_CONTENT_SAFETY_API_KEY=your-content-safety-key

# CosmosDB (for workflow persistence)
COSMOS_ENDPOINT=https://your-cosmos.documents.azure.com:443/
COSMOS_KEY=your-cosmos-key
COSMOS_DATABASE_NAME=emma-agent
COSMOS_CONTAINER_NAME=messages
```

### 3. Service Registration

```csharp
// In Program.cs or Startup.cs
services.AddEmmaCoreServices(configuration);
services.AddEmmaAgentServices();

// For development with enhanced debugging
services.AddEmmaCoreServicesForDevelopment(configuration);
services.AddEmmaAgentServicesForDevelopment();
```

### 4. Load Agent Catalog

```csharp
// Load agents from catalog directory
var agentRegistry = serviceProvider.GetRequiredService<IAgentRegistryService>();
var catalogPath = Path.Combine(Directory.GetCurrentDirectory(), "agents", "catalog");
var loadedCount = await agentRegistry.LoadAgentCatalogAsync(catalogPath);
```

## 🤖 Agent Cards (A2A Protocol)

### Agent Card Schema

Each agent is defined by a JSON manifest following the A2A Agent Card specification:

```json
{
  "id": "emma-orchestrator-v1",
  "name": "EMMA Orchestrator Agent",
  "version": "1.0.0",
  "description": "Master orchestrator for AI-first CRM workflows",
  "type": "orchestrator",
  "capabilities": [
    "workflow_orchestration",
    "intent_routing",
    "agent_coordination"
  ],
  "intents": [
    "GeneralInquiry",
    "ContactManagement", 
    "PropertySearch"
  ],
  "endpoints": {
    "primary": "http://localhost:5000/api/agents/orchestrator",
    "health": "http://localhost:5000/api/agents/orchestrator/health",
    "metrics": "http://localhost:5000/api/agents/orchestrator/metrics"
  },
  "input_schema": {
    "type": "object",
    "properties": {
      "intent": { "type": "string" },
      "context": { "type": "object" },
      "traceId": { "type": "string" }
    }
  },
  "output_schema": {
    "type": "object",
    "properties": {
      "success": { "type": "boolean" },
      "content": { "type": "string" },
      "confidence": { "type": "number" }
    }
  },
  "security": {
    "authentication": "bearer_token",
    "authorization": "role_based",
    "required_roles": ["agent_executor", "workflow_orchestrator"]
  },
  "monitoring": {
    "health_check_interval": 30,
    "performance_metrics": true,
    "distributed_tracing": true
  },
  "compliance": {
    "standards": ["SOC2", "GDPR", "CCPA"],
    "responsible_ai": true,
    "audit_logging": true
  }
}
```

### Creating New Agents

1. Create agent card JSON in appropriate catalog directory
2. Implement agent endpoint following the schema
3. Register security roles and permissions
4. Configure health check and metrics endpoints
5. Load catalog to register the agent

## 🔄 Usage Examples

### Basic Orchestration

```csharp
var orchestrationRequest = new OrchestrationRequest
{
    UserInput = "Update Emily Johnson's contact info and schedule follow-up",
    Context = new Dictionary<string, object>
    {
        ["contactId"] = "550e8400-e29b-41d4-a716-446655440001",
        ["industry"] = "real_estate"
    },
    IncludeRecommendations = true,
    OrchestrationMethod = "custom"
};

var response = await orchestrationController.ProcessRequest(orchestrationRequest);
```

### Workflow Execution

```csharp
var workflowRequest = new WorkflowExecutionRequest
{
    WorkflowId = "client-onboarding-workflow",
    InitialIntent = AgentIntent.ContactManagement,
    InitialInput = "New client wants to start property search",
    Context = new Dictionary<string, object>
    {
        ["clientType"] = "first_time_buyer",
        ["budget"] = 450000
    }
};

var workflowState = await orchestrationController.ExecuteWorkflow(workflowRequest);
```

### Context Intelligence Analysis

```csharp
var analysisRequest = new InteractionAnalysisRequest
{
    InteractionContent = "Client expressed strong interest in downtown properties...",
    ContactContext = new ContactContext
    {
        ContactId = "client-001",
        Name = "Emily Johnson",
        RelationshipState = "ActiveClient"
    }
};

var analysis = await orchestrationController.AnalyzeInteraction(analysisRequest);
```

## 🔧 Configuration

### Orchestration Methods

The system supports two orchestration methods:

1. **Custom Orchestration** (`"custom"`)
   - Uses built-in agent routing and workflow logic
   - Full control over agent coordination
   - Immediate availability

2. **Azure AI Foundry** (`"azure_foundry"`)
   - Integrates with Azure AI Foundry multi-agent workflows
   - Native Microsoft orchestration capabilities
   - Requires Azure AI Foundry preview access

Switch between methods dynamically:

```csharp
communicationBus.SetOrchestrationMethod("azure_foundry");
```

### Agent Health Monitoring

Agents are continuously monitored for:
- Endpoint availability
- Response time performance
- Error rates and patterns
- Capability validation

### Performance Metrics

The system tracks:
- Intent classification accuracy
- Agent response times
- Workflow completion rates
- Context intelligence insights
- User satisfaction scores

## 🧪 Testing

### Integration Tests

Run comprehensive integration tests:

```bash
dotnet test Emma.Tests/Integration/AgentOrchestrationIntegrationTests.cs
```

### Demo Script

Execute the complete demonstration:

```powershell
.\scripts\demo-agent-orchestration.ps1
```

## 📊 Monitoring & Observability

### Distributed Tracing

All requests include trace IDs for end-to-end observability:

```csharp
var traceId = Guid.NewGuid().ToString();
// Trace ID propagated through all agent calls
```

### Application Insights Integration

Automatic telemetry for:
- Agent performance metrics
- Intent classification results
- Workflow execution status
- Error rates and patterns
- User interaction analytics

### Health Endpoints

- `/api/agentorchestration/agents/health` - Agent health status
- `/api/agentorchestration/agents/capabilities` - Available capabilities
- `/api/health` - Overall system health

## 🔒 Security & Compliance

### Authentication & Authorization

- JWT bearer token authentication
- Role-based access control (RBAC)
- Scoped permissions per agent
- Multi-tenant isolation

### Responsible AI

- Azure AI Content Safety integration
- PII detection and redaction
- Bias detection and mitigation
- Audit logging for compliance

### Industry Compliance

- SOC2 Type II ready
- GDPR compliance features
- CCPA privacy controls
- Industry-specific regulations (Fair Housing Act, etc.)

## 🚀 Deployment

### Azure Container Apps

```yaml
# container-app.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: emma-agent-orchestrator
spec:
  replicas: 3
  selector:
    matchLabels:
      app: emma-agent-orchestrator
  template:
    spec:
      containers:
      - name: emma-api
        image: emmacr.azurecr.io/emma-api:latest
        ports:
        - containerPort: 5000
        env:
        - name: AZURE_OPENAI_ENDPOINT
          valueFrom:
            secretKeyRef:
              name: emma-secrets
              key: openai-endpoint
```

### Environment-Specific Configuration

- **Development**: Mock services and enhanced logging
- **Staging**: Full Azure integration with test data
- **Production**: Enterprise security and monitoring

## 🔄 Migration Path

### Phase 1: Custom Orchestration
- Implement all agent services with custom routing
- Establish agent catalog and registration
- Deploy with full observability

### Phase 2: Hybrid Approach
- Side-by-side testing with Azure AI Foundry
- Gradual migration of workflows
- Performance comparison and optimization

### Phase 3: Native Azure AI Foundry
- Full migration to Azure AI Foundry orchestration
- Leverage native multi-agent capabilities
- Advanced workflow designer integration

## 📈 Roadmap

### Q1 2025
- ✅ Core agent orchestration system
- ✅ A2A protocol compliance
- ✅ Intent classification and context intelligence
- 🔄 Enhanced workflow persistence

### Q2 2025
- 🔄 Azure AI Foundry native integration
- 🔄 Advanced agent marketplace
- 🔄 Multi-modal agent capabilities
- 🔄 Enterprise deployment automation

### Q3 2025
- 🔄 Self-organizing agent networks
- 🔄 Advanced learning and adaptation
- 🔄 Cross-cloud agent interoperability
- 🔄 Industry-specific agent packs

## 🤝 Contributing

1. Follow A2A Agent Card specifications
2. Implement comprehensive tests
3. Include security and compliance considerations
4. Document agent capabilities and limitations
5. Ensure Azure AI Foundry compatibility

## 📚 Additional Resources

- [Azure AI Foundry Documentation](https://docs.microsoft.com/azure/ai-foundry)
- [A2A Protocol Specification](https://github.com/microsoft/a2a-protocol)
- [Microsoft Responsible AI Guidelines](https://www.microsoft.com/ai/responsible-ai)
- [EMMA Data Dictionary](../EMMA-DATA-DICTIONARY.md)
- [EMMA AI Architecture Guide](../EMMA-AI-ARCHITECTURE-GUIDE.md)

---

**Built with ❤️ by the EMMA Team**  
*Empowering AI-first CRM with enterprise-grade agent orchestration*
