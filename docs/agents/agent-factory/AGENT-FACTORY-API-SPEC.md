# EMMA Agent Factory - API Specification

## API Overview

The Agent Factory API provides comprehensive endpoints for creating, managing, and deploying AI agents through a RESTful interface. This API supports the complete agent lifecycle from blueprint creation to hot deployment and monitoring.

### Base URL
```
Production: https://api.emma-platform.com/v1/agent-factory
Development: https://dev-api.emma-platform.com/v1/agent-factory
Local: https://localhost:5001/api/agent-factory
```

### Authentication
All API endpoints require JWT Bearer token authentication with appropriate role-based permissions.

```http
Authorization: Bearer <jwt_token>
```

## Agent Blueprint Management

### Create Agent Blueprint
Creates a new agent blueprint with validation.

```http
POST /blueprints
Content-Type: application/json
Authorization: Bearer <token>
```

**Request Body:**
```json
{
  "name": "Follow-Up Reminder Agent",
  "description": "Automatically reminds agents to follow up with leads after property showings",
  "goal": "Increase lead conversion by ensuring timely follow-ups",
  "triggerConfig": {
    "supportedIntents": ["FOLLOW_UP_REMINDER", "LEAD_NURTURING"],
    "eventTriggers": ["interaction.completed", "showing.completed"],
    "mode": "Proactive",
    "triggerConditions": {
      "timeSinceLastContact": "24h",
      "leadStatus": "active"
    }
  },
  "contextConfig": {
    "requiredContextTypes": ["lastInteraction", "contactProfile", "propertyDetails"],
    "optionalContextTypes": ["marketData", "competitorActivity"],
    "accessLevel": "ContactOnly",
    "contextFilters": {
      "includePersonalInfo": true,
      "includeSensitiveData": false
    }
  },
  "actionConfig": {
    "allowedActionTypes": ["SendSMS", "SendEmail", "CreateTask", "ScheduleReminder"],
    "maxAllowedScope": "Hybrid",
    "overrideMode": "RiskBased",
    "actionConstraints": {
      "maxDailyActions": 10,
      "cooldownPeriod": "2h"
    }
  },
  "validationConfig": {
    "minConfidenceThreshold": 0.75,
    "requireApprovalForAllActions": false,
    "highRiskActionTypes": ["SendEmail"],
    "intensity": "Standard"
  },
  "promptConfig": {
    "promptTemplateId": "follow_up_reminder_v2",
    "promptVariables": {
      "tone": "professional",
      "urgency": "medium",
      "personalization": "high"
    },
    "customSystemPrompt": "You are a helpful assistant that creates personalized follow-up reminders for real estate agents.",
    "customUserPromptTemplate": "Create a follow-up reminder for {contactName} regarding {propertyAddress}. Last interaction: {lastInteractionSummary}"
  },
  "industryProfile": "RealEstate",
  "industrySpecificConfig": {
    "mlsIntegration": true,
    "propertyAlerts": true,
    "marketAnalysis": false
  }
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Follow-Up Reminder Agent",
  "description": "Automatically reminds agents to follow up with leads after property showings",
  "goal": "Increase lead conversion by ensuring timely follow-ups",
  "triggerConfig": { /* ... */ },
  "contextConfig": { /* ... */ },
  "actionConfig": { /* ... */ },
  "validationConfig": { /* ... */ },
  "promptConfig": { /* ... */ },
  "createdBy": "john.doe@company.com",
  "createdAt": "2025-06-09T17:00:00Z",
  "lastModified": null,
  "modifiedBy": null,
  "status": "Draft",
  "deploymentId": null,
  "industryProfile": "RealEstate",
  "industrySpecificConfig": { /* ... */ }
}
```

### Get Agent Blueprint
Retrieves a specific agent blueprint by ID.

```http
GET /blueprints/{blueprintId}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Follow-Up Reminder Agent",
  /* ... full blueprint object ... */
}
```

### List Agent Blueprints
Retrieves all agent blueprints with optional filtering.

```http
GET /blueprints?createdBy={email}&status={status}&industryProfile={profile}&page={page}&limit={limit}
Authorization: Bearer <token>
```

