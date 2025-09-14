---
status: Proposed
title: Contact Archive vs Hard Delete
date: 2025-09-14
---

# Context
Contacts are PII-bearing entities with multiple dependents (e.g., Interactions, Messages, EmailAddresses, PhoneNumbers, Tasks, StateHistory). We need to support both reversible business lifecycle management (Archive) and irreversible privacy erasure (Hard Delete) while maintaining tenant isolation, RBAC, and RFC7807 error standards.

This ADR aligns with:
- docs/architecture/UNIFIED_SCHEMA.md (privacy via Interaction tags, contact-centric linking)
- docs/api/EMMA-API.md (JWT auth, org scoping, ProblemDetails)
- ADR 0007 (schema change policy)

# Decision
Support two flows:

1) Archive (soft delete)
- Mark `Contact.IsArchived = true` and set `ArchivedAt`.
- Exclude from default lists and active workflows (assignment, NBA, agents).
- Interactions and analytics remain; operation is reversible (Restore clears flags).

2) Hard Delete (privacy erasure)
- Irreversibly purge the Contact and PII-bearing dependents in a transaction.
- Only OrgOwner/Admin may execute; requires a non-empty reason.
- Store a non-PII audit event row. Do not log PII (use `traceId`).

# Consequences
- UI/UX defaults to Archive; Hard Delete requires extra confirmation + reason.
- Queries/indexes/NBA exclude archived by default.
- Backups/retention policies must respect erasure requests (beyond scope of this ADR).

# Schema (additive)
- Contacts:
  - `IsArchived boolean not null default false`
  - `ArchivedAt timestamptz null`
  - `DeletedAt timestamptz null`
  - `DeletedByUserId uuid null`
  - Index: `(OrganizationId, IsArchived, OwnerId)`
- AuditEvents (non-PII):
  - `Id uuid PK`
  - `OrganizationId uuid`
  - `ActorUserId uuid null`
  - `Action text` (e.g., `ContactErased`)
  - `OccurredAt timestamptz`
  - `TraceId text null`
  - `DetailsJson jsonb null`
- Explicit cascade mapping: `Interaction → Contact` uses `OnDelete(Cascade)` to prevent orphans.

# API
- PATCH `/api/contacts/{id}/archive` → 204
- PATCH `/api/contacts/{id}/restore` → 200 Contact DTO
- DELETE `/api/contacts/{id}?mode=hard&reason=...` → 204
  - VerifiedUser + OrgOwner/Admin, tenant-scoped, requires `reason`.
  - Emits AuditEvent row.
- GET `/api/contacts` excludes archived by default; `includeArchived=true` allowed for Admins.

# RBAC & Errors
- Archive/Restore: Admin-only (OrgOwner/Admin). Hard Delete: Admin-only + reason.
- RFC7807 ProblemDetails via `ProblemFactory`:
  - 400 missing reason, 403 RBAC, 404 cross-tenant, 409 archive/restore conflicts.

# Tests
- Integration: archive/restore flows; hard delete cascade; RBAC failures; cross-tenant 404; GET includeArchived for Admins.
- Unit: conflict cases for archive/restore.

# Status
Proposed → Accept on review. Implemented via migration `S2_AddArchiveAndAudit` and controller/service changes.

# Migration/Implementation Notes
- Migration file: `src/Emma.Infrastructure/Migrations/20250914_AddArchiveAndAudit.cs` (PostgreSQL).
- Model updates:
  - `src/Emma.Models/Models/Contact.cs` (+IsArchived, ArchivedAt, DeletedAt, DeletedByUserId)
  - `src/Emma.Models/Models/AuditEvent.cs` (new)
- DbContext updates:
  - `src/Emma.Infrastructure/Data/EmmaDbContext.cs` (DbSet<AuditEvent>, Contact archive config, composite index, explicit `Interaction → Contact` cascade, AuditEvent config)

# Open Items (tracked)
- Frontend UX for Archive/Restore/Hard Delete (reason capture, double-confirmation, admin only).
- Retention strategy for backups and external storage (blob transcripts, search indices).
- Optional: legal hold ProblemDetails type for 409 in future.
