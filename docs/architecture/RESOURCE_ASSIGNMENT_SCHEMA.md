# RESOURCE_ASSIGNMENT_SCHEMA.md (OBSOLETE)

## 🚨 This schema is obsolete and should not be used in new development. 🚨

### Unified Contact-Centric Model (Effective v3.0+)

- **All resources, service providers, and agents are now represented as `Contact` entities** in the EMMA platform.
- The previous `Resource`, `ResourceAssignment`, and `ResourceRecommendation` entities have been **deprecated** and replaced by:
  - `Contact` (with `RelationshipState.ServiceProvider`, `Agent`, etc.)
  - `ContactAssignment` (links client contacts to service contacts/providers)
  - `ContactCollaborationType` (for assignment/collaboration semantics)

### Migration Guidance
- **Remove/replace all code and documentation** referencing `Resource`, `ResourceAssignment`, or `ResourceRecommendation`.
- **Use the canonical `Contact` and `ContactAssignment` models** as defined in `docs/architecture/UNIFIED_SCHEMA.md`.
- For recommendations, use lightweight result objects or context models based on `Contact`.

### References
- See [`UNIFIED_SCHEMA.md`](./UNIFIED_SCHEMA.md) for the latest canonical entity definitions and relationships.
- See [`EMMA-LEXICON.md`](../EMMA-LEXICON.md) for standardized terminology.
- See migration notes in [`TERMINOLOGY-MIGRATION-GUIDE.md`](../TERMINOLOGY-MIGRATION-GUIDE.md) for details on the transition from resource-centric to contact-centric models.

---

**Summary:**
- ❌ Do NOT use ResourceAssignment or Resource entities.
- ✅ Use Contact, ContactAssignment, and ContactCollaborationType.
- ✅ All provider/service/agent roles are modeled as Contacts with appropriate RelationshipState.

This file is retained for historical reference only and should not be used for implementation or design decisions.


## Overview

The Resource Assignment system enables real estate agents to manage and assign service providers (resources) to their clients based on expressed needs during conversations. This system tracks recommendations, assignments, and outcomes to improve future recommendations.

## Core Entities

### 1. ResourceCategory
Categorizes different types of service providers.

**Key Fields:**
- `Name` - Category name (e.g., "Mortgage Lender/Broker", "Building Inspector")
- `Description` - Detailed description of the category
- `IconName` - UI icon identifier
- `SortOrder` - Display order in UI
- `IsActive` - Whether category is currently available

**Default Categories:**
- Mortgage Lender/Broker
- Building Inspector  
- Real Estate Lawyer
- Collaborator (team members)
- Title Company
- Appraiser

### 2. Resource
Individual service providers within categories.

**Key Fields:**
- `CategoryId` - Links to ResourceCategory
- `OrganizationId` - Which organization owns this resource
- `Name` - Person or company name
- `CompanyName` - Company they work for
- `Email`, `Phone`, `Website` - Contact information
- `Address` - Physical address
- `LicenseNumber` - Professional license (if applicable)
- `Specialties` - Areas of expertise (e.g., "Residential", "Commercial")
- `ServiceAreas` - Geographic coverage
- `RelationshipType` - "Preferred", "Partner", "Referral", "Team Member"
- `Rating` - 1-5 star rating
- `IsPreferred` - Show first in recommendations
- `AgentId` - Links to Agent table for collaborator resources

### 3. ResourceAssignment
Tracks when clients are assigned specific resources.

**Key Fields:**
- `ContactId` - The client/lead receiving the assignment
- `ResourceId` - The assigned resource
- `AssignedByAgentId` - Agent who made the assignment
- `Purpose` - Why assigned (e.g., "Mortgage pre-approval", "Home inspection")
- `Status` - "Active", "Completed", "Cancelled", "On Hold"
- `InteractionId` - Links to conversation where need was expressed
- `ClientRequest` - What client specifically asked for
- `WasUsed` - Did client actually use this resource?
- `ClientRating` - Client's rating of the resource
- `ClientFeedback` - Client's comments

