---
title: "ADR-0007: Schema Change Policy"
status: Accepted
date: 2025-09-13
context: EMMA AI Platform, multi-tenant CRM
---

# ADR-0007: Schema Change Policy

## Decision

We adopt the following Schema Change Policy:

- ✅ Safe without approval:
  - Index creation/removal
  - Seed data updates
  - Non-persisted database views

- ⚠️ Require ADR + approval:
  - New columns
  - New tables
  - New relationships
  - New enum values
  - Data type widening (e.g., `varchar(50)` → `varchar(200)`)
  - Non-breaking constraints

- ❌ Not allowed without compatibility plan:
  - Dropping or renaming columns
  - Narrowing data types
  - Altering primary keys
  - Removing enum values

Every approved schema change must:

1. Include an EF Core migration in `src/Emma.Infrastructure/Migrations`.
2. Update `AppDbContextSchema.md` and `EMMA-DATA-DICTIONARY.md` to reflect the new structure.
3. Be tied to a PR with architectural review (OrgOwner/Architect required reviewer).
4. Include a migration path for data safety (default values, backfills, compatibility layer).

## Rationale

- Tenant isolation and privacy enforcement depend on schema correctness (`OrganizationId`, RBAC, collaborator restrictions).
- AI workflows (NBA, user overrides, context providers) rely on consistent entity models.
- Compliance (GDPR, FINTRAC, MLS, etc.) requires clear traceability of PII and consent fields.
- We need agility to evolve the schema (new subscription entities, audit tables, enum states) while preventing accidental drift.

## Consequences

- Developers cannot merge schema-affecting migrations directly to `main`.
- Lightweight changes (indexes, seed data) can be merged without a new ADR, but still require code review.
- More effort is required up-front (writing ADRs + review), but this avoids costly data breaches or schema regressions.
- Documentation (`AppDbContextSchema.md`, `EMMA-DATA-DICTIONARY.md`) and migrations remain the single source of truth.

## Example Workflow

Developer needs a new `ConsentType` column on `Contacts`.

Developer opens a PR with:

- EF migration in `src/Emma.Infrastructure/Migrations`
- Updated `AppDbContextSchema.md` and `EMMA-DATA-DICTIONARY.md`
- Draft ADR describing the change

Architect reviews ADR + PR → approves → merge allowed.

Deployment applies migration safely; docs remain aligned.

## Related Documents

- `docs/architecture/ARCHITECTURE.md` – EMMA AI finalized architecture
- `docs/architecture/AppDbContextSchema.md` – authoritative schema reference
- `docs/architecture/EMMA-DATA-DICTIONARY.md` – entity and field definitions
- `docs/reference/ENUMS-REFERENCE.md` – enum values and rules
- `docs/reference/USER-OVERRIDE-ARCHITECTURE.md` – validation and override rules
