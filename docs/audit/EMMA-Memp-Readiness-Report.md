# EMMA “Memp” Procedural Memory Readiness Report

Owner: Staff+ Eng / Pair-Architect
Date: 2025-08-28
Status: Draft

## 1) Scope & Method
- Repo scanned for: multi-modal entities/ingestion, orchestrator seam, validation/override pipeline, Cosmos usage, Azure AI abstraction, feature flags, testing.
- Evidence gathered via code search across `src/` and `docs/`.

## 2) Evidence by Checklist

### Multi-modal capture present
- Entities found:
  - `src/Emma.Models/Models/Interaction.cs`
  - `src/Emma.Models/Models/Message.cs`
  - `src/Emma.Models/Models/Transcription.cs`
  - `src/Emma.Models/Models/CallMetadata.cs`
  - Tags/validation helpers: `src/Emma.Models/Validation/NoPrivacyBusinessTagsAttribute.cs`
- Ingestion path samples:
  - `src/Emma.Api/Controllers/InteractionController.cs` (create interactions/messages)
  - `src/Emma.Api/Services/InteractionService.cs` (service methods)

### Inner-World vs Outer-World boundary
- Orchestrator references appear only in tests and archive:
  - `src/Emma.Core.Tests/Services/AgentOrchestratorTests.cs`
  - `archive/Emma.Core/Services/AgentOrchestrator.cs` (legacy)
- No active orchestrator implementation under `src/`

### Validation & Overrides
- Relevance/risk/override logic present in tests and interfaces:
  - `src/Emma.Core.Tests/Services/ActionRelevanceValidatorTests.cs`
  - `src/Emma.Models/Interfaces/IAgentActionValidator.cs`
- No central runtime wiring observed in API pipeline (pre-execution hook not clearly present under `src/Emma.Api/`)

### Data layer split (Cosmos + Postgres)
- Postgres: EF Core contexts/migrations present (`src/Emma.Infrastructure/Data/EmmaDbContext.cs`, migrations under `src/Emma.Infrastructure/Migrations` and `src/Emma.Data/Migrations`).
- Cosmos: No concrete Cosmos client/config or container code found in `src/` (search for `Cosmos`, `CosmosClient`, `Container` yielded none).

### Azure AI abstraction
- Tests reference OpenAI/Azure config and service tests:
  - `src/Emma.OpenAI.Tests/*`
  - `src/Emma.Core.Tests/Services/EmmaAgentServiceTests.cs`
- `src/Emma.Api/Program.cs` references OpenAI config (limited). No explicit `IAIFoundryService` interface found in `src/` (appears in tests/docs only).

### Config governance (feature flags, prompts/enums)
- Feature constructs exist in models/seed:
  - `src/Emma.Data/FeatureSeed.cs`
  - `src/Emma.Models/Models/Feature.cs`
- Historical/archived feature flags file: `archive/Emma.Core/Configuration/FeatureFlags.cs`.
- No explicit `UseFeatureFlags` or Azure App Configuration wiring in `src/Emma.Api/Program.cs`.

### Testing (unit/integration/e2e)
- Unit/integration suites present:
  - `src/Emma.Core.Tests/**`, `src/Emma.Tests/Integration/**`
- Agent/orchestrator behavior is validated in tests, but runtime components are not fully present.

## 3) Gaps
- Orchestrator seam missing in current `src/` (only in tests/archives). No pre-LLM hook.
- Validation pipeline not centrally wired before side-effects in API request flow.
- Cosmos DB layer absent (no containers, clients, or repositories).
- Azure AI Foundry abstraction (`IAIFoundryService`) not present in production `src/` code (tests-only).
- Feature flag framework not integrated at runtime (no `ProceduralMemory.Enabled`, no ring controls).
- No procedural memory interfaces (`IProceduralMemoryService`, `IProcedureExecutor`) in `src/`.
- No telemetry dimensions for replay/fallback/override in App Insights.

## 4) PR Plan (Phased)

