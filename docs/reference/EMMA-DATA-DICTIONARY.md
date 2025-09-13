# EMMA Platform Data Dictionary & Lexicon

**Version**: 2.2  
**Last Updated**: 2025-06-19  
**Status**: ACTIVE - Core Development Document  

---

## Purpose

This document serves as the **authoritative source** for terminology, business rules, and data relationships within the EMMA AI platform. All development, documentation, and business discussions must align with this lexicon.

**EMMA is an AI-centric platform** that leverages Azure AI Foundry for intelligent agent orchestration, natural language processing, and automated workflow management.

---

## Core Terminology Glossary

### AI & Agent Architecture

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **EMMA AI** | The central AI orchestrator that conducts agent workflows, processes natural language requests, and manages Contact relationships | Core AI system built on Azure AI Foundry |
| **Azure AI Foundry** | Microsoft's AI platform providing pre-configured agents, natural language processing, and AI orchestration capabilities | External dependency - all AI logic delegates here |
| **Agent Orchestrator** | Service that coordinates AI agents, manages workflows, and enforces business rules | `AgentOrchestrator` class in `Emma.AI.Orchestration` |
| **AI Foundry Service** | Service layer that handles all communication with Azure AI Foundry APIs | `AIFoundryService` class implementing `IAIFoundryService` |
| **AI Agent** | Specialized autonomous agent with a specific role (NBA, Context, Workflow, Data, Governance) | Implements `IAgent` interface |
| **Agent Message Bus** | Event-driven communication channel for inter-agent communication | `AzureServiceBusMessageBus` in `Emma.AI.Messaging` |
| **Vector Store** | Database for storing and querying vector embeddings | Azure AI Search with vector search capabilities |
| **Embedding Model** | AI model that converts text/data into vector representations | Azure OpenAI's text-embedding-ada-002 or similar |
| **RAG Pipeline** | Retrieval Augmented Generation pipeline for grounding AI responses | `RagService` in `Emma.AI.RAG` |
| **Multi-Agent System** | Collection of specialized agents working together to solve complex tasks | Orchestrated by `MultiAgentOrchestrator` |
| **Industry Profile** | Configuration that customizes AI prompts and workflows for specific verticals (real estate, mortgage, financial advisory) | Tenant-specific prompt engineering |
| **Agent Capability** | Specific AI functions available through Azure AI Foundry (scheduling, property curation, emotional analysis, etc.) | Defined in Azure AI Foundry, not in codebase |
| **Agent Task** | A structured request sent to Azure AI Foundry containing system prompts, user input, and context | Input to AI processing pipeline |
| **Agent Response** | Structured output from Azure AI Foundry containing AI-generated content and suggested actions | Output from AI processing pipeline |
| **Interaction Context** | Persistent conversation state maintained across multiple AI interactions | Managed by Azure AI Foundry |
| **System Prompt** | Industry-specific instructions that guide AI behavior for tenant workflows | Dynamically generated based on tenant profile |

### Primary Entities & Roles

#### Contact

Core entity representing individuals or businesses in the system.

##### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| OrganizationId | Guid | Yes | Owning organization |
| FirstName | string | Yes | Contact's first name |
| LastName | string | Yes | Contact's last name |
| RelationshipState | RelationshipState | Yes | Current relationship state (Lead, Prospect, Client, etc.) |
| IsActiveClient | bool | Yes | Indicates if contact is an active client |
| ClientSince | DateTime? | No | When contact became a client |
| CompanyName | string? | No | For service providers |
| LicenseNumber | string? | No | Professional license number |
| Specialties | List\<string\> | No | Service provider specialties |
| ServiceAreas | List\<string\> | No | Geographic service areas |
| Rating | decimal? | No | Professional rating (1-5) |
| ReviewCount | int | No | Number of reviews |
| IsPreferred | bool | No | Preferred service provider |
| Website | string? | No | Website URL |
| AgentId | Guid? | No | Reference to Agent if applicable |
| Tags | List\<string\> | No | Segmentation tags (no privacy/business logic) |
| LeadSource | string? | No | Source of the lead |
| OwnerId | Guid? | No | Owning agent ID |
| CreatedAt | DateTime | Yes | Record creation timestamp |
| UpdatedAt | DateTime | Yes | Record last update timestamp |
| CustomFields | Dictionary\<string, string\>? | No | Extended properties |

##### Navigation Properties

- **OwnerAgent**: Reference to the agent who owns this contact
- **Organization**: Reference to the organization
- **AssignedAgent**: Reference to the assigned agent (if any)
- **Interactions**: Collection of all interactions with this contact
- **AssignedResources**: Resources assigned to this contact
  
  > OBSOLETE: Use `ContactAssignments` with `serviceProviderContactId` (a `Contact` whose `RelationshipState` is `ServiceProvider`). See `docs/architecture/UNIFIED_SCHEMA.md`.
  
- **ResourceAssignments**: Resources this contact is assigned to
  
  > OBSOLETE: Use `ContactAssignments` (contact-centric). Resource-centric models have been deprecated. See `docs/architecture/UNIFIED_SCHEMA.md` and `docs/development/TERMINOLOGY-MIGRATION-GUIDE.md`.
- **StateHistory**: History of relationship state changes
- **Collaborators**: Agents with access to this contact
- **CollaboratingOn**: Contacts this contact collaborates on

##### Business Rules

1. **Ownership**:
   - A Contact must belong to exactly one Organization
   - A Contact may have one OwnerAgent who is responsible for the relationship
   - Ownership can be transferred between agents with proper audit trail

2. **Lifecycle**:
   - Contacts progress through relationship states (Lead → Prospect → Client)
   - State changes are tracked in StateHistory
   - ClientSince is set when RelationshipState changes to 'Client'

3. **Access Control**:
   - Only the OwnerAgent and Collaborators with appropriate permissions can view or edit the Contact
   - Access to sensitive information is controlled by Interaction Tags and privacy settings

### Agent

Represents a user in the EMMA platform who can interact with contacts, manage relationships, and perform various actions within the system.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| FirstName | string | Yes | Agent's first name |
| LastName | string | Yes | Agent's last name |
| Email | string | Yes | Agent's email address (used for authentication) |
| Password | string | Yes | Hashed password |
| FubApiKey | string? | No | API key for Follow Up Boss integration |
| FubUserId | int? | No | User ID in Follow Up Boss system |
| CreatedAt | DateTime | Yes | When the agent account was created |
| IsActive | bool | Yes | Whether the agent account is active |
| OrganizationId | Guid? | No | Reference to the agent's organization |

#### Navigation Properties

