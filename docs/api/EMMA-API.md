# EMMA API Reference

This document provides a concise, developer-friendly reference for EMMA API endpoints with authentication steps and example curl requests.

- Base URL (local): <http://localhost:5000>
- All routes below include their full path (prefix already applied in controllers).
- Authentication: Most protected endpoints require a Bearer JWT with the VerifiedUser policy. In Development, mint one via the dev-token endpoint.

> Error responses follow RFC7807 ProblemDetails. See ProblemDetails section below.

## Authentication Setup

- Mint a dev JWT (Development only):

```bash
curl -s -X POST http://localhost:5000/api/auth/dev-token \
  -H "Content-Type: application/json" \
  -d '{"email": "dev.user@example.com"}'
```

Response:

```json
{
  "token": "<JWT>",
  "orgId": "<guid>",
  "userId": "<guid>",
  "email": "dev.user@example.com"
}
```

- Use the token in Authorization header:

```bash
TOKEN="<JWT>"
# example
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/Account/profile
```

---

## Account (`AccountController`)

Route prefix: `api/Account`

- GET /api/Account/profile
  - Purpose: Return current user profile/org. Anonymous allowed; if authed, uses identity.
  - curl:

    ```bash
    curl http://localhost:5000/api/Account/profile
    ```

- POST /api/Account/verify
  - Purpose: Verify user via token (legacy; prefer /api/auth/verify-email).
  - Body: { "token": "..." }
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/Account/verify \
      -H "Content-Type: application/json" \
      -d '{"token": "<token>"}'
    ```

## Agent (`AgentController`)

Route prefix: `api/agent`

- POST /api/agent/suggest-followup
  - Purpose: Dev-only stub that returns a suggestion payload.
  - Auth: Bearer (VerifiedUser), Development environment only.
  - Body (optional): { "contactId": "GUID", "context": "..." }
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/agent/suggest-followup \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d '{"contactId":"00000000-0000-0000-0000-000000000000","context":"Hello"}'
    ```

## Contact (`ContactController`)

Route prefix: `api/Contact`

- POST /api/Contact
  - Purpose: Create a contact.
  - Auth: Bearer (VerifiedUser)
  - Body: ContactCreateDto (e.g., OrganizationId, FirstName, LastName, OwnerId, ...)
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/Contact \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d '{
            "organizationId": "<org-guid>",
            "firstName": "Ava",
            "lastName": "Stone",
            "ownerId": "<user-guid>"
          }'
    ```

- GET /api/Contact/{id}
  - Purpose: Get a contact by id.
  - Auth: Bearer (VerifiedUser)
  - Org scoping: Requires `orgId` claim in JWT; returns 403 if contact's organization doesn't match claim; 400 if claim missing/invalid.
  - curl:

    ```bash
    curl http://localhost:5000/api/Contact/<contact-guid>
    ```

- GET /api/Contact?orgId={orgId}
  - Purpose: List contacts by organization.
  - Auth: Bearer (VerifiedUser)
  - Org scoping: Query `orgId` must match `orgId` in JWT; 403 if mismatch; 400 if claim missing/invalid.
  - curl:

    ```bash
    curl "http://localhost:5000/api/Contact?orgId=<org-guid>"
    ```

- PUT /api/Contact/{id}/assign
  - Purpose: Assign a contact to a user.
  - Auth: Bearer (VerifiedUser)
  - Body: ContactAssignDto { "userId": "GUID" }
  - curl:

    ```bash
    curl -X PUT http://localhost:5000/api/Contact/<contact-guid>/assign \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d '{"userId":"<user-guid>"}'
    ```

## ContactCollaborator (`ContactCollaboratorController`)

Route prefix: `api/contacts/{contactId}/collaborators`

- NOTE: Endpoints are placeholders and return 501 (Not Implemented). `contactId` is a `Guid` route parameter in the controller.

- POST /api/contacts/{contactId}/collaborators
- GET /api/contacts/{contactId}/collaborators

## Dev Auth (`DevAuthController`)

Route prefix: `api/auth`

- POST /api/auth/dev-token
  - Purpose: Issue a dev JWT.
  - Auth: Anonymous; Development only.
  - Body (optional): { "orgId": "GUID", "userId": "GUID", "email": "..." }
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/auth/dev-token \
      -H "Content-Type: application/json" \
      -d '{"email":"dev.user@example.com"}'
    ```

## Enum (`EnumController`)

Route prefix: `api/enums`

- GET /api/enums/{type}
  - Purpose: Return dynamic enum values by type. Cosmos-backed with JSON fallback.
  - curl:

    ```bash
    curl http://localhost:5000/api/enums/PlanType
    ```

## Health (`HealthCheckController`)

Route prefix: `api/health`

- GET /api/health
  - Liveness check.
  - curl:

    ```bash
    curl http://localhost:5000/api/health
    ```

- GET /api/health/cosmos
  - Check Cosmos DB connectivity.
  - curl:

    ```bash
    curl http://localhost:5000/api/health/cosmos
    ```

- GET /api/health/postgres
  - Check PostgreSQL connectivity.
  - curl:

    ```bash
    curl http://localhost:5000/api/health/postgres
    ```

- GET /api/health/cosmos/item
  - Read a dev sample item.
  - curl:

    ```bash
    curl http://localhost:5000/api/health/cosmos/item
    ```

