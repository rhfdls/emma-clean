# EMMA AI-First CRM: Unified Project Management Plan

> **This master plan unifies implementation backlog, sprint plans, validation, technical debt, and enhancement tracking—so the team has one canonical view for delivery, review, and evolution.**

---

## 1. Project Structure & Documentation Flow

- **Master Backlog**: All implementation themes and priorities across sprints (see Section 2).
- **Sprint Summaries**: Validate sprint completion and document key outcomes (see Section 3).
- **Validation Checklists**: Used at the end of each sprint for completeness and production readiness.
- **Enhancement/Tech Debt Backlog**: "Later" and tech debt items tracked for grooming, with triggers to elevate based on product maturity, regulatory needs, or scale.
- **Migration Guide**: Ensures migrations are repeatable and documented as schema evolves.

---

## 2. Unified Implementation Backlog

### 2.1 Core Backlog by Sprint

#### **SPRINT 1: Core Architectural Hooks**

- Dynamic Agent Registry (`IAgentRegistry`)
- AgentOrchestrator dynamic routing (via registry, not hardcoded)
- Universal explainability (`Reason`, `AuditId`)
- Agent lifecycle hooks (hot reload, health)
- Feature flag infrastructure (config, code, runtime)
- Context provider abstraction (data access)
- API versioning (`/api/v1/` everywhere)

#### **SPRINT 2: Service Foundation, Monitoring, Extensibility**

- Agent Metadata Registry (DB model, API)
- Blueprint CRUD service (for agent blueprints)
- Real-time metrics and monitoring (Prometheus, App Insights)
- Security validation for factory agents (extends 3-tier validation)
- Human-in-the-loop approval queue
- Extensible action types/scopes (config/registration, not code)

#### **SPRINT 3: Three-Tier Validation, Optimization**

- Tag all actions by scope (InnerWorld/Hybrid/RealWorld)
- Optimize validation for inner-world actions (schema-only, cache)
- Validation metrics dashboard (real-time monitoring)
- Performance improvements and test coverage

#### **SPRINT 4: Agent Factory & Production Readiness**

- Agent compiler service (blueprint → executable agent)
- Hot-reload infrastructure (live update, zero downtime)
- Advanced monitoring & alerting
- Security hardening (RBAC, input validation, audit logs)

---

## 🚩 First Working Dev Release (MVP) – Milestone

### **Goal:**

Deliver a vertically-integrated, minimal EMMA workflow—demonstrating real-world agent orchestration, contact lifecycle, and end-to-end audit/compliance in a single deployable environment.

### **MVP Must-Have Features**

- **Contact CRUD & Ownership:**
  - Create, update, and delete a contact (agent as owner, with basic relationship fields).
  - Enforce ownership/assignment rules; real (not stubbed) access control.
- **Agent-Orchestrated Workflow:**
  - Trigger and execute a core workflow (e.g., contact follow-up, action recommendation).
  - Use dynamic agent registry and orchestrator (no hardcoded logic).
  - Basic LLM/NLP-based "next action" or intent recommendation.
- **Validation & Audit:**
  - Three-tier validation pipeline (with action scope classification).
  - Universal explainability: `Reason` and `AuditId` fields present and populated.
  - End-to-end audit trail for all major events (action, validation, override).
- **API Versioning & Feature Flags:**
  - All APIs versioned (`/api/v1/`), feature flags protecting all new functionality.
- **Minimal UI (Optional):**
  - A web form or API client to exercise core flows (add contact, trigger workflow, view audit).

### **Validation/Acceptance Checklist**

- [ ] Contact create/update/delete working via API and/or UI
- [ ] Only the assigned agent can access/edit their contacts (no stubbed access control)
- [ ] Can trigger workflow, LLM agent responds with suggested next action or summary
- [ ] All actions go through validation, return a populated Reason and AuditId
- [ ] Every workflow step generates an audit event
- [ ] Feature flags can enable/disable MVP features without breaking base system
- [ ] All APIs are accessed via `/api/v1/`
- [ ] Code passes unit/integration tests; minimal regression from prior releases
- [ ] Deployment guide and onboarding doc updated

---

## 🛠️ Tech Debt – Immediate Focus (Deduplicated & Actionable)

**All high/medium-priority open technical debt items, deduplicated from historical docs, for ongoing tracking and sprint prioritization.**

### **Security & Access Control (Highest Priority)**

