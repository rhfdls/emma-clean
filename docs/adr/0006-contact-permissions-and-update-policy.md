# 0006 Contact Permissions and Update Policy

- Status: Accepted
- Date: 2025-09-10
- Stakeholders: API Team, Core/Infra Team, QA, Security/Compliance
- Related: PRs for ContactController updates and test hardening; Integration tests in `tests/Emma.Api.IntegrationTests/Contacts_AssignmentAndUpdateTests.cs`

## Context
The platform needs a clear, enforceable policy for contact ownership and field-level updates to support collaboration while protecting sensitive attributes. Integration tests highlighted specific behaviors we must guarantee:

- A newly created contact often has no owner and should allow an initial ownership claim.
- Reassignment must be restricted to current owner or admins.
- Collaborators should be limited to business-only fields and blocked from sensitive changes.
- Non-admins (including the current owner) should not be able to modify `OwnerId` or `RelationshipState` via the general update endpoint.

The tests also revealed infrastructure needs to produce deterministic behavior in CI/dev:
- Dev token minting must be available during tests without relying on host env.
- JSON binding must accept string enums, and model state should not mask domain-specific permission responses.

## Decision
1. Initial Ownership Claim
   - If a `Contact` has no `OwnerId`, the first assignment request is allowed and returns `204 NoContent`.
   - Implemented in `CreateUserAssignment` in `ContactController`.

2. Reassignment Rules (RBAC)
   - Only the current owner or an admin may reassign ownership.
   - Non-owners/non-admins receive `403 Forbidden`.

3. Forbidden Fields for Non-Admins
   - In `UpdateContact` (PUT), block updates to sensitive fields by non-admins, specifically:
     - `OwnerId`
     - `RelationshipState`
   - Returns `403 Forbidden` with `errors.blockedFields` listing forbidden properties.

4. Collaborator Business-Only Updates
   - Collaborators can edit allowed business fields only; attempts to modify forbidden fields return `403` with `errors.blockedFields`.

5. API Behavior for Deterministic Permission Responses
   - Suppress automatic 400 model-state responses via `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true` so controllers can return domain-specific `403`.
   - Configure JSON options to enable string enum binding and case-insensitive property names.

6. Test Host Bootstrapping (Documentation-only supporting detail)
   - Integration tests force `Development` environment and inject `ALLOW_DEV_AUTOPROVISION=true` and JWT settings via in-memory configuration to ensure `/api/auth/dev-token` is always available during tests.

## Consequences
- Positive
  - Clear, testable rules for ownership assignment and updates.
  - Protection of sensitive fields (`OwnerId`, `RelationshipState`) from non-admin updates.
  - Deterministic test behavior across machines/environments.
- Negative / Risks
  - Additional branching in controller logic.
  - Suppressing automatic model-state 400s shifts validation responsibility into controllers.
  - Collaborator permissions are enforced at controller level; future refactors may centralize into services/policies.

## Alternatives Considered
- Always require admin for any ownership change (including initial claim)
  - Rejected: slows onboarding, adds friction.
- Allow current owner to modify `RelationshipState`
  - Rejected: tests and policy require treating it as sensitive; keep admin-only via general update endpoint.
- Enforce collaborator/business-only permissions via data annotations
  - Rejected: cross-field/role-aware logic fits better in controller/service layer.

## Rollback Plan
- Revert controller changes to pre-ADR state if necessary.
- Re-enable automatic model-state validation if domain-level 403 responses are not required.
- Update tests to match revised behavior (e.g., allowing `RelationshipState` updates by owner) if policy changes.
