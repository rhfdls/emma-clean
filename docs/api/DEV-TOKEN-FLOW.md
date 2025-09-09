# Dev Token Flow: Auto-Provision, Cleanup, and Usage (Development Only)

This document describes how to mint development tokens, auto-provision Users/Organizations, and clean up dev data safely. All endpoints in this doc are available in Development only and require the flag `ALLOW_DEV_AUTOPROVISION=true`.

## Prerequisites

- Environment: Development (`ASPNETCORE_ENVIRONMENT=Development`)
- API base URL: `http://localhost:5000`
- Flag enabled in `appsettings.Development.json` or `.env`:
  - `.env` example:
    ```env
    ALLOW_DEV_AUTOPROVISION=true
    Jwt__Issuer=http://localhost:5000
    Jwt__Audience=emma-dev
    Jwt__Key=a-very-long-strong-shared-secret-at-least-32-characters
    ```

## Endpoints

### Mint Dev Token (with optional auto-provision)
- Path: `POST /api/auth/dev-token`
- Body (examples):
  ```json
  {
    "email": "dev-user@example.local",
    "autoProvision": true
  }
  ```
  ```json
  {
    "userId": "60e757d8-ae9a-4460-8b91-369a86f41a71",
    "orgId": "da60e1ad-acc7-4b1d-91da-307d1b53c0ed",
    "email": "testemail5@example.com",
    "autoProvision": true
  }
  ```
- Behavior:
  - If `autoProvision=true`, creates `Organization` and/or `User` if missing.
  - Always updates `User.LastLoginAt`.
  - Token expiry: 2 hours.
  - Claims include: `sub` (userId), `orgId`, `email`, `scope=verified`.
- Response (`DevTokenResponse`):
  ```json
  {
    "token": "<jwt>",
    "expiresAtUtc": "2025-09-09T12:34:56Z",
    "userId": "...",
    "orgId": "...",
    "email": "..."
  }
  ```

### Dev Cleanup (delete helpers)
- Path: `DELETE /api/dev/user/{userId}`
  - Deletes the user. Returns 204 on success, 404 if not found, 409 on FK constraint errors.
- Path: `DELETE /api/dev/org/{orgId}`
  - Deletes the organization. Returns 204 on success, 404 if not found, 409 on FK constraint errors.

## Files and Code References

- Controller (mint): `src/Emma.Api/Controllers/DevAuthController.cs`
  - DEV + flag gate: `env.IsDevelopment()` and `ALLOW_DEV_AUTOPROVISION=true`
  - Auto-provision `Organization` and `User` when `autoProvision=true`
  - Updates `User.LastLoginAt`
  - Returns `DevTokenResponse`
- DTOs: `src/Emma.Api/Contracts/Auth/DevTokenDtos.cs`
  - `DevTokenRequest`, `DevTokenResponse`
- Dev cleanup controller: `src/Emma.Api/Controllers/DevAdminController.cs`
  - DEV + flag gate
  - DELETE `/api/dev/user/{userId}`
  - DELETE `/api/dev/org/{orgId}`
- Ready/Liveness:
  - `/live` → no checks
  - `/ready` → fails when EF pending migrations exist (`db_migrations` health check)

## Testing via HTTP File

- `tests/Emma.Api.IntegrationTests/dev-flows.http` includes copy/paste requests for:
  - Mint token with auto-provision (generated ids)
  - Mint token with known ids
  - Cleanup user/org

## Troubleshooting

- 401 invalid_token: Ensure `Jwt` Issuer/Audience/Key match between `.env` and `appsettings.Development.json`. Regenerate token after changes.
- 403 User not found: Use `autoProvision=true` or provide an existing `userId`.
- 500 column does not exist (42703): Run `/ready`—if Unhealthy, apply EF migrations. Ensure DB equals the current schema.

## Production Safety

- Dev token minting and cleanup endpoints return 404 unless both conditions hold:
  - `env.IsDevelopment()` is true.
  - `ALLOW_DEV_AUTOPROVISION=true`.
- Never enable the flag in Production.
