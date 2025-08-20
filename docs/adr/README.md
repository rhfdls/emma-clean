# Architectural Decision Records (ADR)

ADRs capture significant architectural decisions and their context, consequences, and status.

- Location: `docs/adr/`
- Filename pattern: `NNNN-title.md` (e.g., `0001-decision-record-template.md`)
- Status values: Proposed | Accepted | Rejected | Superseded
- Link ADRs from PR descriptions when relevant.

## When to write an ADR
- New agent types or orchestration changes
- Datastore choice/indexing/partitioning changes
- Security posture or tenancy invariants updates
- Cross-service interfaces or contracts

## How to create
1. Copy `template.md` to `docs/adr/NNNN-your-title.md`.
2. Fill all sections concisely.
3. Commit with the related change and reference it in the PR.