**Query Parameters:**
- `createdBy` (optional): Filter by creator email
- `status` (optional): Filter by status (Draft, Active, Paused, etc.)
- `industryProfile` (optional): Filter by industry profile
- `page` (optional): Page number (default: 1)
- `limit` (optional): Items per page (default: 20, max: 100)

**Response (200 OK):**
```json
{
  "blueprints": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Follow-Up Reminder Agent",
      "description": "Automatically reminds agents to follow up with leads",
      "status": "Active",
      "createdBy": "john.doe@company.com",
      "createdAt": "2025-06-09T17:00:00Z",
      "deploymentId": "agent-followup-prod-001"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 1,
    "totalPages": 1
  }
}
```

### Update Agent Blueprint
Updates an existing agent blueprint.

```http
PUT /blueprints/{blueprintId}
Content-Type: application/json
Authorization: Bearer <token>
```

**Request Body:** Same as create blueprint request

**Response (200 OK):** Updated blueprint object

### Delete Agent Blueprint
Soft deletes an agent blueprint (sets status to Deprecated).

```http
DELETE /blueprints/{blueprintId}
Authorization: Bearer <token>
```

**Response (204 No Content)**

## Blueprint Validation

### Validate Blueprint
Validates a blueprint without creating it.

```http
POST /blueprints/validate
Content-Type: application/json
Authorization: Bearer <token>
```

**Request Body:** Same as create blueprint request

**Response (200 OK):**
```json
{
  "isValid": true,
  "validationResults": [
    {
      "category": "Security",
      "level": "Info",
      "message": "Blueprint follows security best practices",
      "details": null
    },
    {
      "category": "Performance",
      "level": "Warning",
      "message": "High context requirements may impact performance",
      "details": {
        "estimatedMemoryUsage": "256MB",
        "estimatedExecutionTime": "2.5s"
      }
    }
  ],
  "estimatedPerformance": {
    "compilationTimeMs": 1500,
    "deploymentTimeMs": 800,
    "memoryUsageMB": 256,
    "expectedThroughput": 100
  },
  "securityAssessment": {
    "riskLevel": "Low",
    "requiredApprovals": [],
    "restrictedActions": []
  }
}
```

### Get Validation Templates
Retrieves available validation templates and rules.

```http
GET /validation/templates
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "templates": [
    {
      "id": "real_estate_standard",
      "name": "Real Estate Standard Validation",
      "description": "Standard validation rules for real estate agents",
      "rules": [
        {
          "name": "ContactDataAccess",
          "description": "Validates appropriate contact data access levels",
          "severity": "Error"
        }
      ]
    }
  ]
}
```

## Agent Deployment

### Deploy Agent
Deploys an agent blueprint to production.

```http
POST /blueprints/{blueprintId}/deploy
Authorization: Bearer <token>
```

**Request Body (optional):**
```json
{
  "environment": "production",
  "scalingConfig": {
    "minInstances": 1,
    "maxInstances": 5,
    "targetCpuPercent": 70
  },
  "deploymentOptions": {
    "hotReload": true,
    "rollbackOnFailure": true,
    "healthCheckTimeout": 30
  }
}
```

**Response (202 Accepted):**
```json
{
  "deploymentId": "agent-followup-prod-001",
  "status": "Deploying",
  "estimatedCompletionTime": "2025-06-09T17:02:30Z",
  "deploymentUrl": "/deployments/agent-followup-prod-001"
}
```

### Get Deployment Status
Retrieves the current status of a deployment.

```http
GET /deployments/{deploymentId}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "deploymentId": "agent-followup-prod-001",
  "blueprintId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Active",
  "deployedAt": "2025-06-09T17:02:15Z",
  "deployedBy": "john.doe@company.com",
  "version": 1,
  "healthStatus": "Healthy",
  "lastHealthCheck": "2025-06-09T17:05:00Z",
  "performance": {
    "compilationTimeMs": 1450,
    "deploymentTimeMs": 750,
    "memoryUsageMB": 245,
    "errorCount": 0,
    "successRate": 100.0
  },
  "endpoints": {
    "execute": "/agents/agent-followup-prod-001/execute",
    "health": "/agents/agent-followup-prod-001/health",
    "metrics": "/agents/agent-followup-prod-001/metrics"
  }
}
```

### Update Deployment (Hot Reload)
Updates a deployed agent with a new version.

```http
PUT /deployments/{deploymentId}
Content-Type: application/json
Authorization: Bearer <token>
```

