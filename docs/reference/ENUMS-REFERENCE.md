# EMMA Platform Enums Reference

This document provides a comprehensive reference for all enums used throughout the EMMA platform. Each enum is documented with its purpose, possible values, and usage guidelines.

## Table of Contents

1. [Contact Management](#contact-management)
   - [RelationshipState](#relationshipstate)
   - [CollaboratorRole](#collaboratorrole)
   - [ResourceRelationshipType](#resourcerelationshiptype)

2. [Communication](#communication)
   - [CallDirection](#calldirection)
   - [CallStatus](#callstatus)
   - [MessageType](#messagetype)
   - [TranscriptionType](#transcriptiontype)

3. [Workflow & Tasks](#workflow--tasks)
   - [Priority](#priority)
   - [ResourceAssignmentStatus](#resourceassignmentstatus)

4. [Subscriptions](#subscriptions)
   - [SubscriptionStatus](#subscriptionstatus)

5. [System & Workflow](#system--workflow)
   - [ChangeType](#changetype)
   - [ApprovalState](#approvalstate)
   - [DifferenceType](#differencetype)
   - [ChangeSeverity](#changeseverity)
   - [ConflictResolution](#conflictresolution)

## Contact Management

### RelationshipState

Defines the relationship state of a contact in the EMMA system. Tracks the evolution of relationships from initial contact to various business and personal relationship states.

**Location**: `Emma.Data.Models.Contact`

| Value | Description |
|-------|-------------|
| `Lead` | Initial contact, not yet engaged |
| `Prospect` | Engaged but no active business relationship |
| `Client` | Active business relationship/transaction |
| `PastClient` | Previous client, transaction completed |
| `ServiceProvider` | Service provider (lender, inspector, contractor, etc.) |
| `Agent` | Real estate agent (team member or external) |
| `Vendor` | General business vendor or supplier |
| `Friend` | Personal relationship |
| `Family` | Family member |
| `Colleague` | Industry colleague |
| `Other` | Catch-all for undefined relationships |

### CollaboratorRole

Defines the roles that can be assigned to team members collaborating on a contact.

**Location**: `Emma.Data.Models.ContactCollaborator`

| Value | Description |
|-------|-------------|
| `BackupAgent` | Can handle all business interactions in primary agent's absence |
| `Specialist` | Has expertise in specific area (luxury, commercial, etc.) |
| `Mentor` | Senior agent mentoring junior agent |
| `Assistant` | Administrative support with limited access |
| `TeamLead` | Team leader with oversight access |
| `Observer` | Read-only access for training/oversight |

## Best Practices for Working with Enums

1. **Naming Conventions**
   - Use singular names for enums (e.g., `Priority` not `Priorities`)
   - Be specific about the enum's purpose (e.g., `CallDirection` not just `Direction`)
   - Use PascalCase for enum names and values

2. **Documentation Requirements**
   - Add XML documentation comments to the enum and its values
   - Update this reference document with the new enum's details
   - Include any business rules or constraints in the documentation

3. **Implementation Guidelines**
   - If the enum values might change or be configurable, use the dynamic enum management system
   - Add validation to handle unknown or unsupported values
   - Consider backward compatibility when modifying existing enums
   - Add appropriate null handling for nullable enum properties

4. **Testing**
   - Add unit tests for any business logic that uses the enum
   - Test edge cases and invalid values
   - Verify serialization/deserialization behavior

5. **Versioning**
   - Be cautious when modifying existing enums
   - Consider marking obsolete values with `[Obsolete]` before removing
   - Document any breaking changes

6. **Validation**
   - Validate enum values when deserializing from external sources
   - Use `Enum.IsDefined()` to check if a value is valid

7. **Serialization**
   - Consider using string serialization for better readability in APIs
   - Document serialized names if they differ from enum value names

8. **Best Practices Summary**
   - Use enums for fixed sets of related values
   - Document all enums and their values
   - Follow consistent naming conventions
   - Handle unknown values gracefully
   - Consider using the dynamic enum system for configurable values
   - Test thoroughly, especially when making changes


## Resource Relationship Types

### ResourceRelationshipType

Defines the type of relationship between resources (e.g., vendors, contractors) and the system.

**Location**: `Emma.Data.Enums.ResourceRelationshipType`

| Value | Description |
|-------|-------------|
| `Preferred` | Preferred vendor/contractor |
| `Partner` | Business partner |
| `Referral` | Source of referrals |
| `TeamMember` | Member of the team |
| `Vendor` | General vendor |
| `Contractor` | External contractor |

## Communication

### CallDirection

Indicates the direction of a phone call.

**Location**: `Emma.Data.Enums.CallDirection`

| Value | Description |
|-------|-------------|
| `Inbound` | Incoming call |
| `Outbound` | Outgoing call |

### CallStatus

Represents the status of a phone call.

**Location**: `Emma.Data.Enums.CallStatus`

| Value | Description |
|-------|-------------|
| `Completed` | Call was successfully completed |
| `Missed` | Call was not answered |
| `Voicemail` | Call went to voicemail |
| `Failed` | Call failed to connect |

### MessageType

Defines the type of message in the system.

**Location**: `Emma.Data.Enums.MessageType`

| Value | Description |
|-------|-------------|
| `Text` | SMS/Text message |
| `Call` | Phone call |
| `Email` | Email message |

### TranscriptionType

Specifies the type of transcription for call recordings.

**Location**: `Emma.Data.Enums.TranscriptionType`

| Value | Description |
|-------|-------------|
| `Full` | Complete transcription |
| `Partial` | Partial or summary transcription |

## Workflow & Tasks

### Priority

Indicates the priority level of tasks or items.

**Location**: `Emma.Data.Enums.Priority`

| Value | Description |
|-------|-------------|
| `Low` | Low priority |
| `Normal` | Normal/default priority |
| `High` | High priority |
| `Urgent` | Urgent, requires immediate attention |

### ResourceAssignmentStatus

Tracks the status of resource assignments.

**Location**: `Emma.Data.Enums.ResourceAssignmentStatus`

| Value | Description |
|-------|-------------|
| `Active` | Assignment is currently active |
| `Completed` | Assignment has been completed |
| `Cancelled` | Assignment was cancelled |
| `OnHold` | Assignment is on hold |
| `Expired` | Assignment has expired |

## Subscriptions

### SubscriptionStatus

Represents the status of a subscription.

**Location**: `Emma.Data.Enums.SubscriptionStatus`

| Value | Description |
|-------|-------------|
| `Active` | Subscription is active |
| `Paused` | Subscription is temporarily paused |
| `Cancelled` | Subscription has been cancelled |

## System & Workflow

### ChangeType

Tracks the type of change made to system entities for audit logging.

**Location**: `Emma.Core.Models.EnumModels`

| Value | Description |
|-------|-------------|
| `Create` | New entity created |
| `Update` | Existing entity modified |
| `Delete` | Entity deleted |
| `Rollback` | Change reverted to previous state |
| `Approve` | Change approved |
| `Reject` | Change rejected |
| `Import` | Data imported |
| `Export` | Data exported |

### ApprovalState

Represents the approval state of changes in the system.

**Location**: `Emma.Core.Models.EnumModels`

| Value | Description |
|-------|-------------|
| `Pending` | Awaiting approval |
| `Approved` | Change approved |
| `Rejected` | Change rejected |
| `AutoApproved` | Change automatically approved by system |
| `RequiresReview` | Change needs manual review |

### DifferenceType

Used in version comparison to categorize differences between versions.

**Location**: `Emma.Core.Models.EnumModels`

| Value | Description |
|-------|-------------|
| `Added` | New item added |
| `Removed` | Item removed |
| `Modified` | Existing item changed |
| `Moved` | Item moved to different location |
| `Renamed` | Item renamed |

### ChangeSeverity

Indicates the impact level of a change.

**Location**: `Emma.Core.Models.EnumModels`

| Value | Description |
|-------|-------------|
| `Minor` | Low impact change |
| `Major` | Significant change |
| `Breaking` | Change that breaks compatibility |
| `Critical` | High-impact change requiring immediate attention |

### ConflictResolution

Defines how conflicts should be handled during data operations.

**Location**: `Emma.Core.Models.EnumModels`

| Value | Description |
|-------|-------------|
| `Skip` | Skip the conflicting item |
| `Overwrite` | Overwrite existing data |
| `Rename` | Rename the new item |
| `Merge` | Merge changes with existing data |
| `Prompt` | Prompt user for resolution |

## Usage Guidelines

1. **Consistency**: Always use these enums instead of string literals for the defined values.
2. **Validation**: When accepting enum values, validate against the defined values.
3. **Documentation**: Update this document when adding or modifying enums.
4. **Backward Compatibility**: Be cautious when modifying existing enum values as it may affect stored data.
5. **Localization**: Display values should be localized in the UI based on user preferences.

## Adding New Enums

1. **Choose the Right Location**:
   - For system-wide enums, place them in `Emma.Data.Enums`
   - For domain-specific enums, place them in the relevant domain model's namespace

2. **Naming Conventions**:
   - Use PascalCase for enum names
   - Use singular names for enums (e.g., `Priority` not `Priorities`)
   - Be specific about the enum's purpose (e.g., `CallDirection` not just `Direction`)

3. **Documentation Requirements**:
   - Add XML documentation comments to the enum and its values
   - Update this reference document with the new enum's details
   - Include any business rules or constraints in the documentation

4. **Implementation Guidelines**:
   - If the enum values might change or be configurable, use the dynamic enum management system
   - Add validation to handle unknown or unsupported values
   - Consider backward compatibility when modifying existing enums
   - Add appropriate null handling for nullable enum properties

5. **Testing**:
   - Add unit tests for any business logic that uses the enum
   - Test edge cases and invalid values
   - Verify serialization/deserialization behavior if the enum is used in APIs

6. **UI Considerations**:
   - Add display names for UI presentation if needed
   - Consider adding icons or colors for visual representation
   - Ensure proper localization support for user-facing enum values