- [ ] Replace all hardcoded contact access logic (`ValidateContactAccessAsync` must enforce real ownership/collaboration).
- [ ] Implement real tenant validation and access checks (remove `IsActive=true`, hardcoded tenant IDs).
- [ ] Move consent/preferences from stubs to real user data (contact profile, consent management).
- [ ] Remove any legacy secrets/config from docker-compose files—move to secure Azure Key Vault or appsettings.

### **Core Functionality & Data Integrity**

- [ ] Fix interface mismatch in `NbaContextService` vs `ISqlContextExtractor` (ensure signature alignment and actual context extraction).
- [ ] Populate recent interactions, tasks, agent KPIs, and all reporting fields with live (not placeholder) data in SqlContextExtractor/AdminContextExtractor.
- [ ] Implement real system health monitoring (no more hardcoded "DatabaseStatus = Healthy", etc.).

### **Legacy Code & Migrations**

- [ ] Remove or migrate legacy privacy/business tags from Contact entities (`CRM`, `PERSONAL`, `PRIVATE`).
- [ ] Ensure all technical debt related to consent management, audit logging, and stubs is reflected in the master data migration plan.

### **Logging, Audit, Monitoring**

- [ ] Review and unify logging throughout AgentOrchestrator and major services (all logs consistent, actionable, and linked to AuditId).
- [ ] Add missing audit logging to all endpoints/methods where user or system actions occur.
- [ ] Implement comprehensive error handling and exception capture across all major services (no silent failures).

### **Documentation & Developer Experience**

- [ ] Update all outdated references and onboarding guides to point to this unified plan.
- [ ] Create/maintain a "Tech Debt Detail" appendix in this file (optional: if you want to preserve rationale/history for each item).

---

### **Tech Debt Prioritization Approach**

- Security and data integrity come first—must resolve all "stub-to-prod" issues for access control and data handling before first release.
- Logging/audit and interface mismatches are next, as they impact debugging, support, and compliance.
- Legacy/deprecated code and migration plans must be cleaned up before feature freeze for first production release.

---

## 📑 Tech Debt Appendix (Detailed Item References)

> (Optional, but recommended if you want to preserve full context or code references—copy explanations from TECH_DEBT.md here, or link to the file for detailed rationale.)

---

### 2.2 Enhancement & Tech Debt Backlog

#### Later Priority Items

- Null-safe accessors for `userOverrides`
- RegulatoryContext/ComplianceTags (for financial, healthcare, legal, etc)
- Enforce max batch size for bulk approval
- Audit schema snapshots for regression/migration
- Compliance tags/filters for audit logs and validation
- Document/enforce all default parameter behaviors
- Propagate compliance/industry tags in validation
- Add telemetry/event hooks for audit/monitoring
- Complete hardcoded demo values and stubs (see TECH_DEBT.md)

#### Tech Debt – Immediate Focus

- Security & access control (replace all hardcoded logic)
- Fix interface mismatches (e.g., NbaContextService)
- Remove or document all legacy/compatibility tags and secrets
- [2025-06-12] Expand Workflow Execution in AgentOrchestrator
  - **Type:** Enhancement
  - **Priority:** Medium
  - **Owner:** Backend Team
  - **Related Area:** Architecture, Agents
  - **Description:**  
    Expand the `ExecuteWorkflowAsync` method in `AgentOrchestrator` to handle more complex workflows and scenarios, improving flexibility and functionality.
  - **Dependencies:**  
    None currently identified.
  - **Status:** Proposed
  - **Notes:**  
    Consider potential impacts on existing workflows and ensure thorough testing.
- [2025-06-12] Ensure Consistent Logging in AgentOrchestrator
  - **Type:** Enhancement
  - **Priority:** Medium
  - **Owner:** Backend Team
  - **Related Area:** Logging, Monitoring
  - **Description:**  
    Review and update logging across all methods in `AgentOrchestrator` to ensure consistency and provide adequate context for debugging.
  - **Dependencies:**  
    None currently identified.
  - **Status:** Proposed
  - **Notes:**  
    Align with logging standards and best practices.
- [2025-06-12] Review Error Handling in AgentOrchestrator
  - **Type:** Tech Debt
  - **Priority:** Medium
  - **Owner:** Backend Team
  - **Related Area:** Error Handling, Robustness
  - **Description:**  
    Review error handling in `AgentOrchestrator` to ensure all potential exceptions are caught and logged appropriately.
  - **Dependencies:**  
    None currently identified.
  - **Status:** Proposed
  - **Notes:**  
    Ensure error messages are informative and actionable.

---

## 3. Validation & Success Criteria

