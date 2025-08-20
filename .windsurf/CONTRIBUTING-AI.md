# Goal

High‑quality, consistent AI‑assisted changes to `emma-clean` with safety, performance, and maintainability.

## Table of contents

1. Getting started
2. Task templates
3. Architecture & data rules
4. Coding patterns
5. Testing & quality gates
6. AI prompting library
7. Review checklist

---

## 1) Getting started

* Use Windsurf **Cascade** for repo‑aware edits. Prefer structured prompts (below) and precise file paths.
* Local environment: see `CLOUD_SETUP.md` and `.env.example`. Use Azure resources directly for integration tests.
* Commands: restore/build/test as listed in the brief.

---

## 2) Task templates (copy‑paste)

### A. Patch (bugfix/refactor)

```
<task>Fix …</task>
<context>Touches: …</context>
<constraints>Keep CPM; no archive/ edits; preserve logging.</constraints>
<acceptance>Tests Z pass; no new warnings; validation intact.</acceptance>
```

### B. New endpoint/service

```
<task>Add …</task>
<design>Follow controller→service pattern; DI in Program.cs; thin controller.</design>
<tests>Unit tests + minimal integration.</tests>
<acceptance>Build/tests green; override/approval path respected.</acceptance>
```

### C. Dependency

```
<task>Add …</task>
<constraints>Edit Directory.Packages.props only; locked restore.</constraints>
<acceptance>Restore locked; no drift; CI passes.</acceptance>
```

### D. Frontend feature

```
<task>Add …</task>
<ui_guidelines>Next.js app router; Tailwind; shadcn/ui; a11y.</ui_guidelines>
<acceptance>Runs; lint passes; meets UX notes.</acceptance>
```

### E. Data/Schema change (Contact/Interaction)

```
<task>Add field … to Contact/Interaction</task>
<constraints>Respect tenant isolation; update indexes; migration script + rollback.</constraints>
<tests>EF model tests; repository queries; privacy tag rules.</tests>
<acceptance>Migration applies; queries performant; tags respected.</acceptance>
```

### F. Validation/override augmentation

```
<task>Add relevance rule and approval path for action type X</task>
<constraints>Keep three‑tier flow; log audit entries with TraceId.</constraints>
<tests>Unit for rule; integration for approval; E2E for suppression/alternative.</tests>
<acceptance>Metrics emitted; docs updated.</acceptance>
```

---

## 3) Architecture & data rules (must‑follow)

* **AI orchestration**: All model/agent calls via `IAIFoundryService` and `IAgentOrchestrator`.
* **Contact‑centric**: New features must link through `Contact` + `Interaction`; use `Interaction` tags for privacy and business logic.
* **Validation pipeline**: Always validate before execution; prefer rule‑based first, LLM for complex cases; generate alternatives; log decisions.
* **User overrides**: Support modes (AlwaysAsk/NeverAsk/LLMDecision/RiskBased). Approval flows must capture reason, confidence, and userOverrides.
* **Multi‑tenant**: Scope all queries by `OrganizationId/TenantId`; honor subscription/seat limits.
* **Config**: Use Enum/Prompt providers (versioned, audit‑logged) with hot reload.

---

## 4) Coding patterns

* Controllers → thin; Services → small/single‑purpose; repositories where helpful.
* Async/await for AI/IO; cancellation tokens.
* Centralized error handling; wrap Azure calls; retry/circuit‑breakers.
* Keep DI registrations explicit; prefer interfaces in `Emma.Core`.
* Frontend: app router, server components where sensible, a11y defaults, tailwind utilities, shadcn primitives.

---

## 5) Testing & quality gates

* **Unit**: deterministic, all external deps mocked; cover decision boundaries.
* **Integration**: containerized Postgres; Azure services mocked/stubbed unless explicitly testing connectivity.
* **Contract**: OpenAPI/Pact as needed; backward compatibility.
* **E2E**: critical journeys; staging against Azure.
* **Performance**: load for tenant‑isolated hot paths; AI latency/cost budgets.
* **Snapshots**: for prompts/templates; JSON schema validation for structured outputs.

---

## 6) AI prompting library (ready‑to‑paste)