**Request Body:**
```json
{
  "blueprintId": "550e8400-e29b-41d4-a716-446655440000",
  "hotReload": true,
  "rollbackOnFailure": true
}
```

**Response (202 Accepted):** Deployment status object

### Stop Deployment
Stops a running agent deployment.

```http
POST /deployments/{deploymentId}/stop
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "deploymentId": "agent-followup-prod-001",
  "status": "Stopping",
  "message": "Agent deployment is being gracefully stopped"
}
```

### List Deployments
Retrieves all agent deployments.

```http
GET /deployments?status={status}&deployedBy={email}&page={page}&limit={limit}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "deployments": [
    {
      "deploymentId": "agent-followup-prod-001",
      "blueprintId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Follow-Up Reminder Agent",
      "status": "Active",
      "deployedAt": "2025-06-09T17:02:15Z",
      "deployedBy": "john.doe@company.com",
      "healthStatus": "Healthy"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 1,
    "totalPages": 1
  }
}
```

## Agent Execution

### Execute Agent
Executes a deployed agent with provided context.

```http
POST /agents/{deploymentId}/execute
Content-Type: application/json
Authorization: Bearer <token>
```

**Request Body:**
```json
{
  "context": {
    "contactId": "12345",
    "interactionType": "showing_completed",
    "propertyId": "prop-67890",
    "agentId": "agent-001",
    "customData": {
      "showingDuration": "45min",
      "clientFeedback": "positive",
      "nextSteps": "follow_up_required"
    }
  },
  "overrides": {
    "urgency": "high",
    "personalizedMessage": true
  },
  "executionOptions": {
    "async": false,
    "timeout": 30,
    "retryOnFailure": true
  }
}
```

**Response (200 OK):**
```json
{
  "executionId": "exec-550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "startedAt": "2025-06-09T17:10:00Z",
  "completedAt": "2025-06-09T17:10:02Z",
  "executionTimeMs": 2150,
  "result": {
    "success": true,
    "actions": [
      {
        "actionType": "SendSMS",
        "recipient": "+1234567890",
        "content": "Hi Sarah! Thanks for viewing the property at 123 Main St today. I'd love to hear your thoughts and answer any questions. When would be a good time to chat?",
        "scheduledFor": "2025-06-09T18:00:00Z",
        "confidence": 0.89,
        "validationStatus": "Approved"
      }
    ],
    "metadata": {
      "confidenceScore": 0.89,
      "validationResults": [
        {
          "validator": "ContentSafety",
          "status": "Passed",
          "score": 0.95
        }
      ]
    }
  }
}
```

### Get Agent Health
Checks the health status of a deployed agent.

```http
GET /agents/{deploymentId}/health
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "deploymentId": "agent-followup-prod-001",
  "status": "Healthy",
  "lastCheckAt": "2025-06-09T17:15:00Z",
  "uptime": "2h 13m 45s",
  "checks": [
    {
      "name": "ResponseTime",
      "status": "Healthy",
      "value": "1.2s",
      "threshold": "5s"
    },
    {
      "name": "MemoryUsage",
      "status": "Healthy",
      "value": "245MB",
      "threshold": "512MB"
    },
    {
      "name": "ErrorRate",
      "status": "Healthy",
      "value": "0.1%",
      "threshold": "5%"
    }
  ]
}
```

## Analytics & Monitoring

### Get Agent Metrics
Retrieves performance metrics for a deployed agent.

```http
GET /agents/{deploymentId}/metrics?startDate={date}&endDate={date}&granularity={granularity}
Authorization: Bearer <token>
```

**Query Parameters:**
- `startDate`: Start date for metrics (ISO 8601 format)
- `endDate`: End date for metrics (ISO 8601 format)
- `granularity`: Data granularity (hour, day, week, month)