- **Organization**: Reference to the agent's organization
- **Messages**: Collection of messages associated with this agent
- **Subscription**: The agent's subscription plan
- **PhoneNumber**: The agent's phone number (if configured)
- **SubscriptionAssignments**: Collection of subscription assignments
- **Contacts**: Collection of contacts owned by this agent
- **Interactions**: Collection of interactions involving this agent

#### Business Rules

1. **Authentication & Security**:
   - Email must be unique across the system
   - Passwords must be stored using secure hashing
   - API keys should be encrypted at rest

2. **Access Control**:
   - Agents can only access contacts and interactions they own or have been granted access to
   - Organization admins can manage agents within their organization

3. **Lifecycle**:
   - New agents start with IsActive = false until verified
   - Deactivated agents cannot log in but their data is preserved
   - Audit trails track all agent activities

### Organization

Represents a company, team, or business entity that uses the EMMA platform. An organization can have multiple agents and serves as a container for related data and settings.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| OrgGuid | Guid | Yes | Public GUID for onboarding/links |
| Name | string | Yes | Organization name |
| Email | string | Yes | Organization's primary email address |
| OwnerUserId | Guid | Yes | Reference to the user who owns this organization |
| PlanType | string? | No | Subscription plan identifier |
| SeatCount | int? | No | Number of licensed seats |
| IsActive | bool | Yes | Whether the organization is active |
| CreatedAt | DateTime | Yes | When the organization was created |
| UpdatedAt | DateTime | Yes | When the organization was last updated |

#### Navigation Properties

- **OwnerAgent**: Reference to the agent who owns this organization
- **Interactions**: Collection of interactions associated with this organization
- **Agents**: Collection of agents belonging to this organization
- **Subscriptions**: Collection of the organization's subscriptions

#### Business Rules

1. **Ownership & Access**:
   - Each organization must have exactly one owner (OwnerAgent)
   - The owner has full administrative rights over the organization
   - Organization data is isolated from other organizations

2. **Industry Configuration**:
   - The IndustryCode determines which EMMA profile to use
   - Different industries may have different features and workflows
   - Changing the industry may affect available functionality

3. **Integrations**:
   - Follow Up Boss integration is optional
   - API keys should be stored securely
   - Integration status should be monitored and validated

4. **Lifecycle**:
   - Organizations can be deactivated but not deleted to preserve data
   - All organization activities are logged for audit purposes
   - Subscription status affects organization capabilities

#### Invitations

##### OrganizationInvitation

Represents an invitation for an email to join an organization.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| OrganizationId | Guid | Yes | Target organization |
| Email | string | Yes | Invitee email |
| Role | string | No | Suggested org role (OrgAdmin, Member) |
| Token | string | Yes | Invitation token used in join URL |
| ExpiresAtUtc | DateTime | Yes | Expiration timestamp |
| AcceptedAtUtc | DateTime? | No | When the invite was accepted |
| RevokedAtUtc | DateTime? | No | When the invite was revoked |
| CreatedAt | DateTime | Yes | Creation timestamp |
| UpdatedAt | DateTime | Yes | Last update timestamp |

RBAC: Invitation endpoints require policy `OrgOwnerOrAdmin`.

### Subscription Model

#### Subscription

Represents a user's subscription to a specific plan with features and limits. This is the core entity that ties a user to a subscription plan and tracks their subscription status.

##### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| UserId | Guid | Yes | Reference to the user who owns this subscription |
| PlanId | Guid | Yes | Reference to the subscription plan |
| OrganizationSubscriptionId | Guid? | No | Reference to the parent organization subscription (if part of an organization) |
| StripeSubscriptionId | string? | No | External subscription ID from Stripe for billing |
| StartDate | DateTime | Yes | When the subscription started |
| EndDate | DateTime? | No | When the subscription expires (if applicable) |
| Status | SubscriptionStatus | Yes | Current status of the subscription |
| SeatsLimit | int | Yes | Maximum number of seats/users allowed |
| IsCallProcessingEnabled | bool | Yes | Whether call processing is enabled |
| CreatedAt | DateTime | Yes | When the subscription was created |
| UpdatedAt | DateTime | Yes | When the subscription was last updated |

##### Navigation Properties

- **User**: The user who owns this subscription
- **Plan**: The subscription plan details
- **OrganizationSubscription**: The parent organization subscription (if applicable)
- **UserAssignments**: Collection of user assignments for this subscription

#### OrganizationSubscription

Represents an organization's subscription to a specific plan with seat management. This allows organizations to manage multiple user subscriptions under a single plan.

##### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| OrganizationId | Guid | Yes | Reference to the organization |
| SubscriptionPlanId | Guid | Yes | Reference to the subscription plan |
| SeatsLimit | int | Yes | Maximum number of seats/users allowed |
| StripeSubscriptionId | string? | No | External subscription ID from Stripe for billing |
| StartDate | DateTime | Yes | When the subscription started |
| EndDate | DateTime? | No | When the subscription expires (if applicable) |
| Status | SubscriptionStatus | Yes | Current status of the subscription |
| CreatedAt | DateTime | Yes | When the subscription was created |
| UpdatedAt | DateTime | Yes | When the subscription was last updated |

##### Navigation Properties

- **Organization**: The organization that owns this subscription
- **SubscriptionPlan**: The subscription plan details
- **UserAssignments**: Collection of user assignments for this subscription
- **UserSubscriptions**: Collection of user subscriptions under this organization subscription

#### UserSubscriptionAssignment

Represents the assignment of a user to an organization subscription, allowing for flexible user management within organizations.

##### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| UserId | Guid | Yes | Reference to the user being assigned |
| OrganizationSubscriptionId | Guid | Yes | Reference to the organization subscription |
| SubscriptionId | Guid | Yes | Reference to the user's individual subscription |
| AssignedByUserId | Guid? | No | User who made this assignment |
| AssignedAt | DateTime | Yes | When the assignment was made |
| StartDate | DateTime? | No | When the assignment becomes active (optional) |
| EndDate | DateTime? | No | When the assignment ends (optional) |
| IsActive | bool | Yes | Whether this assignment is currently active |
| DeactivatedAt | DateTime? | No | When the assignment was deactivated |
| DeactivationReason | string? | No | Reason for deactivation |
| CreatedAt | DateTime | Yes | When the assignment was created |
| UpdatedAt | DateTime | Yes | When the assignment was last updated |

##### Navigation Properties

- **User**: The user being assigned
- **OrganizationSubscription**: The organization subscription being assigned
- **Subscription**: The user's individual subscription
- **AssignedByUser**: The user who made this assignment

#### Navigation Properties

- **Agent**: The agent who owns this subscription
- **Plan**: The subscription plan details

#### Business Rules

