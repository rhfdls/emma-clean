# Emma AI Platform — TODO List

## [Open Action Items & Next Steps]

### Configuration & Secrets Management
- [ ] Remove all secrets (API keys, connection strings) from `docker-compose.yml`
- [ ] Create proper `.env` and `.env.local` structure; ensure `.env.local` is in `.gitignore`
- [ ] Standardize all environment variable naming conventions (e.g., UPPERCASE_DOUBLE_UNDERSCORE)
- [ ] Implement startup EnvironmentValidator in `Program.cs` for required env vars
- [ ] Add/Update `SECRETS_MANAGEMENT.md` to document secret handling, onboarding, and rotation
- [ ] Integrate Azure Key Vault for managing production secrets
- [ ] Add environment variable validation to CI/CD pipeline
- [ ] Document order of precedence for env variables (`docker-compose` > `.env` > `appsettings.json`)

### Code Quality & Workflow
- [ ] Enforce creation/updating of automated tests for all significant code changes
- [ ] Provide summary/changelog entry after each major edit (`CHANGELOG.md`)
- [ ] Propose branch name and/or draft PR for collaborative changes, including prefilled description and validation steps
- [ ] Always state current LLM model and warn if switching could lose context
- [ ] Automatically scan for security/compliance risks when editing authentication, secrets, or integrations
- [ ] Reference and update `TODO.md` and `TECH_DEBT.md` after major code changes

### Technical Debt & Decisions (see also `TECH_DEBT.md`)
- [ ] Resolve all duplicate or conflicting env variable definitions between `.env`, `docker-compose`, and codebase
- [ ] Eliminate inconsistent env var naming (e.g., `COSMOSDB__` vs. `CosmosDb__`)
- [ ] Implement script/check for potential environment variable shadowing/conflicts
- [ ] Track all major design/architecture decisions and known tech debt in `TECH_DEBT.md`

### SQL Data Access Implementation (Role-Based Context Extraction)
- [ ] **CRITICAL**: Fix NbaContextService method signature mismatch - `ExtractContactContextAsync` vs `ExtractContextAsync`
- [ ] Implement real contact access validation in `ValidateContactAccessAsync` (replace demo "allow all" logic)
- [ ] Replace hardcoded contact preferences with real data from contact profile/consent management
- [ ] Populate recent interactions list with real filtered data from database
- [ ] Populate tasks list with real task data from database
- [ ] Calculate real agent performance metrics (interactions, conversion rate, task completion)
- [ ] Implement real organization KPIs calculation (interactions, agent counts, interaction types)
- [ ] Add real audit log population for admin context
- [ ] Implement real system health monitoring (database status, API response time, error rates)
- [ ] Replace hardcoded subscription info with real subscription data
- [ ] Add deal data integration for AI workflow context
- [ ] Implement workflow triggers population based on business rules
- [ ] Replace hardcoded data classification with real business logic
- [ ] Implement real tenant context retrieval (replace demo tenant)
- [ ] Add real tenant access validation logic
- [ ] Create comprehensive unit tests for all context extraction methods
- [ ] Add integration tests for role-based data filtering
- [ ] Document privacy compliance measures for extracted data

### Industry Profile Enhancements (RealEstateProfile Excellence)
- [ ] **Add ProfileVersion property** to all industry profiles for migrations and prompt history tracking
- [ ] **Implement localization support** for prompts and templates (i18n/l10n preparation)
- [ ] **Align LLM output schemas** - ensure NBA agent JSON output maps directly to NbaActionTypes and AvailableActions
- [ ] **Create validation/testing framework** for industry profiles to confirm all action types, states, and transitions are referenced
- [ ] **Add inline comments/documentation** for non-obvious profile configurations and business logic
- [ ] **Externalize profile configuration** - move templates and lists to configuration files/database for non-dev modification
- [ ] **Create industry profile checklist** for validating new vertical implementations
- [ ] **Develop YAML/JSON schema** for loading/saving industry profiles outside of C# code
- [ ] **Design integration patterns** for new agents and specializations within industry profiles
- [ ] **Implement profile inheritance** for shared industry characteristics (e.g., sales-based industries)
- [ ] **Add profile performance metrics** to track LLM effectiveness by industry vertical
- [ ] **Create profile migration tools** for version upgrades and schema changes

