# SQL Context Security Filtering Example

This document demonstrates how the `SqlContextExtractor` enforces the same security and privacy restrictions as the underlying SQL data when creating JSON context for AI consumption.

## Security Principle

**The JSON schema inherits and is governed by the same security, privacy restrictions at the user and interaction level as the source SQL data.**

## Role-Based Access Control

### 1. Assigned Agent (Full Access)
```json
{
  "contactId": "c1234567-89ab-cdef-0123-456789abcdef",
  "securityLevel": "Personal",
  "appliedFilters": [],
  "contactProfile": {
    "name": "Jane Smith",
    "email": "jane.smith@email.com",
    "phone": "+1-555-0123",
    "tags": ["LEAD", "HIGH_VALUE", "PERSONAL", "CONFIDENTIAL"],
    "segments": ["VIP_CLIENT", "LUXURY_BUYER"],
    "preferences": {
      "communicationMethod": "Text",
      "bestTimeToCall": "Evening",
      "personalNotes": "Prefers informal communication"
    }
  },
  "businessActivity": {
    "interactionSummary": "15 interactions including personal calls about family situation affecting timeline",
    "sentimentTrend": "Positive",
    "buyingSignals": ["Urgency", "Budget_Confirmed"]
  },
  "communicationContext": {
    "personalPreferences": {
      "avoidMorningCalls": true,
      "preferredTopics": ["Market trends", "Investment advice"]
    }
  }
}
```

### 2. Collaborator (Business Only)
```json
{
  "contactId": "c1234567-89ab-cdef-0123-456789abcdef",
  "securityLevel": "Business",
  "appliedFilters": [
    "CollaboratorFilter",
    "PersonalPreferencesFiltered",
    "PrivacyTagsFiltered",
    "PersonalInteractionsFiltered",
    "PersonalCommunicationFiltered"
  ],
  "contactProfile": {
    "name": "Jane Smith",
    "email": "jane.smith@email.com",
    "phone": "+1-555-0123",
    "tags": ["LEAD", "BUSINESS", "CRM"],
    "segments": ["LUXURY_BUYER"],
    "preferences": null
  },
  "businessActivity": {
    "interactionSummary": "Business interactions only - personal details filtered",
    "sentimentTrend": "Positive",
    "buyingSignals": ["Urgency", "Budget_Confirmed"]
  },
  "communicationContext": {
    "personalPreferences": null
  }
}
```

### 3. No Access (Minimal Context)
```json
{
  "contactId": "c1234567-89ab-cdef-0123-456789abcdef",
  "securityLevel": "NoAccess",
  "appliedFilters": ["NoAccess"]
}
```

## Privacy Tag Filtering

### Personal Tags (Filtered for Collaborators)
- `PERSONAL` - Personal information
- `PRIVATE` - Private interactions
- `CONFIDENTIAL` - Confidential data
- `FAMILY` - Family-related information
- `HEALTH` - Health information
- `FINANCIAL_PERSONAL` - Personal financial data

### Business Tags (Allowed for Collaborators)
- `BUSINESS` - Business interactions
- `CRM` - CRM system data
- `LEAD` - Lead information
- `CLIENT` - Client data
- `PROSPECT` - Prospect information
- `TRANSACTION` - Transaction details
- `PROPERTY` - Property information
- `MARKETING` - Marketing data

## Implementation Details

### Access Verification Flow
1. **Contact Access Check**: Verify agent has permission to access contact
2. **Role Determination**: Identify if agent is assigned agent, collaborator, or admin
3. **Data Extraction**: Pull relevant SQL data based on permissions
4. **Security Filtering**: Apply role-based filters to extracted data
5. **Audit Logging**: Log all access and filtering operations

### Security Filtering Methods

```csharp
// Privacy tag filtering
private static readonly HashSet<string> PersonalTags = new()
{
    "PERSONAL", "PRIVATE", "CONFIDENTIAL", "FAMILY", "HEALTH", "FINANCIAL_PERSONAL"
};

private static readonly HashSet<string> CollaboratorAllowedTags = new()
{
    "BUSINESS", "CRM", "LEAD", "CLIENT", "PROSPECT", "TRANSACTION", "PROPERTY", "MARKETING", "TEAM"
};

// Collaborator filtering example
if (isCollaborator && !isAssignedAgent)
{
    // Filter tags to only include business/collaborator-allowed tags
    contextData.ContactProfile.Tags = contextData.ContactProfile.Tags
        .Where(tag => CollaboratorAllowedTags.Contains(tag.ToUpper()) && 
                     !PersonalTags.Contains(tag.ToUpper()))
        .ToList();
    
    // Remove personal preferences
    contextData.ContactProfile.Preferences = null;
    
    // Filter interaction summaries
    contextData.BusinessActivity.InteractionSummary = 
        "Business interactions only - personal details filtered";
}
```

## Compliance and Audit

### Audit Logging
Every context extraction operation is logged with:
- Requesting agent ID
- Target contact ID
- Security level applied
- Filters applied
- Timestamp
- Success/failure status

### Compliance Features
- **GDPR Compliance**: Personal data filtering and access controls
- **CCPA Compliance**: Privacy tag enforcement and data minimization
- **SOC2 Compliance**: Audit logging and access verification
- **Industry Standards**: Role-based access control and data masking

## Integration with AI Workflows

The filtered JSON context is injected into AI prompts alongside other context sources:

```csharp
// NBA Context Service integration
var sqlContext = await _sqlContextExtractor.ExtractContactContextAsync(
    contactId, 
    organizationId, 
    requestingAgentId, 
    includePersonalData: false);

// Context is automatically filtered based on requesting agent's permissions
var aiPrompt = $@"
CONTACT CONTEXT:
{JsonSerializer.Serialize(sqlContext, new JsonSerializerOptions { WriteIndented = true })}

RECENT INTERACTIONS:
{recentInteractions}

Provide next best action recommendation...
";
```

## Key Security Guarantees

1. **Same Access Rules**: JSON context respects identical access rules as SQL data
2. **Privacy Tag Enforcement**: Personal/Private/Confidential tags filtered per role
3. **No Data Leakage**: Collaborators never see personal data, even in AI context
4. **Audit Trail**: Complete logging of all context access and filtering
5. **Compliance Ready**: Meets GDPR, CCPA, and SOC2 requirements

This ensures that AI agents operate with the same security and privacy constraints as human users, maintaining data protection while enabling intelligent recommendations.