1. **Subscription Management**:
   - Each agent must have an active subscription to access premium features
   - Subscription status determines feature availability
   - Changes to subscription may require approval or payment processing

2. **Access Control**:
   - Features are gated based on subscription level
   - Seat limits are enforced based on subscription tier
   - Call processing can be toggled per subscription

### SubscriptionPlan

Defines a subscription plan with features and limits.

##### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| Name | string | Yes | Name of the subscription plan |

#### Navigation Properties

- **SubscriptionPlanFeatures**: Collection of features included in this plan
- **OrganizationSubscriptions**: Organizations subscribed to this plan

### OrganizationSubscription

Represents an organization's subscription to a specific plan, including usage limits and status.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| OrganizationId | Guid | Yes | Reference to the organization |
| SubscriptionPlanId | Guid | Yes | Reference to the subscription plan |
| SeatsLimit | int | Yes | Maximum number of seats allowed |
| StripeSubscriptionId | string? | No | External subscription ID from Stripe |
| StartDate | DateTime | Yes | When the subscription starts |
| EndDate | DateTime? | No | When the subscription ends (if applicable) |
| Status | SubscriptionStatus | Yes | Current status of the subscription |

#### Navigation Properties

- **Organization**: The organization that owns this subscription
- **SubscriptionPlan**: The plan details
- **AgentAssignments**: Collection of agent assignments to this subscription

### SubscriptionPlanFeature

Defines the relationship between subscription plans and the features they include.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| SubscriptionPlanId | Guid | Yes | Reference to the subscription plan |
| FeatureId | Guid | Yes | Reference to the feature |

#### Navigation Properties

- **SubscriptionPlan**: The associated subscription plan
- **Feature**: The feature included in the plan

### AgentSubscriptionAssignment

Tracks which agents are assigned to organization subscriptions, enabling seat management.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| AgentId | Guid | Yes | Reference to the agent |
| OrganizationSubscriptionId | Guid | Yes | Reference to the organization subscription |
| AssignedAt | DateTime | Yes | When the assignment was created |

#### Navigation Properties

- **Agent**: The assigned agent
- **OrganizationSubscription**: The subscription being assigned

### ContactCollaborator

Defines collaboration access between team members and client contacts, enabling team members to access business interactions while respecting privacy.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| ContactId | Guid | Yes | The client contact that the collaborator has access to |
| CollaboratorAgentId | Guid | Yes | The agent who is collaborating on this contact |
| GrantedByAgentId | Guid | Yes | The agent who granted this collaboration access |
| OrganizationId | Guid | Yes | Owning organization |
| Role | CollaboratorRole | Yes | Role defining the type and scope of collaboration |
| CanAccessBusinessInteractions | bool | Yes | Can access business interactions (CRM tagged interactions) |
| CanAccessPersonalInteractions | bool | Yes | Can access personal interactions (PERSONAL tagged interactions) |
| CanCreateInteractions | bool | Yes | Can create new interactions on behalf of the primary agent |
| CanEditInteractions | bool | Yes | Can modify existing interactions |
| CanAssignResources | bool | Yes | Can assign resources to this contact |
| CanAccessFinancialData | bool | Yes | Can view contact's financial/transaction details |
| Reason | string? | No | Optional reason for granting collaboration access |
| ExpiresAt | DateTime? | No | When collaboration access expires |
| IsActive | bool | Yes | Whether this collaboration is currently active |
| CreatedAt | DateTime | Yes | Record creation timestamp |
| UpdatedAt | DateTime | Yes | Record last update timestamp |

### Interaction

Represents any communication or touchpoint with a contact, including calls, emails, SMS, meetings, notes, and tasks.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| ContactId | Guid | Yes | Reference to the contact |
| OrganizationId | Guid | Yes | Reference to the organization |
| ContactFirstName | string | Yes | Contact's first name (denormalized) |
| ContactLastName | string | Yes | Contact's last name (denormalized) |
| CreatedAt | DateTime | Yes | When the interaction was created |
| ExternalIds | Dictionary\<string, string\>? | No | External system identifiers (FUB, HubSpot, etc.) |
| Type | string | Yes | Type of interaction (call, email, sms, meeting, note, task, other) |
| Direction | string | Yes | Direction of interaction (inbound, outbound, system) |
| Timestamp | DateTime | Yes | When the interaction occurred |
| AgentId | Guid | Yes | Reference to the agent involved |
| Content | string? | No | Message body, note content, etc. |
| Channel | string | Yes | Source channel (twilio, email, gog, crm, other) |
| Status | string | Yes | Current status (completed, pending, failed, scheduled) |
| RelatedEntities | List\<RelatedEntity\\>? | No | Other entities related to this interaction |
| Tags | List\<string\\> | No | Privacy/business logic tags (CRM, PERSONAL, PRIVATE, etc.) |
| CustomFields | Dictionary\<string, string\\>? | No | Additional custom fields |

#### Navigation Properties

- **Contact**: The contact involved in this interaction
- **Agent**: The agent involved in this interaction
- **Organization**: The organization that owns this interaction
- **Messages**: Collection of messages associated with this interaction

#### Business Rules

1. **Data Integrity & Validation**:
   - All interactions must be associated with both a contact and organization (enforced by NOT NULL constraints)
   - Timestamps must be in UTC and validated for reasonable ranges
   - Required fields (Type, Direction, Channel, Status) must be non-null and from allowed values
   - ExternalIds must include source system and be validated against integration schemas

2. **Privacy & Access Control**:
   - Access to interactions is controlled by privacy tags (PERSONAL, PRIVATE, CRM) and ContactCollaborator permissions
   - Personal interactions (PERSONAL tag) require explicit user override even for assigned agents
   - All access is logged to AccessAuditLog with trace ID and purpose
   - Field-level masking applied based on user role and privacy tags

3. **Responsible AI Compliance**:
   - All AI-generated content must be validated through IAgentActionValidator
   - User overrides are tracked with full audit trail (who, when, why)
   - AI confidence scores and reasoning must be stored with all AI-generated interactions
   - High-risk actions (e.g., financial transactions) require explicit approval

4. **Lifecycle Management**:
   - Completed interactions are immutable (enforced at database level)
   - Status transitions: Pending → InProgress → [Completed|Failed|Cancelled]
   - System-triggered interactions must include traceable workflow ID
   - Related entities (Messages, Tasks) must maintain referential integrity

5. **Performance & Scaling**:
   - Interaction content over 10KB should be offloaded to blob storage
   - Indexes on ContactId, OrganizationId, Timestamp for query performance
   - Partitioning by OrganizationId for multi-tenant isolation
   - Soft delete pattern with IsDeleted flag and cleanup policy

