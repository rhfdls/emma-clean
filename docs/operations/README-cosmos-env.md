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

---

**Questions?** See the full documentation or ask the team!