**Response (200 OK):**
```json
{
  "deploymentId": "agent-followup-prod-001",
  "period": {
    "startDate": "2025-06-09T00:00:00Z",
    "endDate": "2025-06-09T23:59:59Z",
    "granularity": "hour"
  },
  "metrics": {
    "execution": {
      "totalExecutions": 156,
      "successfulExecutions": 152,
      "failedExecutions": 4,
      "successRate": 97.4,
      "averageExecutionTimeMs": 1850
    },
    "validation": {
      "averageConfidenceScore": 0.87,
      "validationSuccessRate": 98.7,
      "approvalRequiredCount": 12,
      "autoApprovedCount": 144
    },
    "performance": {
      "averageMemoryUsageMB": 245,
      "peakMemoryUsageMB": 298,
      "cpuUsagePercent": 15.2,
      "responseTimeP95Ms": 2100
    },
    "actions": {
      "totalActionsGenerated": 152,
      "actionsByType": {
        "SendSMS": 89,
        "SendEmail": 45,
        "CreateTask": 18
      },
      "actionsByScope": {
        "InnerWorld": 18,
        "Hybrid": 134,
        "RealWorld": 0
      }
    }
  },
  "timeSeries": [
    {
      "timestamp": "2025-06-09T17:00:00Z",
      "executions": 12,
      "successRate": 100.0,
      "averageExecutionTimeMs": 1750,
      "memoryUsageMB": 240
    }
  ]
}
```

### Get System Metrics
Retrieves system-wide agent factory metrics.

```http
GET /metrics/system?startDate={date}&endDate={date}
Authorization: Bearer <token>
Requires-Role: Administrator
```

**Response (200 OK):**
```json
{
  "period": {
    "startDate": "2025-06-09T00:00:00Z",
    "endDate": "2025-06-09T23:59:59Z"
  },
  "overview": {
    "totalAgents": 25,
    "activeAgents": 23,
    "totalExecutions": 1547,
    "totalDeployments": 8,
    "successfulDeployments": 8,
    "failedDeployments": 0
  },
  "performance": {
    "averageCompilationTimeMs": 1650,
    "averageDeploymentTimeMs": 850,
    "systemCpuUsagePercent": 25.4,
    "systemMemoryUsageMB": 2048,
    "hotReloadSuccessRate": 100.0
  },
  "agentDistribution": {
    "byIndustry": {
      "RealEstate": 18,
      "Mortgage": 4,
      "Financial": 3
    },
    "byScope": {
      "InnerWorld": 8,
      "Hybrid": 15,
      "RealWorld": 2
    },
    "byCreator": {
      "ProductManagers": 20,
      "Developers": 5
    }
  }
}
```

## Templates & Configuration

### Get Agent Templates
Retrieves available agent templates.

```http
GET /templates?industryProfile={profile}&category={category}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "templates": [
    {
      "id": "follow_up_reminder",
      "name": "Follow-Up Reminder Agent",
      "description": "Automatically creates follow-up reminders for leads",
      "category": "Lead Management",
      "industryProfile": "RealEstate",
      "complexity": "Simple",
      "estimatedSetupTime": "10 minutes",
      "blueprint": {
        /* Pre-configured blueprint object */
      },
      "requiredCustomization": [
        "promptVariables",
        "triggerConditions"
      ],
      "tags": ["follow-up", "automation", "leads"]
    }
  ]
}
```

### Get Configuration Options
Retrieves available configuration options for agent creation.

```http
GET /config/options
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "intents": [
    {
      "value": "FOLLOW_UP_REMINDER",
      "label": "Follow-Up Reminder",
      "description": "Generate follow-up reminders for contacts"
    }
  ],
  "actionTypes": [
    {
      "value": "SendSMS",
      "label": "Send SMS",
      "description": "Send text message to contact",
      "scope": "RealWorld",
      "requiresApproval": true
    }
  ],
  "contextTypes": [
    {
      "value": "contactProfile",
      "label": "Contact Profile",
      "description": "Basic contact information and preferences",
      "accessLevel": "ContactOnly"
    }
  ],
  "promptTemplates": [
    {
      "id": "follow_up_reminder_v2",
      "name": "Follow-Up Reminder v2",
      "description": "Professional follow-up reminder template",
      "variables": ["contactName", "propertyAddress", "lastInteractionSummary"]
    }
  ],
  "industryProfiles": [
    {
      "value": "RealEstate",
      "label": "Real Estate",
      "description": "Real estate agents and brokers",
      "availableIntegrations": ["MLS", "CRM", "PropertyData"]
    }
  ]
}
```

## Error Handling