### EMMA Agentic Architecture Implementation (Developer Reference Blueprint)

#### First-Class Agent Development (Stage 1 - Lead Management)
- [ ] **Implement Lead Intake Agent** - Communication Management for intake/qualify new leads
  - [ ] Create `LeadIntakeAgent.cs` following IAgent interface pattern
  - [ ] Implement lead classification logic and welcome email/SMS automation
  - [ ] Integrate with CRM/DB for profile creation
  - [ ] Add trigger: "New lead signup" → intake workflow initiation

#### First-Class Agent Development (Stage 2 - Engagement & Automation)  
- [ ] **Implement Property Interest Agent** - Engagement for strong listing interest detection
  - [ ] Create `PropertyInterestAgent.cs` with MLS integration
  - [ ] Implement repeated listing view detection logic
  - [ ] Add personalized follow-up automation and Realtor notifications
  - [ ] Add trigger: "Multiple property views" → interest detection workflow

- [ ] **Implement Re-Engagement Agent** - Engagement for disengagement detection
  - [ ] Create `ReEngagementAgent.cs` with inactivity monitoring
  - [ ] Implement tailored message/offer generation for inactive leads
  - [ ] Integrate with CRM and SMS/Email providers
  - [ ] Add trigger: "Lead stops interacting for X days" → re-engagement sequence

- [ ] **Implement Interaction Log Agent** - Interaction Monitoring for communication capture
  - [ ] Create `InteractionLogAgent.cs` with voice-to-text integration
  - [ ] Implement conversation transcription and summarization
  - [ ] Add automatic CRM note attachment functionality
  - [ ] Add trigger: "New communication" → log and summarize workflow

- [ ] **Implement Appointment Scheduling Agent** - Task Automation for meeting coordination
  - [ ] Create `AppointmentSchedulingAgent.cs` with calendar integration
  - [ ] Implement automated meeting/call scheduling logic
  - [ ] Integrate with calendar systems and availability checking
  - [ ] Add trigger: "Meeting agreed" → automated scheduling workflow

- [ ] **Implement Proactive Inquiry Agent** - Engagement for check-ins and action driving
  - [ ] Create `ProactiveInquiryAgent.cs` with engagement tracking
  - [ ] Implement proactive check-in message generation
  - [ ] Add information request and action driving capabilities
  - [ ] Add trigger: "No engagement detected" → proactive inquiry sequence

#### First-Class Agent Development (Stage 3 - Advanced Monitoring)
- [ ] **Implement Lead Behavior Monitor Agent** - Sentiment Analysis for engagement monitoring
  - [ ] Create `LeadBehaviorMonitorAgent.cs` with sentiment analysis integration
  - [ ] Implement engagement trend detection and sentiment scoring
  - [ ] Add notification system for dropping engagement alerts
  - [ ] Add trigger: "Sentiment analysis negative/trending down" → behavior monitoring

- [ ] **Implement Deal Progress Agent** - Workflow Automation for transaction tracking
  - [ ] Create `DealProgressAgent.cs` with transaction workflow integration
  - [ ] Implement deal step tracking and blocker escalation
  - [ ] Add automated progress updates and alert system
  - [ ] Add trigger: "Workflow progresses or stalls" → deal progress management

#### Post-Close Agent Development
- [ ] **Implement Client Satisfaction Agent** - Feedback/QA for post-close management
  - [ ] Create `ClientSatisfactionAgent.cs` with feedback collection
  - [ ] Implement post-close feedback sequence automation
  - [ ] Add satisfaction risk detection and mitigation
  - [ ] Add trigger: "Transaction closed" → client satisfaction workflow

