# SQL Context Integration for EMMA's AI NBA Platform

## Overview

This document describes the implementation of secure SQL context extraction for EMMA's AI-driven Next Best Action (NBA) platform. The system extracts structured SQL data, converts it into JSON context objects, and enforces strict role-based access and privacy controls for AI consumption, following EMMA's contact-centric data model.

## Architecture

### Core Components

1. **ISqlContextExtractor Interface** (`Emma.Core.Interfaces`)
   - Defines the contract for SQL data extraction and security filtering
   - Methods for contact, organization, and resource context extraction
   - Enforces tenant isolation and multi-tenant architecture

2. **SqlContextData Model** (`Emma.Core.Models`)
   - Root context object containing role-specific data
   - Includes security metadata and privacy controls
   - Structured for JSON serialization for AI prompt injection
   - Implements EMMA data dictionary compliance rules

3. **SqlContextExtractor Service** (`Emma.Core.Services`)
   - Implements secure SQL data extraction with EF Core
   - Enforces role-based access control (Agent, Admin, AIWorkflow, Collaborator)
   - Applies tenant isolation and data filtering
   - Integrates with EMMA's contact-centric model
   - Implements privacy controls based on data classification

4. **Tenant Context Integration**
   - `ITenantContextService` for multi-tenant data isolation
   - Tenant-specific data filtering and access control
   - Industry-vertical specific context adaptation

5. **Security and Privacy**
   - Role-based access control (RBAC) implementation
   - Data classification and filtering
   - Audit logging and access tracking
   - Privacy controls for personal and sensitive data

## Security Model

### Role-Based Access Control (RBAC)

1. **Agent**  
   - Full access to assigned contacts' data  
   - Personal data access based on relationship context  
   - Tenant-scoped data isolation  
   - Privacy filtering based on data classification

2. **Admin**
   - Organization-wide data access
   - Tenant administration capabilities
   - Privacy controls for sensitive personal data
   - Audit logging and compliance oversight

3. **AIWorkflow**
   - System-level access for automation
   - Tenant and role-appropriate data access
   - Privacy-preserving by design
   - Strict audit logging

4. **Collaborator**
   - Limited access to shared contacts
   - Business context only (no personal data)
   - Tenant-scoped data visibility
   - Privacy controls enforced

### Privacy and Data Classification

- **Data Classification Levels**:  
  - `Personal`: Individual-identifiable information  
  - `Business`: Operational and transactional data  
  - `Confidential`: Sensitive business information  
  - `Restricted`: Highly sensitive data with strict access controls

- **Privacy Controls**:
  - Automatic filtering based on user role and data classification
  - Tenant isolation enforced at data access layer
  - Audit logging for all data access and filtering operations
  - Compliance with GDPR, CCPA, and SOC2 requirements

### Tenant Isolation

- Each tenant represents an industry vertical (Real Estate, Mortgage, etc.)
- Data isolation enforced at database and service layers
- Tenant-specific configurations and business rules
- Industry-appropriate data handling and privacy controls

## Data Models

### SqlContextData

Root context object containing role-specific data with security metadata:

```csharp
public class SqlContextData
{
    // Core metadata
    public string ContextType { get; set; }  // 'agent', 'admin', or 'aiworkflow'
    public string SchemaVersion { get; set; } // Semantic version
    public DateTime GeneratedAt { get; set; }
    
    // Role-specific context data (mutually exclusive)
    public AgentContext Agent { get; set; }
    public AdminContext Admin { get; set; }
    public AIWorkflowContext AIWorkflow { get; set; }
    
    // Security and audit metadata
    public SecurityMetadata Security { get; set; }
}

public class SecurityMetadata
{
    public Guid RequestingAgentId { get; set; }
    public string RequestingRole { get; set; }
    public Guid TenantId { get; set; }
    public string DataClassification { get; set; } // 'Personal', 'Business', 'Confidential'
    public List<string> AppliedFilters { get; set; } = new();
}
```

### Agent Context

Context specific to agent users:

```csharp
public class AgentContext
{
    // Contact information (filtered by privacy rules)
    public ContactProfile Contact { get; set; }
    
    // Relationship context
    public RelationshipContext Relationship { get; set; }
    
    // Business context
    public BusinessContext Business { get; set; }
    
    // Communication preferences and consent
    public CommunicationContext Communication { get; set; }
    
    // Recent activities and interactions
    public List<Interaction> RecentInteractions { get; set; } = new();
    
    // Upcoming tasks and appointments
    public List<Activity> UpcomingActivities { get; set; } = new();
    
    // Assigned service providers (contact-centric)
    public List<ContactAssignment> AssignedServiceProviders { get; set; } = new();
}

public class ContactProfile
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; } // May be filtered based on privacy
    public List<ContactTag> Tags { get; set; } = new();
    // Additional properties with appropriate privacy controls
}
```

### Admin Context

Context for administrative users with organization-wide visibility:

```csharp
public class AdminContext
{
    // Organization details
    public OrganizationProfile Organization { get; set; }
    
    // Team and user management
    public List<Team> Teams { get; set; } = new();
    public List<AgentProfile> Agents { get; set; } = new();
    
    // Performance metrics
    public PerformanceMetrics Metrics { get; set; }
    
    // Compliance and audit
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
    
    // Tenant settings and configurations
    public TenantSettings Settings { get; set; }
}
```

### AI Workflow Context

Context for automated workflows and AI processing:

```csharp
public class AIWorkflowContext
{
    // Workflow execution context
    public string WorkflowId { get; set; }
    public string WorkflowType { get; set; }
    public DateTime StartedAt { get; set; }
    
    // Contact and interaction context
    public LightweightContact Contact { get; set; }
    public List<Interaction> RelatedInteractions { get; set; } = new();
    
    // Available actions and constraints
    public List<WorkflowAction> AvailableActions { get; set; } = new();
    public List<WorkflowConstraint> Constraints { get; set; } = new();
    
    // Privacy and compliance controls
    public bool IsPrivacySensitive { get; set; }
    public List<string> AppliedPrivacyFilters { get; set; } = new();
}
```

### Privacy and Access Control

```csharp
// Example of privacy-aware data access
public async Task<SqlContextData> ExtractContextAsync(
    Guid contactId, 
    Guid requestingAgentId, 
    UserRole requestingRole = UserRole.Agent,
    CancellationToken cancellationToken = default)
{
    // 1. Validate tenant context
    var tenantContext = await _tenantService.GetCurrentTenantAsync();
    if (tenantContext == null)
    {
        throw new UnauthorizedAccessException("No valid tenant context found");
    }

    // 2. Validate access
    var hasAccess = await ValidateContactAccessAsync(contactId, requestingAgentId, requestingRole);
    if (!hasAccess)
    {
        throw new UnauthorizedAccessException($"Access denied to contact {contactId}");
    }

    // 3. Create context with security metadata
    var contextData = new SqlContextData
    {
        ContextType = requestingRole.ToString().ToLower(),
        SchemaVersion = "1.0",
        GeneratedAt = DateTime.UtcNow,
        Security = new SecurityMetadata
        {
            RequestingAgentId = requestingAgentId,
            RequestingRole = requestingRole.ToString(),
            TenantId = tenantContext.TenantId,
            AppliedFilters = new List<string>(),
            DataClassification = "Business"
        }
    };

    // 4. Extract role-specific context
    switch (requestingRole)
    {
        case UserRole.Agent:
            contextData.Agent = await ExtractAgentContextAsync(requestingAgentId, contactId);
            contextData.Security.DataClassification = DetermineAgentDataClassification(contactId, requestingAgentId);
            break;

        case UserRole.Admin:
            contextData.Admin = await ExtractAdminContextAsync(requestingAgentId);
            contextData.Security.DataClassification = "Business";
            break;

        case UserRole.AIWorkflow:
            contextData.AIWorkflow = await ExtractAIWorkflowContextAsync(contactId, requestingAgentId);
            contextData.Security.DataClassification = "Business";
            break;

        default:
            throw new ArgumentException($"Unsupported role: {requestingRole}");
    }

    return contextData;
}
```

