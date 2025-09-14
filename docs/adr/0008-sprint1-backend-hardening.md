# ADR-0008: Sprint 1 Backend Hardening for Org/User/Contact CRUD

Date: 2025-09-13
Status: Accepted

## Context
We need to finalize backend guardrails for Organization, User, and Contact CRUD while maintaining tenant isolation and consistent error semantics. We also want a minimal, low-risk change to open a PR for review without destabilizing the codebase.

## Decision
- Enforce tenant scoping (OrganizationId) on all relevant endpoints using orgId claim from JWT.
- Keep admin endpoints (e.g., user management) behind `VerifiedUser` + `OrgOwnerOrAdmin`.
- Standardize non-2xx responses to RFC7807 ProblemDetails.
- Add indexes for common filters (OrgId, OwnerId, RelationshipState) without schema churn.
- Add XML summaries and `[ProducesResponseType]` on controllers for better Swagger.
- Subsequent step: Centralize Contact update guardrails in a `ContactService` that returns ProblemDetails with `errors.blockedFields` for forbidden collaborator edits (ownership, relationshipState, PII, consent).

## Rationale
- Keeps `main` stable while introducing guardrails in a controlled, testable manner.
- Improves performance with safe indexes only.
- Aligns with EMMA API standards and UNIFIED_SCHEMA enforcement (privacy via Interaction, tenant scoping, ProblemDetails).

## Consequences
- Controllers reflect tenant scoping and improved documentation.
- Future PRs will add the `ContactService` and tests for blockedFields and tenant scoping.

## References
- docs/architecture/UNIFIED_SCHEMA.md
- docs/api/EMMA-API.md
- .github/workflows/adr-schema-enforce.yml