#### Agent Orchestration Enhancement
- [ ] **Enhance AgentOrchestrator** with specialized agent routing
  - [ ] Add agent lifecycle stage management (Stage 1, 2, 3, Post-Close)
  - [ ] Implement trigger-based agent delegation system
  - [ ] Add agent state tracking and hand-off coordination
  - [ ] Create agent workflow mapping and execution engine

#### Dynamic Prompt Management System (HIGHEST PRIORITY - IMPLEMENT FIRST)
- [ ] **Foundation for LLM Intelligence** - Must be completed before agent LLM integration
  - [ ] **Phase 1: Core Infrastructure (Week 1)**
    - [x] Create `IPromptProvider` interface for dynamic prompt management
    - [x] Design `PromptConfiguration` models with agent-specific prompt sets
    - [x] Create sample `prompts.json` configuration file with real estate examples
    - [x] Implement `PromptProvider` service with JSON file loading
    - [x] Add startup configuration to register prompt provider
    - [x] Create prompt validation service with syntax checking
    - [x] Add error handling and logging for invalid prompt configurations
  - [ ] **Phase 2: Template Engine (Week 1-2)**
    - [x] Implement dynamic placeholder substitution (e.g., {ContactId}, {CurrentStage})
    - [x] Add conditional prompt sections based on context
    - [x] Create prompt template inheritance (base + industry overrides)
    - [x] Implement prompt caching and performance optimization
    - [x] Add hot-reload capability for prompt changes without restart
    - [ ] Implement loop support
    - [ ] Add nested conditionals
    - [ ] Create template functions
    - [ ] Implement include support
    - [ ] Add validation engine
  - [ ] **Phase 3: Agent Integration (Week 2)**
    - [ ] Update all agent constructors to inject `IPromptProvider`
    - [ ] Replace hardcoded prompts in `RealEstateProfile` with dynamic loading
    - [ ] Modify `AgentOrchestrator` to use dynamic prompt building
    - [ ] Update `AIFoundryService` to accept agent-specific configurations
    - [ ] Add prompt usage tracking and analytics
  - [ ] **Phase 4: Business User Experience (Week 3)**
    - [ ] Create comprehensive prompt documentation and editing guide
    - [ ] Add prompt validation tools for business users
    - [ ] Implement backup and versioning for prompt file changes
    - [ ] Create sample prompt configurations for multiple industries
    - [ ] Add prompt testing and preview capabilities
  - [ ] **Phase 5: Advanced Features (Week 3-4)**
    - [ ] Implement A/B testing for prompt variations
    - [ ] Add prompt performance analytics and optimization suggestions
    - [ ] Create prompt dependency validation (prevent breaking changes)
    - [ ] Add support for multi-language prompt configurations
    - [ ] Implement prompt approval workflow for production changes

#### Dynamic Enum Management System (NEW FEATURE)
- [ ] **Foundation for Enum Management** - Must be completed before agent enum integration
  - [ ] **Phase 1: Core Infrastructure (Week 1)**
    - [x] Create `IEnumProvider` interface for dynamic enum management
    - [x] Design `EnumConfiguration` models with agent-specific enum sets
    - [x] Create sample `enums.json` configuration file with real estate examples
    - [x] Implement `EnumProvider` service with JSON file loading
    - [x] Add startup configuration to register enum provider
    - [x] Create enum validation service with syntax checking
    - [x] Add error handling and logging for invalid enum configurations
  - [ ] **Phase 2: API and Extensions (Week 1-2)**
    - [x] Implement EnumsController for enum management
    - [x] Add extension methods for UI binding and validation
    - [x] Create multi-format support for dropdown, API, and full formats
    - [x] Implement search and filtering for enum values
    - [x] Add validation endpoints for bulk validation and metadata retrieval
    - [ ] Implement custom value support
    - [ ] Add approval workflow for custom enum additions
    - [ ] Create usage analytics for enum values
    - [ ] Implement bulk import/export for enum datasets
    - [ ] Add admin UI for enum management
  - [ ] **Phase 3: Business Benefits (Week 2)**
    - [x] Implement industry-specific enums for real estate and insurance
    - [x] Add agent-specific enum values for NBA actions and context categories
    - [x] Implement hot-reload support for enum changes without restart
    - [x] Create UI integration with SelectList helpers for MVC dropdowns
    - [x] Implement API-first design for enum management
    - [ ] Implement custom enum support
    - [ ] Add approval workflow for custom enum additions
    - [ ] Create usage analytics for enum values
    - [ ] Implement bulk import/export for enum datasets
    - [ ] Add admin UI for enum management

