# EMMA API – Contact Permissions & Test Hardening Report (2025-09-10)

This report summarizes the changes completed to get the Contact assignment and update integration tests passing, along with supporting infrastructure for deterministic test execution.

## Summary of Accomplishments

- Implemented and refined Contact ownership assignment flow and permission enforcement in `src/Emma.Api/Controllers/ContactController.cs`.
- Ensured collaborators have business-only update rights; blocked sensitive fields for non-admins.
- Fixed integration tests by stabilizing the test host configuration (Development environment + in-memory config) and explicit data seeding for collaborators.
- Resolved JSON binding and automatic validation behavior to allow domain-specific 403 responses.
- Added documentation updates and guidance for running integration tests consistently.

## Detailed Changes

- Contact ownership assignment
  - Allowed initial ownership claim (first assignment) when a contact has no `OwnerId` yet. Returns `204 NoContent`.
  - Preserved RBAC for subsequent reassignments: only current owner or admin can reassign.

- Update Contact (PUT) permission enforcement
  - Non-admins are blocked from modifying `OwnerId` or `RelationshipState`.
  - Collaborators can only modify business-safe fields; attempts to update forbidden fields return `403` with `errors.blockedFields`.

- Dev token and test host bootstrapping
  - Integration tests now force `Development` environment and inject:
    - `ALLOW_DEV_AUTOPROVISION=true`
    - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`
  - Ensures `/api/auth/dev-token` is available during tests without depending on external environment variables.

- Collaborator seed reliability
  - Tests insert `ContactCollaborators` via raw SQL including all non-nullable columns (`CreatedAt`, `UpdatedAt`, and all permission flags) to avoid EF `Computed`/`Identity` omission issues.

- JSON & API behavior
  - Enabled `JsonStringEnumConverter` and case-insensitive property binding to support string enums (e.g., `"ServiceProvider"`).
  - Suppressed automatic model-state 400s so controllers can return domain-specific errors (403) as required by tests.

## Files Touched (high-level)

- `src/Emma.Api/Controllers/ContactController.cs`
  - Initial ownership claim logic
  - Forbidden field enforcement (non-admins)
  - Collaborator business-only validation

- `src/Emma.Api/Program.cs`
  - `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true`
  - JSON options: `JsonStringEnumConverter`, case-insensitive binding

- `tests/Emma.Api.IntegrationTests/Contacts_AssignmentAndUpdateTests.cs`
  - Test host builder overrides
  - Collaborator raw SQL seed (with required columns)
  - Diagnostic assertion for forbidden-path debugging (now passing)

## Test Outcomes

- Passed: `Contacts_AssignmentAndUpdateTests.Owner_Assigns_Then_Collaborator_BusinessOnly`
- Passed: `Contacts_AssignmentAndUpdateTests.CrossOrg_Is_Hidden` (previous work, verified)

## Rationale & Trade-offs

- Raw SQL for collaborator seed was chosen to guarantee NOT NULL columns are set in one go, bypassing EF computed/identity insert behavior. This keeps tests deterministic without changing production EF mappings.
- Suppressing automatic model-state 400s allows our controllers to standardize permission errors (403) aligned with product behavior and test expectations.

## Known Warnings (Non-blocking)

- Several EF Core model warnings regarding value comparers and shadow FKs appear in logs; they are unrelated to the fixed tests and can be addressed in a separate modeling pass.
- CA2017 logging placeholders in `EmmaAgentService` and `ContactController` logs were noted; harmless but should be cleaned later.

## Next Steps

- Add minimal onboarding endpoints to satisfy onboarding/profile integration tests:
  - `POST /api/Onboarding/register` → returns a token for test
  - `GET /api/Account/profile` → returns org and email from claims
- Replace raw SQL collaborator seeding with a dedicated test helper/service or dev-only endpoint (optional) for clarity.
- Address EF model warnings and CA2017 logger template mismatches.

## How to Run the Passing Test

```powershell
# Build
dotnet build .\tests\Emma.Api.IntegrationTests\Emma.Api.IntegrationTests.csproj -c Debug -v minimal

# Run the single test
dotnet test  .\tests\Emma.Api.IntegrationTests\Emma.Api.IntegrationTests.csproj \
  -c Debug --no-restore --no-build -v normal \
  --filter "FullyQualifiedName~Contacts_AssignmentAndUpdateTests.Owner_Assigns_Then_Collaborator_BusinessOnly"
```
