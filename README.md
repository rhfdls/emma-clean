# EMMA - AI Orchestrator for Real Estate

EMMA (Enhanced Multi-Modal AI) is an AI orchestrator designed to analyze call transcripts and provide strategic workflows for real estate professionals.

## Project Structure

```
emma/
├── Emma.Api/              # Web API project
├── Emma.Core/             # Core business logic and models
├── Emma.Data/             # Data access and database context
├── Emma.Web/              # Frontend application (React)
├── docker-compose.yml     # Docker Compose configuration
└── env.docker             # Environment variables for Docker
```

## Prerequisites

- Docker Desktop
- .NET 8.0 SDK
- Node.js (for frontend development)

## Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/emma.git
   cd emma
   ```

2. **Set up environment variables**
   - Copy `env.template` to `.env`
   - Update the `.env` file with your Azure OpenAI credentials

3. **Start the application with Docker**
   ```bash
   docker-compose up -d --build
   ```

4. **Access the application**
   - API: http://localhost:5262/swagger
   - Frontend: http://localhost:3000
   - PostgreSQL: localhost:5432
   - Azurite (Storage Emulator): http://localhost:10010

## Features

- Call transcript analysis

## Development

### Testing

For detailed information on testing, especially for Azure OpenAI SDK integration, see the [Testing Guide](./docs/TESTING.md).

### Running the Application

#### Using Docker (Recommended)
```bash
docker-compose up -d --build
```

#### Running Locally
1. Start the required services:
   ```bash
   docker-compose up -d postgres azurite
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

To apply database migrations:
```bash
cd Emma.Api
dotnet ef database update --project ../Emma.Data
```

## Configuration

### Environment Variables

Copy `env.template` to `.env` and update the values as needed.

### Azure OpenAI Setup

1. Create an Azure OpenAI resource in the Azure Portal
2. Deploy a model (e.g., gpt-4)
3. Update the following environment variables:
   ```
   AZURE_OPENAI_ENDPOINT=your-endpoint
   AZURE_OPENAI_KEY=your-key
   AZURE_OPENAI_DEPLOYMENT=your-deployment-name
   ```

## API Documentation

Once the API is running, access the Swagger documentation at:
http://localhost:5262/swagger

## Features

- Call transcript analysis
- Strategic workflow generation
- Integration with existing services
- Real estate specific insights

## AI Action System

The AI Action System processes natural language messages and converts them into structured actions that can be executed by the EMMA platform.

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
1. Receiving messages and conversation context
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
- Cosmos DB
- FollowUpBoss API
- Microsoft.AspNetCore.RateLimiting (for rate limiting)
- Microsoft.Extensions.Http (for HTTP client factory)

## Getting Started

1. Clone the repository
2. Install dependencies
3. Configure environment variables
4. Run the service