### ✅ **Completed Features**

#### **Core Infrastructure (Steps 961-970)**
- ✅ **Dynamic Enum Provider Service**: Comprehensive service for managing enum configurations with hot-reload support
- ✅ **Multi-level Override System**: Global → Industry → Agent hierarchy for flexible enum customization
- ✅ **Hot-reload Support**: File system watcher for real-time configuration updates during development
- ✅ **Extension Methods**: Rich set of extension methods for UI binding, validation, searching, and API formatting
- ✅ **REST API Controller**: Complete API endpoints for enum management, validation, and metadata operations
- ✅ **Service Registration**: Proper dependency injection setup mirroring the prompt provider architecture

#### **Enterprise Versioning & Audit System (Steps 971-981)**
- ✅ **Comprehensive Versioning Models**: Complete data models for version tracking, audit logging, and rollback support
- ✅ **EnumVersioningService**: Dedicated service for version creation, rollback operations, and change tracking
- ✅ **Audit Trail System**: Detailed change logging with user tracking, timestamps, and metadata
- ✅ **Rollback Capabilities**: Safe rollback to previous versions with pre-rollback backup creation
- ✅ **Version Comparison**: Detailed comparison between configuration versions
- ✅ **Import/Export System**: Configuration backup and migration with multiple merge strategies
- ✅ **Approval Workflow Foundation**: Models and interfaces for enterprise approval processes
- ✅ **EnumProvider Integration**: Full integration of versioning capabilities into the main provider
- ✅ **Versioning API Endpoints**: Complete REST API for version management, rollback, and audit operations

### **Key Business Benefits Delivered**
- **Operational Governance**: Enterprise-grade change tracking and approval workflows
- **Risk Mitigation**: Safe rollback capabilities with automatic backup creation
- **Compliance Support**: Comprehensive audit trails for regulatory requirements
- **Configuration Management**: Import/export capabilities for environment migration
- **Development Efficiency**: Hot-reload for rapid iteration combined with production-grade versioning
- **User Experience**: Rich extension methods for seamless UI integration
- **API Integration**: Complete REST API for programmatic access and integration

### **Architecture Highlights**
- **Consistent Design**: Mirrors prompt provider architecture for familiar developer experience
- **Enterprise-Ready**: Versioning, audit logging, and approval workflows for business environments
- **Flexible Override System**: Multi-level hierarchy supporting global, industry, and agent-specific configurations
- **Safety First**: Pre-rollback backups and integrity checks prevent data loss
- **Performance Optimized**: In-memory caching with file-based persistence and hot-reload support
- **Extensible**: Plugin-ready architecture for custom validation, formatting, and approval logic

### 🚀 **Planned Advanced Features**

#### **Enhanced Approval Workflows**
- Multi-stage approval processes with configurable approval chains
- Email/notification integration for approval requests
- Approval delegation and escalation rules
- Bulk approval operations for efficiency

#### **Analytics & Monitoring**
- Usage analytics for enum values and configuration changes
- Performance monitoring for enum resolution and caching
- Change frequency analysis and optimization recommendations
- Integration with application performance monitoring (APM) tools

#### **Advanced Admin UI**
- Web-based configuration management interface
- Visual diff tools for version comparison
- Drag-and-drop enum value reordering
- Bulk import/export wizards with validation

#### **Integration Enhancements**
- Database-backed enum storage for large-scale deployments
- Redis caching for distributed scenarios
- Integration with external configuration management systems
- Support for encrypted enum configurations

