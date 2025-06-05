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

For detailed information on testing, especially for Azure OpenAI SDK integration, see the [Testing Guide](./docs/TESTING.md).

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

Copy `env.template` to `.env` and update values. See [Infrastructure](./docs/infrastructure.md) for details on required Azure services.

> Note: The Emma AI Platform currently supports both `.env` files and docker-compose.yml for configuration. This dual approach is intentionally maintained during the transition period.

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

## Additional Documentation

Additional documentation can be found in the `docs/` directory:

- [Architecture Overview](./ARCHITECTURE.md)
- [Infrastructure Guide](./docs/infrastructure.md)
- [Secret Management](./SECRETS_MANAGEMENT.md)
- [Cloud Setup Guide](./CLOUD_SETUP.md)

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