### Phase 0 — Prep & Flags (no breaking changes)
- Add `ProceduralMemory.Enabled` feature flag with ring support (pilot, org-allowlist).
- Update `src/Emma.Api/Program.cs` to load flags (env/App Configuration) and expose IOptions pattern.
- Add App Insights dims scaffolding (middleware/telemetry initializers): `procedureId`, `version`, `traceId`, `tenantId`, `replay`, `fallback`, `overrideRequired`.

### Phase 1 — Non-breaking seam
- Interfaces (new in `src/Emma.Core.Interfaces/`):
  - `IProceduralMemoryService` with `TryGetReplayAsync`, `CaptureTraceAsync`, `PromoteAsync`.
  - `IProcedureExecutor` with `ExecuteAsync`.
  - Provide an in-memory adapter for tests.
- DI wiring in `src/Emma.Api/Program.cs` behind feature flag.
- Orchestrator pre-LLM hook (lightweight):
  - Build `ProcedureLookupRequest` from current request/context.
  - If hit → pipe to Validation; if miss → existing LLM planner path.
  - On LLM success → `CaptureTraceAsync` (observability mode).
- Validation pipeline
  - Ensure calls traverse Relevance → Risk/Confidence → Override before any side-effect.
  - Propagate `userOverrides` and log to audit.
- Optional Postgres link (additive):
  - Add nullable `Interaction.ProceduralReplayRef` (string) to reference replay usage.

### Phase 2 — Pilot procedures
- Cosmos DB scaffolding:
  - Create containers: `procedures`, `procedure-traces`, `procedure-versions`, `procedure-insights` (partition by `tenantId`).
  - Implement repository with PII redaction (IDs not raw values).
- Executor tools & guard (low-risk):
  - Effects: `CreateInteraction`, `SendSms`, `CreateTask`.
  - Guard: `RelevanceCheck` using context (relationshipState, lastInteractionAge, privacy tags).
- Seed two pilot procedures (ring: pilot):
  - `schedule-followup-sms`
  - `log-call → summarize → create-NBA-task`
- Add `.http` suites for happy path and relevance-block.

### Phase 3 — Learning loop
- Store traces on LLM-planned executions (privacy-gated).
- `PromoteAsync(traceId, PromoteOptions)` to compile trace → procedure version.
- Admin endpoints to list procedures/versions/insights (OrgOwner/Admin only).

### Telemetry & Dashboard
- Emit metrics for replay hit rate, token savings (est), latency delta, override/blocks, fallback rate.
- Add workbook JSON: `ops/insights/memp-replay.json`.

## 5) Estimates
- Phase 0: 0.5-1d
- Phase 1: 2-3d (interfaces, DI, seam, basic tests)
- Phase 2: 3-4d (Cosmos infra, executor tools, seeding, e2e)
- Phase 3: 2-3d (traces, promotion, admin APIs)
- Telemetry workbook: 0.5d

## 6) Risk & Mitigation
- Misapplied replays → Context Fingerprint + preconditions + relevance guard + TTL.
- Privacy drift → PII redaction by default; block replay when privacy tags indicate.
- Observability gaps → enforce required dims; workbook validation in pilot.

## 7) Acceptance Criteria (DoD)
- Audit report and gap plan committed (this file).
- Feature flags and kill-switch wired and tested.
- Orchestrator pre-LLM seam implemented; replays always traverse Validation; `userOverrides` propagate + audit.
- Cosmos containers created; traces/procedures stored without PII.
- Pilot procedures replay behind flag; e2e .http pass.
- App Insights workbook shows Replay Hit Rate and Token Savings.
- No regressions; CI green.

## 8) References
- `docs/architecture/EMMA-Procedural-Memory-Service.md`
- `docs/architecture/ARCHITECTURE.md`
- `docs/architecture/UNIFIED_SCHEMA.md`
- `docs/reference/ai-first/AI-FIRST-Design-Principles.md`
- `docs/reference/USER-OVERRIDE-ARCHITECTURE.md`