## Interaction (`InteractionController`)

Route prefix: `api/contacts/{contactId}/interactions`

- POST /api/contacts/{contactId}/interactions
  - Purpose: Log a contact interaction. AI analysis currently disabled.
  - Auth: Bearer (VerifiedUser)
  - Body example:

    ```json
    {
      "type": "email",
      "direction": "outbound",
      "subject": "Welcome",
      "content": "Hello from EMMA",
      "consentGranted": true,
      "occurredAt": "2025-08-20T13:00:00Z"
    }
    ```
  
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/contacts/<contact-guid>/interactions \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d '{"type":"email","direction":"outbound","subject":"Welcome","content":"Hello"}'
    ```

- GET /api/contacts/{contactId}/interactions
  - Purpose: List interactions for a contact.
  - Auth: Bearer (VerifiedUser)
  - Org scoping: Requires `orgId` claim in JWT; results filtered by it. 400 if claim missing/invalid.
  - curl:

    ```bash
    curl -H "Authorization: Bearer $TOKEN" \
      http://localhost:5000/api/contacts/<contact-guid>/interactions
    ```

## Onboarding (`OnboardingController`)

Route prefix: `api/Onboarding`

- POST /api/Onboarding/register
  - Purpose: Register an organization and initial user; returns verification token (dev stub).
  - Auth: Anonymous
  - Body example:
  
    ```json
    {
      "organizationName": "Acme Realty",
      "email": "owner@acme.com",
      "password": "Passw0rd!",
      "planKey": "basic",
      "seatCount": 5
    }
    ```
  
  - curl:
  
    ```bash
    curl -X POST http://localhost:5000/api/Onboarding/register \
      -H "Content-Type: application/json" \
      -d '{"organizationName":"Acme Realty","email":"owner@acme.com","password":"Passw0rd!","planKey":"basic","seatCount":5}'
    ```

## Organization (`OrganizationController`)

Route prefix: `api/Organization`

- POST /api/Organization
  - Purpose: Create an organization.
  - Auth: Bearer (VerifiedUser)
  - Body: OrganizationCreateDto
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/Organization \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d '{"name":"Acme Realty","email":"owner@acme.com","ownerUserId":"<user-guid>","seatCount":5}'
    ```

- GET /api/Organization
  - Purpose: List organizations (paged).
  - Auth: Bearer (VerifiedUser)
  - Query: page (default 1), size (default 20)
  - curl:

    ```bash
    curl -H "Authorization: Bearer $TOKEN" \
      "http://localhost:5000/api/Organization?page=1&size=20"
    ```

- GET /api/Organization/{id}
  - Purpose: Get organization by id.
  - Auth: Bearer (VerifiedUser)
  - curl:

    ```bash
    curl -H "Authorization: Bearer $TOKEN" \
      http://localhost:5000/api/Organization/<org-guid>
    ```

### Invitations

- POST /api/Organization/{orgId}/invitations
  - Purpose: Create an invitation for an email to join the org.
  - Auth: Bearer with OrgOwnerOrAdmin policy.
  - Body: CreateInvitationDto { email, role?, expiresInDays?, invitedByUserId? }
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/Organization/<org-guid>/invitations \
      -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
      -d '{"email":"invitee@example.com","role":"Member","expiresInDays":7}'
    ```

- GET /api/Organization/invitations/{token}
  - Purpose: Get an invitation by token.
  - Auth: Public
  - curl:

    ```bash
    curl http://localhost:5000/api/Organization/invitations/<token>
    ```

- POST /api/Organization/invitations/{token}/accept
  - Purpose: Accept an invitation.
  - Auth: Public (token-gated)
  - Responses: 204 No Content on success; ProblemDetails for revoked/expired/not found.
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/Organization/invitations/<token>/accept
    ```

- POST /api/Organization/invitations/{token}/register
  - Purpose: Register user from an invitation and send verification email (dev stub).
  - Auth: Public (token-gated)
  - Body: RegisterFromInvitationDto { firstName?, lastName?/fullName?, password? }
  - Responses: 204 No Content on success; ProblemDetails for revoked/expired/not found/validation.
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/Organization/invitations/<token>/register \
      -H "Content-Type: application/json" \
      -d '{"firstName":"Alex","lastName":"Doe","password":"Passw0rd!"}'
    ```

## Verification (`VerificationController`)

Route prefix: `api/auth`

- POST /api/auth/verify-email
  - Purpose: Verify email via token; idempotent when already verified.
  - Auth: Anonymous
  - Body: { "token": "..." }
  - curl:

    ```bash
    curl -X POST http://localhost:5000/api/auth/verify-email \
      -H "Content-Type: application/json" \
      -d '{"token":"<token>"}'
    ```

---

## Notes

- JWT claims used by policies/queries: `orgId` (GUID), `sub`/`nameidentifier` (user id).
- Some endpoints (e.g., collaborators) are placeholders and return 501.
- Health endpoints are intended for development diagnostics.

---

## ProblemDetails (RFC7807)

- All error responses are standardized using `application/problem+json`.
- Common shapes:
  - 400 Validation failed
  - 401 Unauthorized
  - 403 Forbidden
  - 404 Not Found
  - 409 Conflict

Example:

```json
{
  "type": "about:blank",
  "title": "Validation failed",
  "status": 400,
  "detail": "Token is required"
}
```