## Integration Points

### NBA Context Service Integration
```csharp
public async Task<NbaContext> GetNbaContextAsync(
    Guid contactId, 
    Guid organizationId, 
    Guid requestingAgentId,  // NEW: For security filtering
    int maxRecentInteractions = 5, 
    int maxRelevantInteractions = 10,
    bool includeSqlContext = true)  // NEW: Control SQL context inclusion
```

### AI Prompt Integration
The SQL context data is serialized to JSON and can be injected into AI prompts alongside:
- Rolling summaries
- Vector search results  
- Recent interactions
- Contact state

## Usage Examples

### Extract Contact Context (Assigned Agent)
```csharp
var sqlContext = await _sqlContextExtractor.ExtractContactContextAsync(
    contactId, organizationId, assignedAgentId, includePersonalData: true);

// Full access - includes personal preferences and private data
```

### Extract Contact Context (Collaborator)
```csharp
var sqlContext = await _sqlContextExtractor.ExtractContactContextAsync(
    contactId, organizationId, collaboratorId, includePersonalData: true);

// Limited access - personal data filtered, business data only
```

### NBA Context with SQL Integration
```csharp
var nbaContext = await _nbaContextService.GetNbaContextAsync(
    contactId, organizationId, requestingAgentId, includeSqlContext: true);

// Comprehensive context including SQL data with security filtering
```

## Compliance and Audit

### Data Privacy Compliance
- GDPR: Right to access, portability, erasure
- CCPA: Consumer privacy rights
- SOC2: Security controls and audit trails

### Audit Logging
- All extraction operations logged with agent ID and timestamp
- Security filter applications tracked
- Failed access attempts recorded
- Data access patterns monitored

## Performance Considerations

### Optimization Strategies
1. **Caching**: Consider caching frequently accessed context data
2. **Lazy Loading**: Load context components on-demand
3. **Batch Processing**: Aggregate multiple context requests
4. **Indexing**: Ensure proper database indexes for query performance

### Monitoring
- Track extraction performance and latency
- Monitor SQL context usage patterns
- Alert on security violations or unusual access patterns

## Testing

### Integration Tests
- `SqlContextExtractorTests.cs` provides comprehensive test coverage
- Tests security filtering for different user roles
- Validates JSON serialization and deserialization
- Verifies organization and resource context extraction

### Test Scenarios
1. Assigned agent full access
2. Collaborator limited access  
3. JSON serialization/deserialization
4. Organization context extraction
5. Resource context extraction

## Future Enhancements

### Planned Improvements
1. **Real-time Metrics**: Live calculation of engagement scores and trends
2. **Advanced Analytics**: ML-powered insights and predictions
3. **Custom Fields**: Support for organization-specific data fields
4. **Workflow Integration**: Context-aware workflow recommendations
5. **Performance Optimization**: Caching and query optimization

### Integration Opportunities
1. **Vector Search Enhancement**: Use SQL context to improve semantic search
2. **AI Model Training**: Use structured context for model fine-tuning
3. **Reporting and Analytics**: Aggregate context data for business intelligence
4. **External Integrations**: Sync context with CRM and marketing platforms

## Dependencies

### Required Packages
- Entity Framework Core (database access)
- Microsoft.Extensions.Logging (audit logging)
- System.Text.Json (JSON serialization)

### EMMA Platform Dependencies
- EMMA Data Dictionary (compliance rules)
- Existing domain models (Contact, Interaction, etc.)
- Role-based access control system
- Audit and compliance framework

## Deployment Notes

### Database Considerations
- Ensure proper indexes on frequently queried fields
- Consider read replicas for context extraction queries
- Monitor query performance and optimize as needed

### Security Configuration
- Validate role-based access control rules
- Configure audit logging levels
- Set up monitoring for security violations

### Performance Tuning
- Profile context extraction performance
- Implement caching where appropriate
- Monitor memory usage for large context objects

---

This implementation provides a secure, compliant foundation for integrating comprehensive SQL context into EMMA's AI-driven NBA platform while maintaining strict privacy controls and audit capabilities.
