# Emma AI Platform: Cosmos DB Environment Setup & Onboarding

## 🚀 Quick Start: Secure Cosmos DB Integration (Docker-Native)

### 1. Environment Variables

- Copy `env.template` to `.env` in the project root:
  ```sh
  cp env.template .env
  ```
- Fill in the real Cosmos DB values in `.env` (do NOT commit `.env`):
  ```env
  COSMOSDB__ACCOUNTENDPOINT=your-cosmosdb-endpoint
  COSMOSDB__ACCOUNTKEY=your-cosmosdb-key
  COSMOSDB__DATABASENAME=your-cosmosdb-database
  COSMOSDB__CONTAINERNAME=your-cosmosdb-container
  ```
- `.env` is git-ignored for security. Never store secrets in source control.

### 2. Docker Compose Usage

- Docker Compose automatically injects these variables into the backend:
  ```yaml
  environment:
    - COSMOSDB__ACCOUNTENDPOINT=${COSMOSDB__ACCOUNTENDPOINT}
    - COSMOSDB__ACCOUNTKEY=${COSMOSDB__ACCOUNTKEY}
    - COSMOSDB__DATABASENAME=${COSMOSDB__DATABASENAME}
    - COSMOSDB__CONTAINERNAME=${COSMOSDB__CONTAINERNAME}
  ```

### 3. Fail-Fast Safety

- The backend will **not start** if any required Cosmos DB env vars are missing.
- You’ll see a clear error message if a variable is missing—fix your `.env` or deployment secrets.

### 4. Developer Workflow

1. Copy `env.template` to `.env`
2. Fill in real values (never commit `.env`)
3. Run `docker-compose up` to start the Emma AI Platform

### 5. Required Seeding (Dev/Test)

Some features require seed data in Cosmos (e.g., dynamic enums/config). If you start with a fresh Cosmos account or container, seed these before running API or tests:

- See `docs/reference/ENUMS-REFERENCE.md` for the list of dynamic enums used in the platform.
- Use the helper script from repo root to (re)populate enum documents:

  ```powershell
  # From repository root
  pwsh ./update-enum-references.ps1
  ```

- If you maintain a separate container for messages or dev samples, ensure any expected fixture data is present as well.

This seeding step is a recurring requirement whenever you reset your CosmosDB environment.

---

**Questions?** See the full documentation or ask the team!