6. **Integration Rules**:
   - External system references (e.g., FUB, CRM) must be validated and normalized
   - Webhook callbacks must be idempotent and include correlation IDs
   - Rate limiting and retry policies for external API calls
   - Synchronization state tracking for eventual consistency

### Message

Represents individual messages within an interaction, including text messages, call transcripts, and emails. Messages are the atomic units of communication that make up an interaction.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| AgentId | Guid | Yes | Reference to the agent who sent/received the message |
| Agent | Agent | No | Navigation property to the agent |
| Payload | string | Yes | The actual message content |
| AiResponse | string? | No | AI-generated response or analysis of the message |
| BlobStorageUrl | string | Yes | URL to the message content in blob storage (for large messages) |
| Type | MessageType | Yes | Type of message (Text, Call, Email, SMS) - See MessageType enum for all possible values |
| CreatedAt | DateTime | Yes | When the message was created in the system |
| OccurredAt | DateTime | Yes | When the message was originally sent/received |
| InteractionId | Guid | Yes | Reference to the parent interaction |
| Interaction | Interaction | No | Navigation property to the parent interaction |
| Transcription | Transcription? | No | Transcription of the message (for audio/video) |
| CallMetadata | CallMetadata? | No | Metadata for call messages |
| EmmaAnalyses | List\<EmmaAnalysis\\> | No | AI analysis results for this message |

#### Navigation Properties

- **Agent**: The agent who sent or received this message
- **Interaction**: The parent interaction this message belongs to
- **Transcription**: Transcription of the message (for call/audio messages)
- **CallMetadata**: Additional metadata for call messages
- **EmmaAnalyses**: Collection of AI analysis results for this message

### Message Types and Required Metadata

#### 1. Email Messages

- **Type**: `Email`
- **Required Metadata**:
  - `From` email address
  - `To`, `CC`, `BCC` recipients
  - `Subject` line
  - Email headers for tracking and threading
  - Attachments (stored in blob storage with references)

- **Processing Rules**:
  - HTML content should be sanitized
  - Attachments over 10MB should be linked rather than embedded
  - Email threading should be preserved using Message-Id and In-Reply-To headers

#### 2. SMS/Text Messages

- **Type**: `SMS`
- **Required Metadata**:
  - Sender and recipient phone numbers
  - Message direction (inbound/outbound)
  - Carrier information (if available)
  - Delivery status

- **Processing Rules**:
  - Maximum length of 1600 characters (supports concatenated SMS)
  - Media messages (MMS) should include content type and size
  - Delivery receipts should update message status

#### 3. Phone Calls

- **Type**: `Call`
- **Required Metadata**:
  - Caller and callee numbers
  - Call duration
  - Call recording URL (if recorded)
  - Call transcription (if available)
  - Call quality metrics

- **Processing Rules**:
  - Call recordings must be stored securely
  - Transcriptions should be processed asynchronously
  - Call outcomes should be captured (voicemail, answered, busy, etc.)

#### 4. Chat/Text Messages

- **Type**: `Text`
- **Required Metadata**:
  - Sender and recipient information
  - Message direction
  - Read receipts (if available)
  - Message status (sent, delivered, read)

- **Processing Rules**:
  - Support for emojis and rich media
  - Typing indicators and read receipts
  - Message threading and context preservation

### Business Rules

1. **Data Integrity & Validation**:
   - All messages must be associated with an agent and interaction
   - Payload cannot be empty or exceed 10,000 characters (use BlobStorageUrl for larger content)
   - OccurredAt must be in the past and not in the future
   - Message type must be one of the allowed values (Text, Call, Email, SMS)
   - Type-specific metadata must be provided based on message type:
     - Call: CallMetadata is required
     - Email: From/To addresses and subject are required
     - SMS: Sender/recipient numbers are required

2. **Privacy & Access Control**:
   - Inherits privacy settings from parent Interaction
   - Access to message content is controlled by Interaction's privacy tags
   - Sensitive data (e.g., PII) must be masked based on user permissions
   - All access is logged to AccessAuditLog with trace ID

3. **Responsible AI Compliance**:
   - AI-generated content (AiResponse) must be validated through IAgentActionValidator
   - AI analysis results (EmmaAnalyses) must include confidence scores and reasoning
   - User overrides for AI-generated content are tracked with full audit trail
   - High-risk AI actions require explicit approval based on configured policies

4. **Lifecycle Management**:
   - Messages are immutable once created (enforced at database level)
   - Deletion is performed via soft delete pattern with IsDeleted flag
   - Parent Interaction tracks message count and last message timestamp
   - Related entities (Transcription, CallMetadata) are cascade deleted

5. **Performance & Scaling**:
   - Messages over 1KB in size must use BlobStorageUrl
   - Indexes on InteractionId, AgentId, OccurredAt for query performance
   - Partitioned by InteractionId for efficient retrieval
   - Caching strategy for frequently accessed messages

6. **Integration Rules**:
   - External message IDs must be stored in Interaction.ExternalIds
   - Webhook notifications for new messages include correlation IDs
   - Rate limiting and retry policies for message processing
   - Synchronization state tracking for external systems

### CallMetadata

Stores metadata specific to call messages, including call direction, duration, and status. This entity is only populated for messages where `Message.Type` is `Call`.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| MessageId | Guid | Yes | Foreign key to the parent Message |
| ClientPhoneNumber | string | Yes | The phone number of the other party |
| DurationInSeconds | int | Yes | Duration of the call in seconds |
| DirectionBasedOnAgent | CallDirection | Yes | Whether the call was inbound or outbound from the agent's perspective |
| Status | CallStatus | Yes | The final status of the call (Completed, Missed, Voicemail, Failed) |
| ReferenceId | string | Yes | External reference ID from the telephony provider |
| Message | Message | No | Navigation property to the parent message |

#### Enumerations

**CallDirection**
- `Inbound`: Call was received by the agent
- `Outbound`: Call was initiated by the agent

**CallStatus**
- `Completed`: Call was successfully connected and completed
- `Missed`: Call was not answered by the agent
- `Voicemail`: Call went to voicemail
- `Failed`: Call failed due to technical issues

#### Business Rules

1. **Data Integrity & Validation**:
   - Required for all messages where `Message.Type` is `Call`
   - `ClientPhoneNumber` must be in E.164 format
   - `DurationInSeconds` must be ≥ 0
   - `ReferenceId` must be unique per telephony provider
   - `MessageId` is immutable once set

2. **Privacy & Access Control**:
   - Inherits privacy settings from parent Message
   - `ClientPhoneNumber` must be masked based on user permissions
   - Access logged to AccessAuditLog with trace ID
   - Redacted in audit logs when containing sensitive information

