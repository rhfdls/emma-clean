# EMMA Platform Documentation

Welcome to the EMMA AI platform documentation suite. This directory provides a structured, discoverable, and future-proof knowledge base for all development, architecture, operations, and project management activities.

---

## 📂 Documentation Structure & Navigation

### 🟩 [Project Management](./project-management/)

> **Start here for all planning, priorities, and delivery!**

- **EMMA Unified Engineering Backlog.md** - 📌 *The “single pane of glass” for all sprints, roadmap, validation, enhancements, and tech debt tracking. Always reference this first for project management, architectural review, and Windsurf integration.*
- **MASTER-IMPLEMENTATION-BACKLOG.md** - Detailed phased engineering roadmap.
- **SPRINT-1-IMPLEMENTATION-SUMMARY.md**, **SPRINT-1-VALIDATION-CHECKLIST.md**, **SPRINT-2-ROADMAP.md** - Sprint-specific outcomes and validation.
- **TODO.md**, **TODO-ENHANCEMENT-BACKLOG.md** - Active work and enhancement backlogs.
- **TECH_DEBT.md** - Technical debt, design decisions, and remediation plan.
- **MIGRATION-README.md** - Data/schema migration reference.
- **CHANGELOG.md** - Historical changes and releases.

### 🏗️ [Architecture](./architecture/)

System design, core schemas, and technical foundations.

- **EMMA-AI-ARCHITECTURE-GUIDE.md**, **ARCHITECTURE.md**, **UNIFIED_SCHEMA.md**, **SQL-CONTEXT-INTEGRATION.md**.
- NuGet Governance:
  - **[NUGET-8x-PINNING-PLAYBOOK.md](./architecture/NUGET-8x-PINNING-PLAYBOOK.md)** – Architecture guardrail for NuGet 8.x pinning with CPVM + lockfiles.
  - **[Auto-Fix NuGet Version Drift – Cascade Prompt](./architecture/Auto-Fix-NuGet-Version-Drift-cascade-prompt.md)** – Ready-to-run Windsurf prompt to diagnose and remediate NuGet version drift using the playbook.
- Additional architecture notes:
  - **[EMMA-Procedural-Memory-Service.md](./architecture/EMMA-Procedural-Memory-Service.md)** – Procedural memory architecture and service design.
  - **[circular-dependency-resolution.md](./architecture/circular-dependency-resolution.md)** – Guidance for resolving circular dependencies.
- Note: `RESOURCE_ASSIGNMENT_SCHEMA.md` is kept for historical context only and is marked OBSOLETE. Use the contact-centric model in `UNIFIED_SCHEMA.md` and see `development/TERMINOLOGY-MIGRATION-GUIDE.md` for migration guidance.

### 🔒 [Security](./security/)

Compliance, security patterns, and responsible AI:

- **PRIVACY_IMPLEMENTATION_GUIDE.md**, **SECRETS_MANAGEMENT.md**, **SECURITY-FILTERING-EXAMPLE.md**, **RESPONSIBLE-AI-VALIDATION.md**.

### ⚙️ [Operations](./operations/)

Environment, deployment, and infrastructure guides:

- **CLOUD_SETUP.md**, **DEMO_SETUP_GUIDE.md**, **ENVIRONMENT_BACKUP.md**, **infrastructure.md**, **README-cosmos-env.md**, **azure-data-studio-instructions.md**.

### 🤖 [Agents](./agents/)

All agent-related specs and inventories:

- **AGENT-ORCHESTRATION-COMPLETE.md**, **AGENTIC-FRAMEWORK-INVENTORY.md**, **agents-catalog-readme.md**, plus **[agent-factory/](./agents/agent-factory/)** for factory implementation docs.

### 👨‍💻 [Development](./development/)

Development, testing, and UI/UX design:

- **TESTING.md**, **TERMINOLOGY-MIGRATION-GUIDE.md**, **Configuration-Management-Guide.md**, **azure-openai-integration.md**.
- **[designs/](./development/designs/)** - UI and experience specifications.

### 📚 [Reference](./reference/)

Key APIs, contracts, and definitions:

- **EMMA-DATA-DICTIONARY.md**, **DATA_CONTRACT.md**, **USER-OVERRIDE-ARCHITECTURE.md**, **[ai-first/](./reference/ai-first/)** for design patterns and compliance checklists.

---

## 🚦 Project Management & Roadmap

- **ALWAYS start with [EMMA Unified Engineering Backlog.md](./project-management/EMMA%20Unified%20Engineering%20Backlog.md)** for:
  - Sprint priorities
  - Enhancement and tech debt promotion
  - Validation checklists and definitions of done
  - Architecture and roadmap reviews
  - Backlog grooming and quarterly/annual planning

- All new sprints, RFCs, and major design reviews MUST reference the unified backlog to ensure alignment.

- Windsurf IDE and all developer onboarding materials should treat this as the master control document for status, dependencies, and decision history.

---

## 🚀 Quick Start Guide

### Developers

1. Start with [Project Management](./project-management/EMMA%20Unified%20Engineering%20Backlog.md) to align with the current sprint.
2. Review [Architecture](./architecture/) and [Development](./development/) for implementation details.
3. Use [Reference](./reference/) for data contracts and terminology.

### Operations/DevOps

- See [Operations](./operations/) and [Security](./security/) for environment setup and compliance.

### Product/Business

- Use [Agents](./agents/) for AI capabilities, [Project Management](./project-management/) for roadmaps.

---

## 🛠️ Contributing & Best Practices

- Place all new docs in the correct folder and update this README.
- Reference [EMMA Unified Engineering Backlog](./project-management/EMMA%20Unified%20Engineering%20Backlog.md) for all PRs, RFCs, and new development streams.
- Follow naming conventions and cross-link related files.
- Document all schema changes in [MIGRATION-README.md](./project-management/MIGRATION-README.md).

---

## 🔍 Finding Information

- **“How do I…?”** – See Development or Operations
- **“What is…?”** – See Reference or Architecture
- **“When will…?”** – Project Management (Unified Backlog or Roadmaps)
- **“Why does…?”** – Architecture or Security
- **“Where is…?”** – Reference or Operations

---

*Last Updated: 2025-06-10*  
*Maintained By: EMMA Platform Engineering Team*

