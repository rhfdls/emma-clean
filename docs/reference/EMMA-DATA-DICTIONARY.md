# EMMA Platform Data Dictionary & Lexicon

**Version**: 2.1  
**Last Updated**: 2025-06-10  
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
| **Agent Orchestrator** | Thin wrapper service that routes requests to Azure AI Foundry with industry-specific prompts | `AgentOrchestrator` class in `Emma.Core.Services` |
| **AI Foundry Service** | Service layer that handles all communication with Azure AI Foundry APIs | `AIFoundryService` class implementing `IAIFoundryService` |
| **Industry Profile** | Configuration that customizes AI prompts and workflows for specific verticals (real estate, mortgage, financial advisory) | Tenant-specific prompt engineering |
| **Agent Capability** | Specific AI functions available through Azure AI Foundry (scheduling, property curation, emotional analysis, etc.) | Defined in Azure AI Foundry, not in codebase |
| **Agent Task** | A structured request sent to Azure AI Foundry containing system prompts, user input, and context | Input to AI processing pipeline |
| **Agent Response** | Structured output from Azure AI Foundry containing AI-generated content and suggested actions | Output from AI processing pipeline |
| **Interaction Context** | Persistent conversation state maintained across multiple AI interactions | Managed by Azure AI Foundry |
| **System Prompt** | Industry-specific instructions that guide AI behavior for tenant workflows | Dynamically generated based on tenant profile |

### Primary Entities & Roles

| Term | Definition | Implementation Notes |
|------|------------|---------------------|
| **Contact** | An individual or business entity stored in the system; can be a Lead, Client, or Personal Contact | Base entity in EF Core model - AI-centric relationship management |
| **Lead** | A Contact who has not yet become a Client | State/classification of Contact |
| **Client** | A Contact with an active or past contract with the Assigned Agent | State/classification of Contact |
| **Consumer** | The end-client; may be used to distinguish "Contact" as a business record from "Consumer" as a person purchasing or selling | Business terminology |
| **Assigned Agent** | The Agent who currently holds a contract (listing or representation) with a given Contact | Use this term, not just "Agent" |
| **Collaborator** | A user (internal team member, co-agent, or outside partner) who is granted restricted access to a Contact, by the Assigned Agent | Represented by ContactCollaborator entity |
| **Contact Owner** | The Agent or Organization currently responsible for the Contact record. May differ from the Assigned Agent if the Contact is transferred | May be Agent or Organization level |
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
| **Interaction** | Any logged communication or event (call, SMS, email, note, meeting, etc.) linked to a Contact | Core entity for all communications |
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
| **ConversationInsights** | **Dictionary<string, object> Insights** | Flexible AI-generated data structures | ✅ COMPLETE |
| **SentimentAnalysis** | **Dictionary<string, object> Sentiment** | Flexible sentiment data | ✅ COMPLETE |
| **BuyingSignal** | **List<string> BuyingSignals** | Simplified signal representation | ✅ COMPLETE |

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
| **Insights** | `Dictionary<string, object>` | Flexible AI-generated insights | `{"lead_quality": "high", "urgency": 0.8}` |
| **Sentiment** | `Dictionary<string, object>` | Sentiment analysis results | `{"overall": "positive", "confidence": 0.9}` |
| **BuyingSignals** | `List<string>` | Detected purchase indicators | `["timeline_mentioned", "budget_discussed"]` |
| **Recommendations** | `List<string>` | Suggested next actions | `["schedule_showing", "send_listings"]` |

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

---

## Questions or Updates?

For questions about this lexicon or to propose changes, please:
1. Review existing terminology first
2. Propose specific changes with business justification
3. Consider impact on existing code and documentation
4. Follow change management protocol

**This document is the single source of truth for EMMA platform terminology and business rules.**