#### LLM Intelligence Migration (IMPLEMENT AFTER PROMPT MANAGEMENT)
- [ ] **Context-Aware Agent Intelligence** - Migrate from rule-based to LLM-powered agents
  - [ ] **Phase 1: Infrastructure Preparation (Week 4-5)**
    - [ ] ✅ **DEPENDENCY**: Complete Dynamic Prompt Management System first
    - [ ] Enhance `IAIFoundryService` with agent-specific method signatures
    - [ ] Create `AgentResponse` parsing utilities for structured JSON responses
    - [ ] Add LLM response validation and error handling
    - [ ] Implement fallback strategies for LLM failures
  - [ ] **Phase 2: NbaAgent LLM Integration (Week 5-6)**
    - [ ] Refactor `NbaAgent.ProcessRequestAsync` to use LLM intelligence
    - [ ] Replace hardcoded recommendation logic with context-aware prompts
    - [ ] Implement structured JSON response parsing for NBA actions
    - [ ] Add fallback strategies for LLM service unavailability
    - [ ] Create comprehensive logging with trace IDs for audit trail
  - [ ] **Phase 3: All Existing Agents Migration**
    - [ ] Migrate `ResourceAgent` to LLM-powered resource recommendations
    - [ ] Migrate `ContextIntelligenceAgent` to LLM-powered context analysis
    - [ ] Migrate `IntentClassificationAgent` to LLM-powered intent detection
    - [ ] Update all agents to use standardized `AgentResponse` objects
  - [ ] **Phase 4: New Agents Implementation**
    - [ ] Implement `LeadIntakeAgent` with LLM-powered lead qualification
    - [ ] Implement `PropertyInterestAgent` with LLM-powered engagement analysis
    - [ ] Implement `ReEngagementAgent` with LLM-powered re-engagement strategies
    - [ ] Apply LLM intelligence pattern to all future agents
  - [ ] **Phase 5: Privacy & Security Integration**
    - [ ] Integrate privacy filtering with LLM context serialization
    - [ ] Add compliance checks before external LLM API calls
    - [ ] Implement data masking for sensitive information in prompts
    - [ ] Create audit logging for all LLM interactions
  - [ ] **Phase 6: Testing & Validation**
    - [ ] Create unit tests with mocked LLM responses for all agents
    - [ ] Implement integration tests for LLM service abstraction
    - [ ] Add performance testing for LLM-powered agent workflows
    - [ ] Create validation tests for prompt construction and response parsing

#### ENUM Management Feature (HIGH PRIORITY)
- [ ] **Dynamic Business Configuration System** - Enable non-developers to manage all system ENUMs
  - [ ] **Phase 1: File-Based ENUM Management**
    - [ ] Create central ENUM definition file structure (JSON/YAML format)
    - [ ] Design `IEnumProvider` service interface for dynamic ENUM lookup
    - [ ] Implement `EnumProvider` class with file loading and validation
    - [ ] Create `EnumValue` model with Value, Label, Description, Order properties
    - [ ] Add startup configuration to load ENUMs from external file
    - [ ] Implement validation for duplicates, missing fields, reserved values
    - [ ] Add error handling and logging for invalid ENUM definitions
  - [ ] **Phase 2: Code Integration**
    - [ ] Replace hardcoded C# enums with dynamic ENUM provider calls
    - [ ] Update all dropdowns and forms to use dynamic ENUM values
    - [ ] Integrate ENUM provider with LLM prompt builders
    - [ ] Update agent workflows to reference dynamic ENUMs
    - [ ] Modify industry profiles to use configurable ENUM definitions
  - [ ] **Phase 3: Business User Experience**
    - [ ] Create sample ENUM definition files for real estate industry
    - [ ] Add documentation for editing ENUM files (business user guide)
    - [ ] Implement backup and versioning for ENUM file changes
    - [ ] Create validation tools for ENUM file structure
    - [ ] Add hot-reload capability for ENUM changes without restart
  - [ ] **Phase 4: Advanced Features**
    - [ ] Implement ENUM usage tracking (where each ENUM is referenced)
    - [ ] Add ENUM dependency validation (prevent breaking changes)
    - [ ] Create ENUM migration tools for system updates
    - [ ] Add support for conditional/contextual ENUM values
  - [ ] **Phase 5: Management UI (Future)**
    - [ ] Design web admin interface for ENUM management
    - [ ] Implement CRUD operations for ENUM values
    - [ ] Add role-based access control for ENUM editing
    - [ ] Create ENUM impact analysis (show usage before changes)
    - [ ] Add bulk import/export capabilities for ENUM definitions

