# Emma AI Platform — Technical Debt & Design Decisions

Document all technical debt, design decisions, and architectural notes here for the Emma AI Platform. Reference related TODOs as needed.

## SQL Data Access Implementation - Hardcoded Values & Stubs

### SqlContextExtractor Service (Emma.Core/Services/SqlContextExtractor.cs)

**Agent Context Extraction (`ExtractAgentContextAsync`):**
- **Contact Access Validation**: `ValidateContactAccessAsync` currently allows all contacts for demo purposes (line ~90)
  - Production logic needed for ownership/collaboration-based access control
- **Contact Preferences**: All hardcoded defaults (lines 129-136):
  - `PreferredContactMethod = "Email"` - should be from contact profile data
  - `BestTimeToContact = "Business Hours"` - should be from contact preferences
  - `EmailOptIn = true` - should be from consent management
  - `SmsOptIn = true` - should be from consent management
  - `DoNotContact = false` - should be from contact preferences
- **Recent Interactions**: Empty list placeholder (line 140)
- **Tasks**: Empty list placeholder (line 143)
- **Agent Performance**: Hardcoded values (lines 146-153):
  - `InteractionsThisWeek = 0` - needs calculation from interaction data
  - `ConversionRate = 0.25m` - placeholder value, needs real calculation
  - `TasksCompleted = 0` - needs calculation from task data
  - `TasksOverdue = 0` - needs calculation from task data

**Admin Context Extraction (`ExtractAdminContextAsync`):**
- **Organization KPIs**: Hardcoded values (lines 185-189):
  - `TotalInteractionsThisWeek = 0` - needs calculation from interaction data
  - `ActiveAgents = 1` - hardcoded, needs real count
  - `InteractionsByType = new Dictionary<string, int>()` - empty, needs population
- **Agent Summaries**: Hardcoded values (lines 201-206):
  - `LastLogin = null` - needs login audit tracking
  - `AssignedContacts = 0` - needs calculation from assignments
  - `ConversionRate = 0` - needs calculation from performance data
  - `InteractionsThisWeek = 0` - needs calculation from interaction data
- **Audit Logs**: Empty list (line 210)
- **System Health**: All hardcoded values (lines 213-218):
  - `DatabaseStatus = "Healthy"` - needs real health check
  - `ApiResponseTime = 150` - needs real monitoring
  - `ActiveConnections = 25` - needs real connection count
  - `ErrorRate = 0.02` - needs real error tracking
- **Subscription Info**: All hardcoded values (lines 221-226):
  - `PlanName = "Professional"` - needs real subscription data
  - `UsersLimit = 50` - needs real plan limits
  - `CurrentUsers = 5` - needs real user count
  - `ExpiresAt = DateTime.UtcNow.AddMonths(1)` - needs real expiration

**AI Workflow Context Extraction (`ExtractAIWorkflowContextAsync`):**
- **Deal Data**: Always null (line 269) - needs deal integration
- **Agent Phone**: Always null (line 275) - needs agent profile data
- **Workflow Triggers**: Empty list (line 277) - needs workflow rule population

**Data Classification**: Hardcoded to "Business" (line 302) - needs real classification logic

### TenantContextService (Emma.Core/Services/TenantContextService.cs)

**Tenant Context**: All hardcoded demo values (lines 22-28):
- `TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001")` - demo GUID
- `TenantName = "Demo Organization"` - hardcoded name
- `DatabaseConnectionString = "DefaultConnection"` - hardcoded connection
- `IsActive = true` - always true

**Industry Profile**: Hardcoded to "RealEstate" (line 37) - needs tenant-based selection

**Access Validation**: Always returns true (line 44) - needs real validation logic

### NbaContextService Method Mismatch

**Interface Mismatch** (Emma.Api/Services/NbaContextService.cs, line 81):
- Calls `ExtractContactContextAsync(contactId, organizationId, requestingAgentId, includePersonalData: true)`
- But `ISqlContextExtractor` only defines `ExtractContextAsync(contactId, requestingAgentId, requestingRole, cancellationToken)`
- Method signature mismatch needs resolution

## Legacy Issues

- Example: Legacy privacy/business logic tags ('CRM', 'PERSONAL', 'PRIVATE') are still present on Contact entities for backward compatibility. See TODO.md for migration tracking.
- Example: Secrets are still managed via docker-compose.yml due to migration risk. See TODO.md for future migration plan.

## Priority Resolution Order

1. **High Priority - Security & Access Control**:
   - Implement real contact access validation
   - Fix NbaContextService method signature mismatch
   - Replace hardcoded tenant validation

2. **Medium Priority - Data Population**:
   - Populate recent interactions and tasks with real data
   - Calculate real performance metrics and KPIs
   - Implement real system health monitoring

3. **Low Priority - Enhancements**:
   - Add real contact preferences and consent management
   - Implement workflow triggers and deal integration
   - Add comprehensive audit logging