- **Core Components:**
  - Dynamic agent registry is thread-safe, fully tested
  - Agent lifecycle management covers hot-reload, health
  - Explainability (`Reason`, `AuditId`) in all major models
  - Feature flags for all new capabilities; off by default
  - Context provider abstracts all data access
  - All APIs versioned
- **Quality Gates:**
  - Interface-driven, async code throughout
  - DI (Dependency Injection) best practices
  - Microsoft/Azure compliance & best practices
  - End-to-end integration and unit tests (target: 85%+ code coverage)
- **Operational/Monitoring:**
  - Structured logs, health endpoints, and metrics dashboards live
  - Rollback and deployment strategies defined and tested
- **Security:**
  - Input validation, audit logs, and RBAC in place for all new features

---

## 4. Documentation, Testing, and Migration

- **Docs**: All architecture, API, and implementation docs are in `/docs` per the README and Sprint summaries.
- **Testing**: Unit, integration, and end-to-end tests tracked with each sprint checklist and validated at release.
- **Migrations**: [MIGRATION-README.md](./MIGRATION-README.md) used for all DB/data schema changes—update and reference in PRs.

---

## 5. Change Management & Review Process

- **Enhancement backlog** ([TODO-ENHANCEMENT-BACKLOG.md](./TODO-ENHANCEMENT-BACKLOG.md)) reviewed monthly for promotion to current sprint.
- **Tech debt** ([TECH_DEBT.md](./TECH_DEBT.md)) reviewed after every sprint.
- **Sprint validation** checklists ([SPRINT-1-VALIDATION-CHECKLIST.md](./SPRINT-1-VALIDATION-CHECKLIST.md), etc.) signed off before prod deployment.
- **Migration guide** updated for every schema change.

---

## 6. Summary Table: Workstreams & Ownership

| Area                      | File/Section                          | Owner/Lead       | Success Metric                     |
|---------------------------|---------------------------------------|------------------|------------------------------------|
| Core Agent Factory        | MASTER-IMPLEMENTATION-BACKLOG.md      | Lead Architect   | All agents dynamic, registry-based |
| Sprint Outcomes           | SPRINT-1-IMPLEMENTATION-SUMMARY.md    | Tech Lead        | All validation complete            |
| Validation/Testing        | SPRINT-1-VALIDATION-CHECKLIST.md      | QA Lead          | 85%+ code coverage, 0 regressions  |
| Enhancements/Tech Debt    | TODO-ENHANCEMENT-BACKLOG.md, TECH_DEBT.md | Product Owner | 100% resolved/prioritized monthly  |
| Data/Schema Migrations    | MIGRATION-README.md                   | DBA              | Zero-fault DB upgrades             |
| Docs/Onboarding           | README.md, /docs                      | All Leads        | Up-to-date, used by all new hires  |

---

## 7. How To Use This Plan

1. **Project Kickoff**: Share this doc, with links to referenced files for every workstream.
2. **Sprint Planning**: Copy priorities from sprint sections into your sprint boards (Jira, Azure Boards, etc).
3. **Validation & Review**: At sprint end, complete validation checklist. Update backlog/enhancement/tech debt docs as needed.
4. **Continuous Grooming**: Regularly review and promote enhancements/tech debt as needed.
5. **Documentation Discipline**: All new features/migrations reference this doc and relevant files for implementation and QA signoff.

---

### Appendix: Key File Reference

- [MASTER-IMPLEMENTATION-BACKLOG.md](./MASTER-IMPLEMENTATION-BACKLOG.md)
- [SPRINT-1-IMPLEMENTATION-SUMMARY.md](./SPRINT-1-IMPLEMENTATION-SUMMARY.md)
- [SPRINT-1-VALIDATION-CHECKLIST.md](./SPRINT-1-VALIDATION-CHECKLIST.md)
- [SPRINT-2-ROADMAP.md](./SPRINT-2-ROADMAP.md)
- [TODO.md](./TODO.md)
- [TODO-ENHANCEMENT-BACKLOG.md](./TODO-ENHANCEMENT-BACKLOG.md)
- [TECH_DEBT.md](./TECH_DEBT.md)
- [MIGRATION-README.md](./MIGRATION-README.md)

---

**This document is your “single pane of glass” for EMMA project management. Use it for sprint planning, roadmap review, onboarding, and decision tracking. Replace all legacy TODOs, sprint notes, and enhancement lists with this as the master reference.**

---

_Last Updated: 2025-06-10_  
_Maintained By: EMMA Platform Engineering Team_
