# EMMA - AI Orchestrator for Real Estate

EMMA (Enhanced Multi-Modal AI) is an AI orchestrator designed to analyze call transcripts and provide strategic workflows for real estate professionals.

## Project Structure

```
emma/
├── src/
│   ├── Emma.Core/           # Core AI analysis logic
│   ├── Emma.Services/       # Service implementations
│   ├── Emma.Data/          # Data models and storage
│   └── Emma.Integration/   # Integration with other services
├── tests/
│   └── Emma.Tests/         # Unit and integration tests
└── docs/                   # Documentation
```

## Features

- Call transcript analysis

## Running PostgreSQL with Docker Compose

This project provides a `docker-compose.yml` for running a local PostgreSQL instance for development.

### Steps:

1. Ensure Docker Desktop is installed and running.
2. In the project root, start PostgreSQL:
   ```sh
   docker-compose up -d
   ```
3. The database will be available at `localhost:5432` (or `host.docker.internal:5432` from inside other Docker containers).
   - **User:** `postgres`
   - **Password:** `postgres`
   - **Database:** `emma`

### .NET Connection String Example

```
Host=host.docker.internal;Port=5432;Database=emma;Username=postgres;Password=postgres
```

You can now run your .NET API and it will connect to this PostgreSQL instance.

- Strategic workflow generation
- Integration with existing services
- Real estate specific insights

## Dependencies

- OpenAI API
- Azure Functions
- Cosmos DB
- FollowUpBoss API

## Getting Started

1. Clone the repository
2. Install dependencies
3. Configure environment variables
4. Run the service