### Error Response Format
All API errors follow a consistent format:

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Blueprint validation failed",
    "details": [
      {
        "field": "actionConfig.maxAllowedScope",
        "message": "RealWorld scope requires administrator approval",
        "code": "INSUFFICIENT_PERMISSIONS"
      }
    ],
    "requestId": "req-550e8400-e29b-41d4-a716-446655440000",
    "timestamp": "2025-06-09T17:15:00Z"
  }
}
```

### Common Error Codes

| HTTP Status | Error Code | Description |
|-------------|------------|-------------|
| 400 | `INVALID_REQUEST` | Request body or parameters are invalid |
| 400 | `VALIDATION_FAILED` | Blueprint validation failed |
| 401 | `UNAUTHORIZED` | Authentication required |
| 403 | `FORBIDDEN` | Insufficient permissions |
| 404 | `NOT_FOUND` | Resource not found |
| 409 | `CONFLICT` | Resource already exists or conflict |
| 422 | `UNPROCESSABLE_ENTITY` | Request is valid but cannot be processed |
| 429 | `RATE_LIMITED` | Too many requests |
| 500 | `INTERNAL_ERROR` | Internal server error |
| 503 | `SERVICE_UNAVAILABLE` | Service temporarily unavailable |

## Rate Limiting

API endpoints are rate limited based on user role and endpoint type:

| Endpoint Category | Rate Limit | Window |
|------------------|------------|---------|
| Blueprint CRUD | 100 requests | 1 hour |
| Agent Execution | 1000 requests | 1 hour |
| Deployment Operations | 20 requests | 1 hour |
| Metrics/Analytics | 500 requests | 1 hour |

Rate limit headers are included in all responses:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1625097600
```

## Webhooks

### Deployment Status Webhooks
Configure webhooks to receive deployment status updates.

```http
POST /webhooks/deployment-status
Content-Type: application/json
Authorization: Bearer <token>
```

**Request Body:**
```json
{
  "url": "https://your-app.com/webhooks/agent-deployment",
  "events": ["deployment.started", "deployment.completed", "deployment.failed"],
  "secret": "your-webhook-secret"
}
```

**Webhook Payload Example:**
```json
{
  "event": "deployment.completed",
  "timestamp": "2025-06-09T17:02:15Z",
  "data": {
    "deploymentId": "agent-followup-prod-001",
    "blueprintId": "550e8400-e29b-41d4-a716-446655440000",
    "status": "Active",
    "deployedBy": "john.doe@company.com"
  }
}
```

## SDK Examples

### JavaScript/TypeScript SDK
```typescript
import { AgentFactoryClient } from '@emma/agent-factory-sdk';

const client = new AgentFactoryClient({
  baseUrl: 'https://api.emma-platform.com/v1/agent-factory',
  apiKey: 'your-api-key'
});

// Create and deploy an agent
const blueprint = await client.blueprints.create({
  name: 'Follow-Up Reminder Agent',
  description: 'Automatically reminds agents to follow up with leads',
  // ... rest of configuration
});

const deployment = await client.blueprints.deploy(blueprint.id);
console.log(`Agent deployed: ${deployment.deploymentId}`);

// Execute the agent
const result = await client.agents.execute(deployment.deploymentId, {
  context: {
    contactId: '12345',
    interactionType: 'showing_completed'
  }
});
```

### C# SDK
```csharp
using Emma.AgentFactory.Sdk;

var client = new AgentFactoryClient(new AgentFactoryClientOptions
{
    BaseUrl = "https://api.emma-platform.com/v1/agent-factory",
    ApiKey = "your-api-key"
});

// Create and deploy an agent
var blueprint = await client.Blueprints.CreateAsync(new CreateAgentRequest
{
    Name = "Follow-Up Reminder Agent",
    Description = "Automatically reminds agents to follow up with leads",
    // ... rest of configuration
});

var deployment = await client.Blueprints.DeployAsync(blueprint.Id);
Console.WriteLine($"Agent deployed: {deployment.DeploymentId}");

// Execute the agent
var result = await client.Agents.ExecuteAsync(deployment.DeploymentId, new ExecuteAgentRequest
{
    Context = new Dictionary<string, object>
    {
        ["contactId"] = "12345",
        ["interactionType"] = "showing_completed"
    }
});
```

---

**Document Version**: 1.0  
**Last Updated**: 2025-06-09  
**Next Review**: 2025-07-01  
**Owner**: Platform Engineering Team
