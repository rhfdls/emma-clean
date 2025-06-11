# SQL Context Integration for EMMA's AI NBA Platform

## Overview

This document describes the implementation of secure SQL context extraction for EMMA's AI-driven Next Best Action (NBA) platform. The system extracts structured SQL data, converts it into JSON context objects, and enforces strict role-based access and privacy controls for AI consumption.

## Architecture

### Core Components

1. **ISqlContextExtractor Interface** (`Emma.Core.Interfaces`)
   - Defines the contract for SQL data extraction and security filtering
   - Methods for contact, organization, and resource context extraction

2. **SqlContextModels** (`Emma.Core.Models`)
   - Comprehensive data models representing structured business context
   - Includes security metadata and privacy controls
   - Designed for JSON serialization for AI prompt injection

3. **SqlContextExtractor Service** (`Emma.Core.Services`)
   - Implements secure SQL data extraction with EF Core
   - Enforces role-based access control and privacy filtering
   - Applies EMMA data dictionary compliance rules

4. **Enhanced NbaContext** (`Emma.Data.Models`)
   - Updated to include SQL context data alongside existing context
   - Enhanced metadata tracking for security and audit

5. **Updated NbaContextService** (`Emma.Api.Services`)
   - Integrates SQL context extraction into NBA workflow
   - Handles security filtering and error handling

## Security Model

### Access Control Levels

1. **Assigned Agent** - Full access including personal data
2. **Collaborator** - Business data only, personal data filtered
3. **Admin** - Organizational boundaries respected
4. **No Access** - Minimal context returned

### Privacy Filtering

- **Personal Tags**: `Personal`, `Private`, `Confidential`
- **Business Tags**: `Business`, `CRM`
- Automatic filtering based on user role and relationship to contact
- Audit logging for all extraction and filtering operations

## Data Models

### SqlContextData (Contact Context)
```csharp
{
  "contactId": "guid",
  "organizationId": "guid", 
  "contactProfile": {
    "basicInfo": { /* name, email, phone, etc */ },
    "preferences": { /* personal preferences - filtered by role */ },
    "tags": [ /* filtered based on access level */ ]
  },
  "relationshipContext": {
    "relationshipState": "Lead|Client|ServiceProvider",
    "assignedAgentId": "guid",
    "collaborators": [ /* agent relationships */ ]
  },
  "businessActivity": {
    "totalInteractions": 0,
    "sentimentTrend": "Positive|Neutral|Negative",
    "buyingSignals": [ /* detected signals */ ],
    "urgencyLevel": "Low|Medium|High"
  },
  "communicationContext": {
    "consentStatus": { /* email, sms, phone, marketing */ },
    "doNotContact": false,
    "preferredChannels": [ /* communication preferences */ ]
  },
  "transactionContext": {
    "activeDeals": [ /* current opportunities */ ],
    "pastTransactions": [ /* transaction history */ ],
    "lifetimeValue": 0
  },
  "resourceContext": {
    "assignedResources": [ /* current service providers */ ],
    "serviceHistory": [ /* past service engagements */ ]
  },
  "securityLevel": "Personal|Business|Confidential",
  "appliedFilters": [ /* audit trail of applied filters */ ]
}
```

### OrganizationContextData
- Organization profile and industry information
- Team structure and agent distribution
- Performance metrics and KPIs
- Industry-specific workflows and requirements

### ResourceContextData
- Available service providers and resources
- Resource ratings, specialties, and availability
- Usage metrics and success rates
- Preferred provider relationships

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
