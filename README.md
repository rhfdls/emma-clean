# Emma AI Platform

EMMA (Enhanced Multi-Modal AI) is an AI orchestrator designed to analyze call transcripts and provide strategic workflows for real estate professionals.

## Project Structure

```text
emma/
├── Emma.Api/              # Web API project
├── Emma.Core/             # Core business logic and models
├── Emma.Data/             # Data access and database context
├── Emma.Web/              # Frontend application (React)
├── docs/                  # Documentation
└── .env                   # Environment variables (not in source control)
```

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+
- Azure account with:
  - Azure PostgreSQL
  - Azure Cosmos DB
  - Azure OpenAI resource

## Getting Started

1. **Clone the repository**

   ```powershell
   git clone https://github.com/yourusername/emma.git
   cd emma
   ```

2. **Create a `.env` file**

   - Copy `env.template` to `.env`
   - Update the `.env` file with your Azure service connection strings and credentials
   - Run `./load-env.ps1` to load environment variables into your current session

3. **Run the setup script**

   ```powershell
   ./dev-setup.ps1
   ```

   - Apply database migrations to Azure PostgreSQL

4. **Access the application**

   - API: [http://localhost:5262/swagger](http://localhost:5262/swagger)
   - Frontend: [http://localhost:3000](http://localhost:3000)

## Features

- Call transcript analysis and AI-powered insights
- Agent-based workflows for real estate professionals
- Azure OpenAI and Azure Cosmos DB integration
- Subscription and organization management

## Development

### Testing

For detailed information on testing, especially for Azure OpenAI SDK integration, see the [Testing Guide](./docs/development/TESTING.md).

### Running the Application

#### Running With Azure Services

1. Load environment variables:

   ```powershell
   ./load-env.ps1
   ```

2. Run the API:

   ```bash
   cd Emma.Api
   dotnet run
   ```

3. Run the frontend (in a new terminal):

   ```bash
   cd Emma.Web
   npm install
   npm start
   ```

### Database Migrations

To apply database migrations to Azure PostgreSQL:

```bash
cd Emma.Api
dotnet ef database update --project ../Emma.Data
```

> Note: While it's now possible to run migrations directly on the host, for backwards compatibility the team may still use Docker-based migration commands in some contexts.

## Configuration

### Environment Variables

Copy `env.template` to `.env` and update values. See [Infrastructure](./docs/operations/infrastructure.md) for details on required Azure services.

> Note: The Emma AI Platform currently supports both `.env` files and docker-compose.yml for configuration. This dual approach is intentionally maintained during the transition period.

### Configuration Precedence

At runtime and during EF design-time, configuration sources are applied in this order (last wins):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development)
4. Environment Variables

Implication: environment variables will override values from JSON files. Be cautious when debugging connection strings or credentials.

Required JWT settings for local dev (used by `src/Emma.Api/Controllers/DevAuthController.cs`):

```json
{
  "Jwt": {
    "Issuer": "http://localhost",
    "Audience": "emma-local",
    "Key": "a-very-long-dev-only-secret"
  }
}
```

### Troubleshooting: Stale Environment Variables Shadowing JSON

Symptoms: the app logs or behavior indicate it is using an old value (e.g., Postgres password) despite updates to `appsettings.json`.

Checklist:

- Clear or update the corresponding OS-level environment variable (User and Machine scopes).
- If using `.env`, reload it in the current shell (e.g., `./load-env.ps1`).
- Verify `appsettings.Development.json` and `appsettings.json` are consistent.
- Check any secret stores or deployment variables that might be injected by your run profile.

Verification:

- Start the API and hit health endpoints (`/api/health/postgres`).
- Check logs for masked connection strings and confirm the expected source values are in effect.

### Azure Service Setup

1. Configure Azure PostgreSQL for relational data storage
2. Set up Azure Cosmos DB for AI-related data and RAG content
3. Deploy Azure OpenAI resources for AI capabilities
4. Update the environment variables in your `.env` file

```bash
AZURE_OPENAI_ENDPOINT=your-endpoint
AZURE_OPENAI_KEY=your-key
AZURE_OPENAI_DEPLOYMENT=your-deployment-name
```

## API Documentation

API documentation is available via Swagger UI at [http://localhost:5262/swagger](http://localhost:5262/swagger) when the API is running.
  
- Comprehensive API reference with curl examples: [docs/api/EMMA-API.md](./docs/api/EMMA-API.md)
- OpenAPI specification (YAML): [docs/api/openapi.yaml](./docs/api/openapi.yaml)

## Additional Documentation

📚 **Comprehensive documentation is now organized in the [`docs/`](./docs/) directory:**

### Quick Links by Category:
- **🏗️ [Architecture](./docs/architecture/)** - System design and technical architecture
- **🔒 [Security](./docs/security/)** - Security, privacy, and compliance  
- **⚙️ [Operations](./docs/operations/)** - Deployment and operational guides
- **🤖 [Agents](./docs/agents/)** - Agent system documentation
- **👨‍💻 [Development](./docs/development/)** - Development guides and processes
- **📋 [Project Management](./docs/project-management/)** - Tasks, roadmaps, and planning
- **📚 [Reference](./docs/reference/)** - Data dictionary, contracts, and specifications

### Docs index
- AI-assisted contribution guide: [docs/reference/AI-assisted-contribution-guide.md](./docs/reference/AI-assisted-contribution-guide.md)
- Assistant brief: [.windsurf/assistant-brief.md](./.windsurf/assistant-brief.md)
- AI contributing guide: [.windsurf/CONTRIBUTING-AI.md](./.windsurf/CONTRIBUTING-AI.md)

### Essential Documents:
- [📖 Documentation Index](./docs/README.md) - Complete documentation guide
- [🏗️ System Architecture](./docs/architecture/EMMA-AI-ARCHITECTURE-GUIDE.md) - Comprehensive architecture guide
- [📚 Data Dictionary](./docs/reference/EMMA-DATA-DICTIONARY.md) - Official terminology and business rules
- [🔒 Security Guide](./docs/security/PRIVACY_IMPLEMENTATION_GUIDE.md) - Privacy and security implementation
- [⚙️ Cloud Setup](./docs/operations/CLOUD_SETUP.md) - Azure deployment guide
- [📋 Current Tasks](./docs/project-management/TODO.md) - Active development tasks

## Licensing

This project is proprietary and confidential.

## AI Action System

The AI Action System processes natural language messages and converts them into structured actions that can be executed by the EMMA platform.

## Fulltext Interaction API & Demo Integration

### New Endpoints
- `POST /api/fulltext-interactions`: Save a fulltext document (transcript, email, sms, etc.) to CosmosDB
- `GET /api/fulltext-interactions?agentId=...`: Query fulltext documents by agent, contact, type, and date range

### Usage Example
- Use the updated demo UI to paste a transcript or message and save it to CosmosDB
- Query all interactions for the demo agent via the UI

### Integration & Testing Notes
- Service layer (`FulltextInteractionService`) enables future business logic, logging, and tracing
- Integration test: `Emma.Api/IntegrationTests/FulltextInteractionControllerTests.cs` demonstrates POST/GET usage
- CosmosDB configuration via `.env` and documented in `README-cosmos-env.md`
- For microservice extensibility, consider splitting CosmosDB logic into a dedicated service in the future

### Security & Compliance
- Endpoints require authentication (RBAC recommended)
- All data is indexed for fast retrieval by agent, contact, type, and timestamp

### EmmaAction Class
The `EmmaAction` class represents an action determined by the AI. It includes:
- `Action`: The type of action to perform (SendEmail, ScheduleFollowup, None)
- `Payload`: Additional data required to execute the action

### Expected JSON Format
The AI should return JSON in this format:
```json
{
    "action": "sendemail|schedulefollowup|none",
    "payload": "Additional data for the action"
}
```

### EmmaAgentService
The `EmmaAgentService` handles:
1. Receiving messages and interaction context
2. Sending them to Azure OpenAI
3. Parsing the response into an `EmmaAction`
4. Executing the appropriate action
5. Returning the result

#### Logging
The service logs:
- Correlation ID for request tracing
- Message processing start/end
- AI response details
- Action execution results

#### Error Handling
- Validates AI responses
- Handles API errors gracefully
- Preserves correlation IDs for debugging

## Dependencies

- OpenAI API
- Azure Functions
- FollowUpBoss API
- Microsoft.AspNetCore.RateLimiting (for rate limiting)
- Microsoft.Extensions.Http (for HTTP client factory)

## Getting Started

1. Clone the repository
2. Install dependencies
3. Configure environment variables
4. Run the service