### 4. ResourceRecommendation
Tracks what resources were recommended vs. what was chosen.

**Key Fields:**
- `ContactId` - Client who received recommendations
- `ResourceId` - One of the recommended resources
- `InteractionId` - Conversation where recommendation was made
- `RecommendationOrder` - 1st, 2nd, 3rd choice
- `WasSelected` - Did client choose this recommendation?
- `WasContacted` - Did client contact this resource?
- `AlternativeResourceName` - If client chose someone else
- `WhyAlternativeChosen` - Client's reason for choosing alternative

## Business Workflow

### 1. Need Expression
- Client expresses need during conversation: "I need a mortgage broker"
- System captures this in an Interaction record
- Agent identifies the need and resource category required

### 2. Recommendation
- Agent searches available Resources by category
- System shows preferred/highly-rated resources first
- Agent selects 2-3 resources to recommend
- System creates ResourceRecommendation records for each

### 3. Client Selection
- Client contacts recommended resources
- Client selects one (may be from list or alternative)
- Agent updates ResourceRecommendation records with outcomes
- System creates ResourceAssignment for selected resource

### 4. Tracking & Follow-up
- Agent tracks assignment status and completion
- Client provides feedback and rating
- System learns from outcomes to improve future recommendations

## Key Relationships

```
Organization
├── Resources (owns)
├── Agents
└── ResourceAssignments

Agent
├── CreatedResources (created)
├── AssignedResources (assigned to clients)
└── RecommendedResources (recommended to clients)

Contact
├── ResourceAssignments (assigned resources)
└── ResourceRecommendations (received recommendations)

ResourceCategory
└── Resources (categorized)

Interaction
├── ResourceAssignments (context for assignment)
└── ResourceRecommendations (context for recommendation)
```

## Database Indexes

**Performance Optimizations:**
- `Resource`: `(OrganizationId, Name, CategoryId)` for organization resource searches
- `ResourceAssignment`: `(ContactId, Status)` for client assignment queries
- `ResourceAssignment`: `(OrganizationId, AssignedAt)` for organization reporting
- `ResourceRecommendation`: `(ContactId, RecommendedAt)` for client history
- `ResourceRecommendation`: `(ResourceId, WasSelected)` for resource performance

## Usage Examples

### Adding a New Resource
```csharp
var mortgageBroker = new Resource
{
    CategoryId = mortgageCategoryId,
    OrganizationId = agentOrganizationId,
    Name = "John Smith",
    CompanyName = "ABC Mortgage",
    Email = "john@abcmortgage.com",
    Phone = "555-1234",
    Specialties = new List<string> { "First-time buyers", "FHA loans" },
    ServiceAreas = new List<string> { "Downtown", "Suburbs" },
    RelationshipType = ResourceRelationshipType.Preferred,
    IsPreferred = true,
    CreatedByAgentId = agentId
};
```

### Recording a Recommendation
```csharp
var recommendation = new ResourceRecommendation
{
    ContactId = clientId,
    ResourceId = mortgageBrokerId,
    RecommendedByAgentId = agentId,
    InteractionId = interactionId,
    Purpose = "First-time buyer mortgage pre-approval",
    RecommendationOrder = 1,
    RecommendationNotes = "Specializes in first-time buyers, great rates"
};
```

### Creating an Assignment
```csharp
var assignment = new ResourceAssignment
{
    ContactId = clientId,
    ResourceId = selectedResourceId,
    AssignedByAgentId = agentId,
    Purpose = "Mortgage pre-approval",
    Status = ResourceAssignmentStatus.Active,
    InteractionId = interactionId,
    ClientRequest = "Need help getting pre-approved for mortgage"
};
```

## Future Enhancements

1. **AI-Powered Matching**: Use client preferences and past outcomes to automatically suggest best resources
2. **Performance Analytics**: Track resource success rates and client satisfaction
3. **Integration APIs**: Connect with external resource databases
4. **Automated Follow-ups**: System-generated reminders for assignment status updates
5. **Resource Availability**: Calendar integration for scheduling-dependent resources
