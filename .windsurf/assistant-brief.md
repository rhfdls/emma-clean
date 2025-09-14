**Purpose**: Make Cascade (Windsurf’s repo-aware AI) productive **without hand-holding** while strictly preserving EMMA’s AI‑first architecture, Azure integration patterns, and coding standards.

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

## Quality gates & Definition of Done (DoD)

A change is “Done” only if all are true:
- Locked restore passes; CPM only; EF/Npgsql pinned (8.x).
- Orchestration boundary respected (all AI via `IAIFoundryService`/`IAgentOrchestrator`).
- Validation → override → execution intact; `userOverrides` propagated; audited with reason taxonomy.
- Structured outputs validate against JSON Schemas (see `docs/schemas/`).
- Tenancy checks present: Postgres filtered by `OrganizationId/TenantId` (or RLS), Cosmos uses `/tenantId`.
- Telemetry includes: `traceId`, `tenantId`, `orgId`, hashed `userId`, `endpoint`, `modelName`, `modelVersion`, `tokensIn`, `tokensOut`, `totalCostEstimate`, `decision`, `overrideMode`, `aiConfidenceScore`, `durationMs`.
- Tests updated: unit (LLM mocked), prompt snapshot, schema validation; integration when IO involved.
- ADR added/updated for architectural changes.

### Rejection ⇒ Alternatives
- If any tier rejects (validation/risk/approval), record reason (taxonomy) and propose ≥1 safer alternative action.

### Telemetry SLOs (validation path)
- p95 duration ≤ 1500 ms; p99 ≤ 3000 ms.

### Tenancy invariants
- Postgres: scope queries by `OrganizationId/TenantId` or rely on RLS; no cross-tenant scans.
- Cosmos: include partition key `/tenantId`; cross-partition queries require code comment and PR justification.

### Security & Safety policy
- PII-in-prompts ban: no raw emails, phone numbers, exact addresses; use stable IDs/masks.
- Secrets scanning: no credentials/tokens in code, tests, or dumps.
- Red-team: add two negative tests per new prompt (jailbreak and prompt-leak attempts).

### ADR policy
- Require ADRs for new agent types, cross-service orchestration changes, datastore changes, or security posture changes.

### Schema Change Policy (ADR-0007)
- Any schema change (new columns/tables/relationships/enums, type widening, non-breaking constraints) requires an ADR and architect approval.
- Destructive changes (drop/rename columns, narrow types, alter PKs, remove enum values) must include a compatibility plan.
- Every approved schema change must:
  - Include an EF Core migration under `src/Emma.Infrastructure/Migrations/`
  - Update `docs/architecture/AppDbContextSchema.md` and `docs/architecture/EMMA-DATA-DICTIONARY.md`
  - Be tied to a PR with architect review
  - Include a safe migration path (defaults, backfills, compatibility layer)
- PRs that include migrations must reference an ADR in the PR description (e.g., “ADR-0007”).
  
See: `docs/adr/0007-schema-change-policy.md`

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