#### Integration Points Development
- [ ] **CRM Integration** - Follow Up Boss and GOG DB connectivity
  - [ ] Implement Follow Up Boss API integration
  - [ ] Add GOG database connectivity and data synchronization
  - [ ] Create unified CRM interface for all agents

- [ ] **MLS Integration** - Multiple Listing Service connectivity
  - [ ] Implement MLS API integration for property data
  - [ ] Add listing view tracking and analytics
  - [ ] Create property matching and recommendation engine

- [ ] **Communication Integration** - Email/SMS/Calendar automation
  - [ ] Implement email provider integration (SendGrid, etc.)
  - [ ] Add SMS provider integration (Twilio, etc.)
  - [ ] Create calendar system integration (Google Calendar, Outlook)

- [ ] **AI Services Integration** - Sentiment and voice analysis
  - [ ] Implement voice-to-text service integration
  - [ ] Add sentiment analysis service connectivity
  - [ ] Create conversation intelligence and analytics

#### Extensibility Framework
- [ ] **Create Agent Extension Framework** for new agent types
  - [ ] Design standardized agent interface contracts
  - [ ] Implement agent registration and discovery system
  - [ ] Add agent configuration and customization capabilities
  - [ ] Create agent testing and validation framework

- [ ] **Future Agent Preparation** - Market Intel and Compliance agents
  - [ ] Design Market Intel Agent for market analysis and insights
  - [ ] Plan Compliance Agent for regulatory and legal compliance
  - [ ] Create agent plugin architecture for third-party extensions

---

## [Completed]
- [x] Added PR template and `CHANGELOG.md` to repo
- [x] Added initial audit of configuration and secrets management
- [x] **RESOLVED ALL BUILD ERRORS** - Emma.Api now compiles successfully with 0 errors
- [x] **Fixed missing using directives** across 6 files (AgentController, NbaContextService, PrivacyDebugMiddleware, Program.cs)
- [x] **Added missing NuGet packages** - Azure.AI.ContentSafety, Microsoft.ApplicationInsights, Microsoft.ApplicationInsights.AspNetCore
- [x] **Implemented missing interface methods** - ProcessAgentRequestAsync in AIFoundryService
- [x] **Corrected property mappings** - AgentRequest and SecurityMetadata property access
- [x] **Validated AI agent architecture** - All agents (NbaAgent, ResourceAgent, ContextIntelligenceAgent, IntentClassificationAgent) building successfully
- [x] **Integrated layered AI agent system** - AgentOrchestrator properly routing requests to specialized agents
- [x] **Confirmed privacy framework integration** - Data masking and audit logging infrastructure ready

---

## **TECH_DEBT.md** (Example — add to your repo)

```markdown
# Emma AI Platform — Technical Debt & Design Decisions

## [2024-06-01]
- Multiple env variable naming styles detected (`COSMOSDB__`, `CosmosDb__`). Will standardize to UPPERCASE_DOUBLE_UNDERSCORE.
- Some secrets and API keys have previously been committed to config files—full secret rotation and audit needed.
- Current CI/CD process does not validate environment variables; will add startup validator and pipeline checks.
- Known risk of variable shadowing between `.env`, system, and Docker Compose—add script to detect/prevent.
- No documentation existed for secret onboarding or rotation; now tracked in `SECRETS_MANAGEMENT.md`.

```