3. **Lifecycle Management**:
   - Created automatically when a call message is created
   - Immutable after creation to maintain call record integrity
   - Deleted when parent Message is deleted (cascade delete)

4. **Performance & Scaling**:
   - Indexed on `MessageId` for fast lookups
   - Consider archiving old call metadata for performance
   - Partitioned by organization for multi-tenant isolation

## Transcription

Stores transcriptions of message content, particularly for audio/video messages.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Unique identifier for the transcription |
| BlobStorageUrl | string | Yes | URL to the blob storage location of the transcription content |
| Type | [TranscriptionType](#transcriptiontype) | Yes | Type of transcription (Full or Partial) |
| CreatedAt | DateTime | Yes | When the transcription was created |
| MessageId | Guid | Yes | Reference to the parent message |
| Message | Message | No | Navigation property to the parent message |

### Enumerations

#### TranscriptionType

Defines the type of transcription:

| Value | Description |
|-------|-------------|
| Full | Complete transcription of the entire message content |
| Partial | Incomplete transcription, typically used for real-time or streaming transcriptions |

### Business Rules

1. **Data Integrity & Validation**:
   - `BlobStorageUrl` must be a valid URL pointing to accessible blob storage
   - `MessageId` is required and must reference an existing Message
   - `Type` must be a valid TranscriptionType value
   - `CreatedAt` is automatically set to UTC now on creation

2. **Privacy & Access Control**:
   - Inherits privacy settings from the parent Message and Interaction
   - Access to transcriptions is controlled by the parent Message's permissions
   - Sensitive information in transcriptions should be redacted based on data classification

3. **Lifecycle Management**:
   - Automatically created when a message with audio/video content is processed
   - Updated if the transcription is revised or completed
   - Soft delete pattern should be used to maintain referential integrity
   - Consider retention policies for compliance with data protection regulations

4. **Performance & Scaling**:
   - Indexed on `MessageId` for fast lookups
   - Store only metadata in the database, with content in blob storage
   - Consider implementing a cleanup job for failed or abandoned transcriptions
   - Partition transcriptions by organization for multi-tenant isolation

5. **Integration Rules**:
   - Integration with speech-to-text services should be fault-tolerant
   - Implement retry logic for transient failures in transcription services
   - Support webhook callbacks for asynchronous transcription completion
   - Include correlation IDs for tracing transcription requests

6. **Compliance & Security**:
   - Log access to transcriptions for audit purposes
   - Implement data retention policies in line with regulations
   - Consider encryption of transcription content at rest and in transit
   - Regular security reviews of blob storage access controls
   - Synchronized with speech-to-text services via webhooks
   - Includes correlation ID for tracking across systems
   - Retry policy for failed transcription requests
   - Rate limiting to prevent abuse
   - Retention policy for transcriptions based on legal requirements
   - Redaction of sensitive information in transcriptions
   - Audit trail of all access to transcription content
   - Compliance with data protection regulations (e.g., GDPR, CCPA)

## EmmaAnalysis

Stores AI-generated analysis and insights for messages, including lead status, recommended strategies, tasks, and agent assignments.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Unique identifier for the analysis |
| MessageId | Guid | Yes | Reference to the analyzed message |
| Message | Message | No | Navigation property to the parent message |
| LeadStatus | string | No | Current status of the lead as determined by analysis |
| RecommendedStrategy | string | No | Suggested strategy based on the analysis |
| TasksList | List\<EmmaTask\\> | No | Collection of tasks generated by the analysis |
| AgentAssignments | List\<AgentAssignment\\> | No | List of agent assignments related to this analysis |
| ComplianceFlags | List\<string\\> | No | Flags indicating compliance issues or considerations |
| FollowupGuidance | string | No | Guidance for follow-up actions |
| CreatedAt | DateTime | Yes | When the analysis was created (UTC) |

### Related Entities

#### EmmaTask

Represents a task generated from message analysis.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | int | Yes | Unique identifier for the task |
| TaskType | string | Yes | Type/category of the task |
| Description | string | Yes | Detailed description of the task |
| DueDate | DateTime | Yes | Deadline for task completion |

#### AgentAssignment

Represents an agent assignment resulting from message analysis.

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Unique identifier for the assignment |
| EmmaAnalysisId | Guid | Yes | Reference to the parent analysis |
| EmmaAnalysis | EmmaAnalysis | No | Navigation property to parent analysis |
| AgentId | Guid | Yes | Identifier of the assigned agent |
| AssignmentType | string | Yes | Type of assignment (e.g., "Primary", "Secondary", "Reviewer") |
| AssignedAt | DateTime | Yes | When the assignment was made (UTC) |

### Business Rules

1. **Data Integrity & Validation**:
   - `MessageId` is required and must reference an existing Message
   - `LeadStatus` must be a recognized status from the organization's configuration
   - `RecommendedStrategy` must be non-empty if provided
   - `CreatedAt` is automatically set to UTC now on creation
   - Task due dates must be in the future when created

2. **Privacy & Access Control**:
   - Inherits privacy settings from the parent Message and Interaction
   - Access to analysis results is controlled by the parent Message's permissions
   - Compliance flags trigger additional access restrictions when present
   - Agent assignments respect team membership and access controls

3. **Lifecycle Management**:
   - Created when a message is processed by the AI analysis pipeline
   - Updated when new analysis results are available
   - Deleted when the parent Message is deleted (cascade delete)
   - Historical analysis snapshots are maintained for audit purposes

4. **Performance & Scaling**:
   - Indexed on `MessageId` for fast lookups
   - Large analysis results stored in dedicated storage with metadata in the database
   - Consider implementing caching for frequently accessed analysis results
   - Partitioned by organization for multi-tenant isolation

5. **Integration Rules**:
   - Integration with AI analysis services should be fault-tolerant
   - Implement retry logic for transient failures in analysis services
   - Support webhook callbacks for asynchronous analysis completion
   - Include correlation IDs for tracing analysis requests

6. **Compliance & Security**:
   - Log access to analysis results for audit purposes
   - Implement data retention policies in line with regulations
   - Regular security reviews of analysis storage access controls
   - Compliance with data protection regulations (e.g., GDPR, CCPA)
   - Sensitive information must be redacted from analysis results

#### Navigation Properties

- **Message**: Reference to the analyzed message
- **TasksList**: Collection of tasks generated by the analysis
- **AgentAssignments**: Collection of agent assignments from the analysis
- **Contact**: Reference to the related contact
- **CollaboratorAgent**: Reference to the agent who is collaborating
- **GrantedByAgent**: Reference to the agent who granted access
- **Organization**: Reference to the organization

#### Business Rules

1. **Access Control**:
   - Only the Contact's OwnerAgent can grant collaboration access
   - Access to personal interactions is restricted by default
   - Financial data access requires explicit permission

2. **Lifecycle**:
   - Collaborations can be set to expire automatically
   - All access is revoked when IsActive is set to false
   - Changes to collaboration permissions are audited

3. **Validation**:
   - A collaborator cannot be assigned the same role multiple times for the same contact
   - The Granting agent must have appropriate permissions

### Interaction

Records all communications and events related to contacts, providing a complete audit trail of all touchpoints.

#### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| Id | Guid | Yes | Primary key |
| ContactId | Guid | Yes | Related contact |
| OrganizationId | Guid | Yes | Owning organization |
| ContactFirstName | string | Yes | Contact's first name (denormalized) |
| ContactLastName | string | Yes | Contact's last name (denormalized) |
| CreatedAt | DateTime | Yes | Record creation timestamp |
| ExternalIds | Dictionary\<string, string\>? | No | External system references |
| Type | string | Yes | Type of interaction (call\|email\|sms\|meeting\|note\|task\|other) |
| Direction | string | Yes | Direction (inbound\|outbound\|system) |
| Timestamp | DateTime | Yes | When the interaction occurred |
| AgentId | Guid | Yes | Agent involved |
| Content | string? | No | Message body or notes |
| Channel | string | Yes | Source channel (twilio\|email\|gog\|crm\|other) |
| Status | string | Yes | Current status (completed\|pending\|failed\|scheduled) |
| RelatedEntities | List\<RelatedEntity\\>? | No | Related business entities |
| Tags | List\<string\\> | No | Privacy/business logic tags |
| CustomFields | Dictionary\<string, string\>? | No | Extended properties |

#### Navigation Properties

- **Contact**: Reference to the related contact
- **Agent**: Reference to the agent involved
- **Organization**: Reference to the organization
- **Messages**: Collection of messages in this interaction

#### Business Rules

1. **Data Integrity**:
   - Every interaction must be associated with a valid Contact
   - The Timestamp must be set to when the interaction actually occurred, not when it was recorded
   - The Type must be one of the predefined values

2. **Privacy & Compliance**:
   - Interactions with sensitive information must be tagged appropriately
   - Access to interactions is controlled by the Contact's access rules
   - Certain interaction types may require additional compliance logging

3. **Lifecycle**:
   - Interactions are immutable once created (new versions should be created for updates)
   - Status transitions must follow allowed state changes

### Other Primary Entities

| Term | Definition | Implementation Notes |
| **Contact** | An individual or business entity stored in the system; can be a Lead, Client, or Personal Contact | Base entity in EF Core model - AI-centric relationship management |
| **Lead** | A Contact who has not yet become a Client | State/classification of Contact |
| **Client** | A Contact with an active or past contract with the Assigned Agent | State/classification of Contact |
| **Consumer** | The end-client; may be used to distinguish "Contact" as a business record from "Consumer" as a person purchasing or selling | Business terminology |
| **Assigned Agent** | The Agent who currently holds a contract (listing or representation) with a given Contact | Uses `AssignedAgent` property in the Contact class |
| **Collaborator** | A user (internal team member, co-agent, or outside partner) who is granted restricted access to a Contact, by the Assigned Agent | Represented by ContactCollaborator entity |
| **Contact Owner** | The Agent or Organization currently responsible for the Contact record. May differ from the Assigned Agent if the Contact is transferred | Now uses `OwnerAgent` property in the Contact class |
| **Referral Agent** | An Agent who refers a Contact to the Assigned Agent. May be a Collaborator or have view-only rights | Special type of Collaborator |
| **Resource** | A Contact with RelationshipState.ServiceProvider who provides external services (legal, inspection, contracting, etc.) that are not managed by the Organization. The Organization is not liable for service quality or legal issues | Contact with ServiceProvider state |
| **Resource Assignment** | The process of an Assigned Agent providing a selection of appropriate Resources to a Contact based on their specific needs | Tracked via ContactAssignment entity |

### Organizational Structure

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Organization** | A brokerage, real estate team, or company entity. Agents, admins, and staff belong to an Organization | Top-level entity in hierarchy |
| **Team** | A group of Agents within an Organization, potentially sharing leads/clients and collaborating | Sub-organizational grouping |
| **Admin** | A user role with elevated privileges for managing Agents, Collaborators, and system configuration | Distinct from Assigned Agent or Collaborator |

### Communication & Interactions

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Interaction** | Any logged communication or event (call, SMS, email, note, meeting, etc.) linked to a Contact | Core entity for all communications, includes `Timestamp`, `Agent`, and `Contact` properties |
| **Interaction Type** | The category of an Interaction (e.g., Call, SMS, Email, Note, Meeting, Automated) | Classification system |
| **Interaction Tag** | A classification for an Interaction. Key tags include: Business, CRM, Personal, Private, Confidential | Controls access permissions |
| **Engagement** | Any tracked touchpoint or activity with a Contact, including system-generated actions (e.g., auto-texts, follow-up reminders) | Broader than Interaction |

### Business Process Concepts

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Pipeline/Stage** | The current status of a Contact in a sales process (e.g., New Lead, Nurturing, Active Client, Under Contract, Closed, Lost) | Contact lifecycle tracking |
| **Deal/Transaction** | A specific business opportunity or closed sale, often associated with a Contact and an Assigned Agent | May have multiple Collaborators |
| **Task/Follow-Up** | A scheduled activity or to-do associated with a Contact or Deal. Can be assigned to Agents or Collaborators | Workflow management |
| **Appointment** | A scheduled meeting, call, or showing with a Contact. May involve multiple Agents or Collaborators | Calendar integration |
| **Relationship State** | The current connection between an Agent and a Contact (Lead, Active Client, Past Client, Personal, Referral, etc.) | Contact classification |

### Access Control & Privacy

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Role** | The set of permissions assigned to a user (e.g., Assigned Agent, Collaborator, Admin, etc.) | System-level access control |
| **Access Privilege** | The specific rights a user or role has with respect to viewing or editing Contact or Interaction data | Enforced at API/UI/audit levels |
| **Access Level** | The degree of access granted to a user for a given Contact, Interaction, or Deal (e.g., Full, Read-Only, Limited) | Granular permission control |
| **Visibility** | Specifies which roles/users can view a given Contact, Interaction, or field (may be Public, Organization-Only, Private) | Data exposure control |
| **Consent** | A Contact's explicit permission to receive communication (calls, SMS, email), as required by compliance (e.g., CASL, CAN-SPAM) | Legal compliance requirement |
| **Consent Status** | Tracks whether and when a Contact has opted-in or out of communication | Compliance tracking |
| **Audit Log** | A system record of access and changes to sensitive data, for compliance and review | Security and compliance |
| **Redaction** | The removal or masking of sensitive information in communications or logs, for privacy/compliance | Data protection mechanism |

### Classification & Organization

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Tag** | A label applied to Contacts, Interactions, or Deals for classification or workflow routing (e.g., VIP, Urgent, Do Not Contact) | Flexible categorization |
| **Segment** | A defined group of Contacts with shared traits (e.g., by geography, budget, engagement history) for targeted outreach or analysis | Marketing/analysis grouping |
| **Source** | The origin of a Contact or Lead (e.g., Website, Referral, Social Media, Open House) | Lead attribution |
| **Status** | The current state of a Contact, Deal, Task, or Appointment (Active, Inactive, Completed, Cancelled, etc.) | Workflow state tracking |

### System & Technical

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Custom Field** | A user-defined data point added to a Contact, Deal, or Organization record, for business-specific needs | Extensibility feature |
| **Integration** | A connection to an external system (e.g., MLS, Email, CRM, Marketing platform) for data sync and workflow automation | Third-party connectivity |

---

## CRITICAL TERMINOLOGY STANDARDIZATION

### ⚠️ DEPRECATED TERMS - DO NOT USE

| ❌ Deprecated Term | ✅ Correct Term | Reason | Migration Status |
|-------------------|-----------------|---------|------------------|
| **Conversation** | **Interaction** | Inconsistent with data models and contact-centric architecture | ✅ COMPLETE |
| **ConversationContext** | **InteractionContext** | Services must align with data model terminology | ✅ COMPLETE |
| **conversationId** | **interactionId** | Parameter naming consistency | ✅ COMPLETE |
| **ConversationInsights** | **Dictionary\<string, object\> Insights** | Flexible AI-generated data structures | ✅ COMPLETE |
| **SentimentAnalysis** | **Dictionary\<string, object\> Sentiment** | Flexible sentiment data | ✅ COMPLETE |
| **BuyingSignal** | **List<string\> BuyingSignals** | Simplified signal representation | ✅ COMPLETE |

### Terminology Enforcement Rules

1. **"Interaction" is the ONLY approved term** for communication events
2. **All new code MUST use "interaction" terminology**
3. **Existing "conversation" references MUST be migrated**
4. **Dictionary-based intelligence structures are REQUIRED** for AI-generated data
5. **No custom type objects for intelligence data** - use flexible dictionaries

---

## AI AGENT ARCHITECTURE

### Core Agent System Components

| Component | Purpose | Implementation | Dependencies |
|-----------|---------|----------------|--------------|
| **Agent Orchestrator** | Coordinates multi-agent workflows and task distribution | Azure AI Foundry integration | Context Provider, Action Validator |
| **Context Provider** | Supplies interaction context and intelligence to agents | ContextProvider.cs service | Azure AI, Entity Framework |
| **Action Validator** | Validates agent actions through Responsible AI pipeline | ActionRelevanceValidator.cs | User Override system, LLM validation |
| **Intelligence Engine** | Generates insights, sentiment, and buying signals | ContextIntelligence class | Azure AI Foundry, flexible dictionaries |
| **User Override System** | Controls agent autonomy and approval workflows | UserOverrideExtensions.cs | Action validation pipeline |

### Agent Context Flow

```
Contact Interaction → Context Provider → Agent Intelligence → Action Generation → Validation Pipeline → User Override Check → Action Execution
```

### Agent Intelligence Data Structures

| Intelligence Type | Data Structure | Purpose | Example |
|------------------|----------------|---------|---------|
| **Insights** | `Dictionary\<string, object\>` | Flexible AI-generated insights | `{"lead_quality": "high", "urgency": 0.8}` |
| **Sentiment** | `Dictionary\<string, object\>` | Sentiment analysis results | `{"overall": "positive", "confidence": 0.9}` |
| **BuyingSignals** | `List<string\>` | Detected purchase indicators | `["timeline_mentioned", "budget_discussed"]` |
| **Recommendations** | `List<string\>` | Suggested next actions | `["schedule_showing", "send_listings"]` |

### User Override Architecture

#### Override Modes

| Mode | Behavior | Use Case | Risk Level |
|------|----------|----------|------------|
| **AlwaysAsk** | Require approval for all actions | High-risk agents or sensitive contacts | HIGH |
| **NeverAsk** | Full automation, no approval needed | Trusted agents with proven reliability | LOW |
| **LLMDecision** | AI decides when to request approval | Balanced automation with safety | MEDIUM |
| **RiskBased** | Approval based on confidence thresholds | Dynamic risk assessment | VARIABLE |

#### Validation Pipeline Stages

1. **Action Relevance Check** - Validates action appropriateness for context
2. **Risk Assessment** - Evaluates potential impact and confidence levels  
3. **User Override Evaluation** - Determines if approval is required
4. **Approval Workflow** - Routes to user for review if needed
5. **Action Execution** - Executes approved or auto-approved actions
6. **Audit Logging** - Records all decisions for explainability

#### Critical Integration Points

- **ActionRelevanceRequest** must include `userOverrides` parameter
- **AgentActionValidationContext** must propagate `userOverrides` through pipeline
- **LLM prompts** must incorporate serialized `userOverrides` for decision-making
- **Approval workflows** must consider original `userOverrides` in decisions
- **Audit trail** must capture `userOverrides` for compliance and explainability

### Agent Data Classification

| Classification Level | Access Control | Audit Requirements | Override Behavior |
|---------------------|----------------|-------------------|-------------------|
| **Public** | Standard access rules | Basic logging | Standard validation |
| **Internal** | Organization-restricted | Enhanced logging | Moderate validation |
| **Confidential** | Assigned agent only | Full audit trail | Strict validation |
| **Restricted** | Admin approval required | Comprehensive audit | Always require approval |

---

## Access Control Matrix

| Data Type | Assigned Agent | Collaborator | Non-Assigned Agent | Admin |
|-----------|----------------|--------------|-------------------|-------|
| Contact Details | Full Access | Business Only | No Access | Full Access |
| Business Interactions (CRM) | Full Access | Read Access | No Access | Full Access |
| Personal Interactions | Full Access | NEVER | No Access | Limited Access |
| Confidential/Private Notes | Full Access | NEVER | No Access | Audit Only |
| Assign/Remove Collaborators | Full Control | No Access | No Access | Full Control |
| Deal/Transaction Data | Full Access | Limited Access | No Access | Full Access |
| Consent Management | Full Access | View Only | No Access | Full Access |
| Resource Assignment | Full Access | Limited Access | No Access | Full Access |

---

## Data Model Relationships

### Core Entities

#### Contact
- **Primary Entity**: Individual or business record
- **States**: Lead → Client (lifecycle progression)
- **Key Relationships**:
  - `Contact.Owner` → Assigned Agent (OwnerId FK)
  - `Contact.Agent` → Associated Agent record (AgentId FK) 
  - `Contact.Collaborators` → List of Collaborators with access
  - `Contact.Interactions` → All communications/events
  - `Contact.AssignedResources` → Resources assigned TO this contact
  - `Contact.ResourceAssignments` → When this contact IS a resource
  - `Contact.Organization` → Owning Organization
  - `Contact.Deals` → Associated business transactions

#### ContactCollaborator
- **Purpose**: Defines Collaborator access to specific Contacts
- **Key Relationships**:
  - `ContactCollaborator.Contact` → The Contact being collaborated on
  - `ContactCollaborator.CollaboratorAgent` → The Agent who is collaborating
  - `ContactCollaborator.GrantedByAgent` → The Agent who granted access
- **Access Controls**: Role-based permissions (CanAccessBusinessInteractions, etc.)

#### Agent
- **Purpose**: System users who can be assigned to Contacts
- **Key Relationships**:
  - `Agent.OwnedContacts` → Contacts where this Agent is the Owner
  - `Agent.CollaboratingOn` → ContactCollaborator records where this Agent is collaborating
  - `Agent.Organization` → Employing Organization
  - `Agent.Team` → Team membership

#### Organization
- **Purpose**: Top-level business entity (brokerage, team, company)
- **Key Relationships**:
  - `Organization.Agents` → All Agents in this Organization
  - `Organization.Contacts` → Organization-owned Contacts
  - `Organization.Teams` → Sub-organizational groups

---

## Business Rules

### CRITICAL ACCESS RULES

1. **Collaborators NEVER see Personal, Private, or Confidential interactions**
   - Enforced by Interaction Tags at API/UI/audit levels
   - No exceptions - this is a core privacy requirement

2. **Assigned Agent has full control**
   - Can view all interactions (including Personal/Private)
   - Can assign/remove Collaborators
   - Can modify access permissions

3. **Tag-based access enforcement**
   - `Business`, `CRM` → Visible to Collaborators
   - `Personal`, `Private`, `Confidential` → Assigned Agent ONLY
   - System must enforce at all layers

4. **No cross-contamination**
   - Collaborators only see Contacts they're explicitly granted access to
   - No inheritance or implied access

5. **Consent compliance**
   - All communication must respect Contact consent status
   - Opt-out requests must be immediately honored
   - Audit trail required for all consent changes

6. **Organization-level access**
   - Admins can access Organization-owned Contacts
   - Team-level sharing follows Collaborator rules
   - Privacy restrictions still apply

7. **Resource Assignment**
   - Assigned Agent can assign Resources to Contacts
   - Collaborators can view assigned Resources
   - System tracks Resource assignments

### Resource Management Rules

1. **Resource Definition**
   - Resources are Contacts with `RelationshipState.ServiceProvider`
   - Provide external services (legal, home inspection, contractors, lenders, etc.)
   - Organization is NOT liable for Resource service quality or legal issues
   - Resources are independent third-party service providers

2. **Resource Assignment Process**
   - Assigned Agent maintains curated list of Resources per Organization
   - When Contact requests services, Assigned Agent provides selection of appropriate Resources
   - Assignment based on evaluation of fit to Contact's specific needs
   - System tracks assignments via `ContactAssignment` entity

3. **Resource Access Control**
   - Assigned Agent: Full access to assign/remove Resources, view all assignments
   - Collaborators: Can view assigned Resources, limited assignment capabilities
   - Resources themselves: Can update their own Contact information and service details
   - Admin: Full access for Resource management and oversight

4. **Resource Data Management**
   - Resources maintain own Contact record with service-specific fields:
     - `CompanyName`, `LicenseNumber`, `Specialties`, `ServiceAreas`
     - `Rating`, `ReviewCount`, `IsPreferred`, `Website`
   - Assignment tracking includes: Purpose, Status, Client feedback, Usage confirmation
   - System maintains Resource performance metrics for future assignments

5. **Liability and Compliance**
   - Clear documentation that Organization provides Resources "as-is"
   - No warranties or guarantees on Resource service quality
   - Client feedback and ratings for future reference only
   - Compliance with referral disclosure requirements where applicable

---

## Implementation Standards

### Naming Conventions
- Always use "**Assigned Agent**" in business discussions (not just "Agent")
- All non-assigned users with access are "**Collaborators**"
- Contact encompasses all people/entities with Lead/Client as states
- Interaction Tags determine visibility for each role

### Code Standards
- EF Core navigation properties must align with this lexicon
- API endpoints must enforce tag-based access control
- UI components must respect role-based visibility
- Audit logs must capture all access attempts

### Testing Requirements
- Unit tests must verify Assigned Agent full access
- Unit tests must verify Collaborator restrictions
- Integration tests must prevent cross-contamination
- Security tests must validate tag enforcement
- Compliance tests must verify consent handling

---

## Change Management

### Document Updates
- All changes must be reviewed and approved
- Version history maintained
- Breaking changes require migration plan
- Implementation teams notified of updates

### Communication Protocol
When this lexicon changes:
1. Update this document with new version
2. Notify development team
3. Update code documentation
4. Update API documentation
5. Update user training materials

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-06-07 | Initial creation - Core terminology and access rules established | Development Team |
| 1.1 | 2025-06-07 | Added extended business concepts, real estate-specific terminology, organizational structure, and compliance terms | Development Team |
| 1.2 | 2025-06-08 | Added Resource terminology and business rules to align with the current implementation | Development Team |
| 1.3 | 2025-06-08 | Added comprehensive Resource business rules and implementation details | Development Team |
| 2.0 | 2025-06-08 | Updated data dictionary to include AI-centric architecture terminology and Azure AI Foundry integration concepts | Development Team |
| 2.1 | 2025-06-10 | Added AI Agent Architecture and Terminology Consistency sections to the existing data dictionary, incorporating the lexicon content and addressing the conversation/interaction terminology issue | Development Team |
| 2.2 | 2025-06-19 | Updated subscription model documentation to reflect the latest code changes, including OrganizationSubscription and UserSubscriptionAssignment entities, and added comprehensive property documentation | Development Team |

---

## Data Dictionary Owner and Review Information

**Data Dictionary Owner**: EMMA Product Team  
**Last Reviewed**: 2025-06-19  
**Next Review Due**: 2025-09-19

---

## Questions or Updates?

For questions about this lexicon or to propose changes, please:
1. Review existing terminology first
2. Propose specific changes with business justification
3. Consider impact on existing code and documentation
4. Follow change management protocol

**This document is the single source of truth for EMMA platform terminology and business rules.**
