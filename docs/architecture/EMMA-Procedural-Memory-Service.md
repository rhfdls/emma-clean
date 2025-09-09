# EMMA Procedural Memory Service — Architecture Extension

Owner: Architecture Guild · Authors: EMMA Staff+ (Pair‑Architect), Platform Team
  Version: 0.9 (Draft)
  Date: 2025‑08‑28
  Status: PROPOSAL → Review → Pilot

## 1. Executive Summary

Procedural Memory Service (PMS) adds a reusable “skill trace” layer to EMMA so common workflows (e.g., schedule follow‑up SMS, log call → summarize → assign NBA) can be executed via replay of a validated procedure instead of re‑prompting a large model each time.
Outcomes:

- Cost ↓: Fewer LLM tokens for routine, previously‑validated behaviors.
- Latency ↓: Sub‑second execution for common flows using pre‑compiled procedures.
- Safety ↑: Replays are gated by our existing Validation + User‑Override systems (no bypass).
- Learning ↑: User overrides and outcomes continuously adapt procedures per tenant/user.

## 2. Goals & Non‑Goals

### Goals
Introduce a production‑grade, multi‑tenant procedural memory layer with replay + adaptive refinement.
Integrate with Orchestrator (pre‑inference hook) and the Three‑Tier Validation Pipeline (post‑selection, pre‑execute).
Persist skill traces in Cosmos DB with governance (versioning, audit, rollout flags).
Honor User Overrides and privacy tags end‑to‑end; ensure auditability and rollback.

### Non‑Goals (Phase 1)
No vendor lock: PMS is model‑agnostic (works with Azure AI Foundry Agents and direct Azure OpenAI).
No fine‑tuning/training in v1; this is trace capture + replay + light editing, not model training.
No custom Dev UI yet—Phase 1 surfaces within Agent Factory and internal admin screens.

## 3. Context & Alignment

Agentic pattern: EMMA already splits Inner‑World (context, evaluation) and Outer‑World (actuation) agents; PMS gives them a shared Skill Catalog to reduce redundant LLM reasoning.
Validation First: PMS never bypasses Relevance → Risk/Confidence → Override checks; it feeds them with rich trace metadata.
Tenant‑Aware: Skill traces are scoped (Global → Tenant → User) with inheritance and guardrails.

## 4. High‑Level Architecture

### 4.1 Components

PMS Router
Intercepts Orchestrator requests (pre‑LLM) with a ProcedureLookupRequest (intent, action type, context hash).
Returns either a ReplayPlan (procedure id + parameter map) or Miss → fall back to LLM plan.

Skill Trace Store (Cosmos)
Containers: procedures, procedure-traces, procedure-versions, procedure-insights.
Partition by tenantId; secondary keys on actionType, capabilities, tags.

Skill Compiler
Normalizes captured traces → typed graph: Steps, Guards, Params, Effects.
Emits Safe‑Replay Plan with preconditions and redaction rules.

Matcher
k‑NN (vector) + rules (exact keys) on: actionType, contact state, tags, channel, industry, riskBand, and Context Fingerprint (hash of salient fields).

Executor
Deterministic runner for Tools/Effects (e.g., CreateInteraction, SendSms, CreateTask, UpsertContactTag).
Emits telemetry + ValidationRequest prior to any side‑effect.

Feedback Loop
Consumes outcomes (success/failure, overrides, ratings), updates procedure-insights and (optionally) promotes traces to new versions.

Governance Hooks
Feature flags, rollout rings, kill‑switch, audit write‑through, PII redaction, RBAC.

### 4.2 Integration Points

Orchestrator (Pre‑Plan) → PMS Router (lookup) → Replay or LLM Plan.
Validation Pipeline (Relevance → Risk/Confidence → Override) consumes ReplayPlan metadata and blocks/escorts as usual.
User Overrides flow recorded back into the trace (learning signal + policy updates).
Telemetry: App Insights dimensions: procedureId, version, replay, fallback, tenantId, orgId, riskBand.

## 5. Data Model (v1)
\
### 5.1 Cosmos DB (JSON Schemas)

procedures (authoritative compiled procedures)

```json
{
  "id": "proc_72ad…",
  "tenantId": "{guid}",
  "scope": "Global|Tenant|User",
  "name": "Schedule SMS follow-up",
  "actionType": "schedulefollowup",
  "capabilities": ["sms", "nba", "calendar"],
  "preconditions": {"contactState": ["Lead","Prospect"], "privacyTagsNot": ["PERSONAL"]},
  "parameters": {"delayHours": {"type":"int","default": 24}},
  "steps": [
    {"tool": "CreateInteraction", "args": {"type":"task","status":"scheduled"}},
    {"guard": "RelevanceCheck"},
    {"tool": "SendSms", "argsTemplate": "{{templates.followup}}"}
  ],
  "version": 3,
  "rollout": {"ring":"pilot","enabled": true},
  "audit": {"createdBy":"system","createdAt":"2025-08-10T12:00:00Z"}
}
```

procedure-traces (raw captured runs)

```json
{
  "id": "trace_bf18…",
  "procedureCandidate": "schedulefollowup",
  "tenantId": "{guid}",
  "contextFingerprint": "cfp:…",
  "observations": {"inputs": {...}, "llmRationales": ["…"], "tools": ["SendSms"]},
  "outcome": {"status":"success","overrideMode":"RiskBased","approval": true},
  "links": {"interactionId":"…","contactId":"…"}
}
```