> Use these with Cascade. Replace `{…}` placeholders. Keep sections short.

### a) Diff‑first patch

```
<task>Implement {change} touching {paths}</task>
<context>Existing patterns: {refs}; do not touch archive/</context>
<constraints>CPM only; locked restore; preserve logging; no secrets</constraints>
<acceptance>{tests}; CI green; validation/override unchanged</acceptance>
<run>dotnet restore --locked-mode && dotnet build && dotnet test</run>
```

### b) New validation rule

```
<task>Add relevance rule for {ActionType} with {threshold}</task>
<context>Validator: {path}; ensure alternatives + audit</context>
<tests>Unit: happy/edge; Integration: suppression + approval</tests>
<acceptance>Rule active; logs contain Reason & TraceId; metrics emitted</acceptance>
```

### c) User override propagation

```
<task>Propagate userOverrides across validation + approval</task>
<context>Update ActionRelevanceRequest, AgentActionValidationContext, UserApprovalRequest</context>
<acceptance>End‑to‑end test proves presence; audit includes userOverrides</acceptance>
```

### d) New agent capability (delegated)

```
<task>Expose {capability} via IAIFoundryService</task>
<context>Do not add custom agent logic; orchestrator delegates</context>
<acceptance>Feature flagged by subscription; tests + error handling included</acceptance>
```

### e) Frontend UI slice

```
<task>Implement {component} with shadcn/ui</task>
<ui_guidelines>Spacing x4, accessible by default, minimal palette</ui_guidelines>
<acceptance>Story or page renders; a11y checks; lint passes</acceptance>
```

### f) RAG content update

```
<task>Add {docs} to RAG store with metadata</task>
<constraints>Vectorize via provider; respect tenant partitioning</constraints>
<acceptance>Retrieval test passes; latency within budget</acceptance>
```

---

## 7) Review checklist (PRs)

* [ ] Locked restore/build/tests pass on CI (Win+Linux)
* [ ] AI calls go through orchestrator/service; no direct SDKs in business code
* [ ] Validation/override/audit intact; alternatives on suppression
* [ ] Tenant + subscription checks present
* [ ] No archive/ changes; CPM only; no secret leakage
* [ ] Tests adequate and deterministic; snapshots updated
* [ ] Telemetry, metrics, and docs updated if behavior changed

---

## Definition of Done (DoD)

A change is Done only if:
* Locked restore passes; CPM only; EF/Npgsql 8.x pinned.
* Orchestration boundary enforced (all AI via `IAIFoundryService`/`IAgentOrchestrator`).
* Validation → override → execution intact; `userOverrides` propagated; audited with reason taxonomy.
* Structured outputs validate against JSON Schemas in `docs/schemas/`.
* Tenancy checks present: Postgres filtered by `OrganizationId/TenantId` (or RLS), Cosmos uses `/tenantId`.
* Telemetry includes: `traceId`, `tenantId`, `orgId`, hashed `userId`, `endpoint`, `modelName`, `modelVersion`, `tokensIn`, `tokensOut`, `totalCostEstimate`, `decision`, `overrideMode`, `aiConfidenceScore`, `durationMs`.
* Tests updated: unit (LLM mocked), prompt snapshot, schema validation; integration when IO involved.
* ADR added/updated for architectural changes.

### Rejection ⇒ Alternatives
* If any tier rejects (validation/risk/approval), capture reason (taxonomy) and propose ≥1 safer alternative.

### Telemetry SLOs (validation path)
* p95 ≤ 1500 ms; p99 ≤ 3000 ms.

### Tenancy invariants
* Postgres: scope by `OrganizationId/TenantId` or RLS; no cross-tenant scans.
* Cosmos: include partition key `/tenantId`; cross-partition scans require comment + PR justification.

### Security & Safety policy
* PII-in-prompts ban (no raw emails/phones/addresses); use stable IDs/masks.
* Secrets scanning: no credentials/tokens in code, tests, or dumps.
* Red-team: two negative tests per new prompt (jailbreak and prompt-leak attempts).

### ADR policy
* ADRs required for new agent types, cross-service orchestration changes, datastore changes, or security posture changes.
