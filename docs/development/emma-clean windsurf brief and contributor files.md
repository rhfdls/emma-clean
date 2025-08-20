# `.windsurf/assistant-brief.md`

> **Purpose**: Make Cascade (Windsurf’s repo-aware AI) productive **without hand-holding** while strictly preserving EMMA’s AI‑first architecture, Azure integration patterns, and coding standards.

---

## Principles

1. **AI‑first conductor**: Orchestrate through Azure AI Foundry/Azure OpenAI; keep our services as thin wrappers.
2. **Contact‑centric**: All business logic flows through `Contact` + `Interaction` with privacy tags and validation.
3. **Safety by design**: Three‑tier validation (relevance → risk → override) with audit trails; default to fail‑safe.
4. **Tenant & subscription aware**: Never ignore tenant, subscription, or seat limits in code or tests.
5. **Consistency > cleverness**: Prefer patterns already present in `src/` over novel designs.

---

## Workspace brief

* **Solution layout**: `Emma.sln`; app code in `src/`; tests in `tests/`; utilities in `tools/`; legacy in `archive/` (read‑only).
* **Frontend**: `frontend/web/` (Next.js + TS + Tailwind + shadcn). Keep designs minimal, accessible, and consistent.
* **Dependency policy**: Central Package Management via `Directory.Packages.props`. **Never** pin versions in `.csproj`; respect EF/Npgsql 8.x pins; locked restore only.
* **Cloud & AI**: All AI calls go through `IAIFoundryService`/`IEmmaAgentService`. No custom agent logic in business services. Use conversation/thread IDs consistently.
* **DB selection**: PostgreSQL = relational/ACID; Cosmos DB = un/semistructured + vector search. Respect tenant partitioning.
* **Telemetry**: Emit structured logs + metrics for validation, overrides, costs, latency. Correlate with `TraceId`.

---

## Guardrails (hard rules)

* Do **not** modify anything under `archive/`.
* Do **not** introduce unpinned package versions or bypass CPM.
* Do **not** embed secrets. Use `appsettings.*` + Key Vault bindings.
* Do **not** add new AI agents/services that bypass `Emma.Api` or the orchestrator.
* Do **not** use Contact **tags** for privacy/business rules; use **Interaction** tags and policy services.
* Do **not** commit generated API keys, test data with PII, or model artifacts.

---

## Coding style

* .NET 8, async/await everywhere for IO/AI.
* Controllers thin → services in `src/Emma.Core/Services/`.
* DI registration mirrors existing patterns in `Program.cs`.
* Narrow interfaces; prefer records/immutable DTOs.
* Unit tests mirror `src/` namespace layout under `tests/`.

---

## CI & local commands

* **Restore (locked)**: `dotnet restore --locked-mode`
* **Build**: `dotnet build /bl:msbuild.binlog`
* **Unit tests**: `dotnet test tests/Emma.Api.UnitTests/ -c Release`
* **Integration**: `dotnet test --filter "Category=Integration"`
* **Frontend**: `npm i && npm run dev` in `frontend/web/`

---

## Autonomy & output discipline

* **Autonomy**: Be proactive. Propose multi‑file patches. Split large diffs into phases. Ask only if absolutely blocked.
* **Explain concisely, diff verbosely**: Keep chat short, include full diffs, and list exact shell commands to run.
* **File targeting**: Always specify absolute repo paths and insertion anchors.

---

## Acceptance checks (every task)

1. Locked restore passes; no version drift.
2. Build clean on Windows + Linux runners.
3. Tests added/updated and passing.
4. Validation/override/audit pathways respected.
5. Tenant & subscription checks included where applicable.
6. Lint/format OK (C# analyzers, ESLint/Prettier for web).

---

## Architecture hooks you must use

* `IAIFoundryService` for all model/agent calls.
* `IAgentOrchestrator` to delegate orchestration.
* Validation pipeline: `IActionRelevanceValidator` → `IUserOverrideService` → approval ↔ execution.
* Configuration via Enum/Prompt providers with hot‑reload; never hardcode enums/prompts.

---

## Structured prompt blocks (use in chat with me)

Wrap requests in these XML‑like sections. I will follow **exactly**.

```
<task>…concrete change with file paths…</task>
<context>…links to related files/classes…</context>
<constraints>…what not to change, perf/security limits…</constraints>
<design>…patterns to mimic (DI, controllers, services)…</design>
<tests>…units/integration to add or update…</tests>
<acceptance>…build/test/validation/override expectations…</acceptance>
<autonomy>Be proactive. Propose patches.</autonomy>
<run>…commands to verify locally/CI…</run>
```

Shorter aliases are fine: `<task|constraints|acceptance|run>`.

---

## Repo‑specific patterns I will mimic

* Controllers thin; small services in `src/Emma.Core/Services/`.
* Tenant context and subscription checks at service entry.
* Interaction over Conversation naming everywhere.
* Privacy/business rules via Interaction tags and policy services.
* RAG/embeddings and prompt loading via providers; versioned and audited.

---

## Self‑reflection rubric (internal)

Before posting patches I will internally score: **correctness**, **blend‑in**, **tests**, **perf**, **security**. If any score < 8/10 I’ll iterate once.

---

## Prompt snippets (quick reuse)

* **Bugfix/refactor (small)**

```
<task>Fix X in src/Emma.Api/.../Y.cs</task>
<context>Relevant: A.cs, B.cs. Do not change archive/.</context>
<constraints>Keep CPM versionless; preserve logging style.</constraints>
<acceptance>Unit tests Z pass; CI drift guards pass.</acceptance>
<autonomy>Be proactive. Propose patches.</autonomy>
```

* **Add dependency**

```
<task>Add Foo.Bar for X</task>
<constraints>Edit Directory.Packages.props only. Respect EF/Npgsql 8.x. Locked restore must succeed.</constraints>
<acceptance>Restore locked; build succeeds; no drift.</acceptance>
```

* **New API/service**

```
<task>Add POST /contacts/bulk in src/Emma.Api/Controllers/ContactsController.cs</task>
<design>Follow existing DI in Program.cs; thin controller; service in src/Emma.Core/Services/.</design>
<tests>Unit tests under tests/Emma.Api.UnitTests/Services/; add integration skeleton.</tests>
<acceptance>Build succeeds; tests pass; validation/override covered.</acceptance>
```

* **Frontend (Next.js/TS/Tailwind/shadcn)**

```
<task>Create minimal dashboard in frontend/web/ (app router + shadcn/ui).</task>
<ui_guidelines>Limited palette, spacing x4, skeletons, a11y via Radix.</ui_guidelines>
<structure>/src/app, /components, /lib, /types</structure>
<acceptance>Runs locally; lint passes; visually consistent.</acceptance>
```

* **Validation/override pipeline change**

```
<task>Propagate userOverrides through ActionRelevanceRequest and AgentActionValidationContext.</task>
<context>See IUserOverrideService, IActionRelevanceValidator.</context>
<acceptance>Unit+integration tests prove propagation; audit logs include userOverrides.</acceptance>
```

---

# `CONTRIBUTING-AI.md`

> **Goal**: High‑quality, consistent AI‑assisted changes to `emma-clean` with safety, performance, and maintainability.

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

**That’s it.** Save the brief as `.windsurf/assistant-brief.md` and this guide as `CONTRIBUTING-AI.md`. Cascade should now “blend in” with `emma-clean`, propose precise diffs, and keep the AI‑first architecture intact.