### 5.2 PostgreSQL (relational)
No new tables required in v1.

Extend Interaction write path to attach ProcedureReplayRef (nullable) so replays are queryable in analytics.

## 6. Service Interfaces (Draft)

```csharp
public record ProcedureLookupRequest(
    Guid TenantId,
    Guid OrganizationId,
    Guid? UserId,
    string ActionType,
    string Channel,
    string Industry,
    string RiskBand,
    string ContextFingerprint,
    IDictionary<string, object>? Params,
    IDictionary<string, object>? UserOverrides);

public record ReplayPlan(
    string ProcedureId,
    int Version,
    IDictionary<string, object> BoundParameters,
    IReadOnlyList<ReplayStep> Steps,
    bool RequiresValidation);

public interface IProceduralMemoryService
{
    Task<ReplayPlan?> TryGetReplayAsync(ProcedureLookupRequest request, CancellationToken ct);
    Task CaptureTraceAsync(ProcedureTrace trace, CancellationToken ct);
    Task PromoteAsync(string traceId, PromoteOptions options, CancellationToken ct);
}

public interface IProcedureExecutor
{
    Task<ExecutionResult> ExecuteAsync(ReplayPlan plan, ValidationContext vctx, CancellationToken ct);
}
```

## 7. Orchestrator & Validation Pipeline Wiring

### 7.1 Orchestrator (pre‑LLM)

Build ProcedureLookupRequest from intent & context.
PMS.TryGetReplayAsync → if ReplayPlan != null, go to 7.2; else call LLM planner (existing path).
On LLM plan success, capture trace (observability mode) for potential promotion.

### 7.2 Validation Pipeline (existing, unchanged contracts)

Relevance: replay includes preconditions + context fingerprint; if drifted → cancel or fallback.
Risk/Confidence: replay carries prior confidence bands; can down‑weight if stale.
Override: obey OverrideMode (AlwaysAsk/NeverAsk/RiskBased/etc.).
Outcome: log ProcedureReplayRef to Interaction/Message.

## 8. Developer Experience & Config

Feature Flags: ProceduralMemory.Enabled, ring‑scoped (pilot, org-allowlist).
Prompt/Enum Providers: use existing hot‑reload infra for templates and risk bands; procedure metadata stored in Cosmos (not in Enum/Prompt configs).
Windsurf: add Workspace Brief prompts and Cascade tasks to generate replay candidates and tests; provide .http suites for regression.

## 9. Telemetry & Observability

**Key Metrics**
Replay Hit Rate (%), Token Savings, Median Latency (replay vs LLM), Override Rate, Relevance‑Block Rate, Fallback Rate, Per‑Tenant Cost/1k interactions.
**Logging**
Correlate traceId, procedureId, interactionId, approvalRequestId.
**Dashboards**
Replay Efficiency, Risk/Override Heatmap, Drift Alerts (fingerprint mismatch).

## 10. Rollout Plan

Capture‑Only (2 weeks): store traces, no replay; score candidates.
Pilot Replay (allowlisted orgs): enable schedulefollowup, log‑call‑summary only.
Ring Expansion: enable more procedures by action type; watch override spikes.
General Availability: default‑on for low‑risk actions; high‑risk remain LLM‑first.

## 11. Risks & Mitigations

Stale or Misapplied Procedures → Context Fingerprint + Relevance Gate + Expiry TTL.
Policy/Privacy Drift → governance checks in compiler; privacy tags block replay.
Overfitting to a Tenant → scope rules; Global defaults remain available.
Debuggability → full trace → plan → effect lineage in logs & App Insights.

## 12. Testing Strategy (additions)

Unit: matcher determinism; compiler idempotency; executor step semantics.
Contract: Orchestrator ↔ PMS (TryGetReplayAsync, ReplayPlan).
Integration: replay vs LLM parity on golden paths (HTTP suites).
E2E: pilot flows behind feature flag; validate token & latency budgets.
Performance: load replay hit‑rate sweeps; drift + fallback behavior.
Safety: override escalation & audit completeness.

## 13. Appendix A — Example Replay Plan (condensed)

```json
{
  "procedureId": "proc_followup_sms",
  "version": 3,
  "boundParameters": {"delayHours": 24, "templateKey": "followup.default"},
  "steps": [
    {"tool":"CreateInteraction","args":{"type":"task","status":"scheduled"}},
    {"guard":"RelevanceCheck","args":{"ctxFields":["contact.state","lastInteractionAgeDays"]}},
    {"tool":"SendSms","argsTemplate":"{{templates.followup}}"}
  ],
  "requiresValidation": true
}
```

## 14. Appendix B — Context Fingerprint (illustrative)

```text
cfp := hash(
  actionType,
  contact.relationshipState,
  industry,
  channel,
  lastInteractionAgeDaysBucket,
  privacyTags,
  tenantPolicyVersion
)
```

## 15. Open Questions

When to auto‑promote traces → procedures (thresholds, human in the loop)?
How to represent multi‑agent joint procedures (composed plans) in v1?
Where to visualize drift and override hotspots (Ops dashboard vs Product UI)?
