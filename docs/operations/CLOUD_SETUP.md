# Emma AI Platform - Cloud Development Setup

This document outlines the cloud-based development approach for the Emma AI Platform, which uses Azure cloud services for all infrastructure requirements.

## Overview

The Emma AI Platform development environment now supports a direct connection to Azure cloud services for development. This approach:

1. Provides consistent development environments across the team
2. Eliminates "works on my machine" issues
3. Simplifies onboarding for new developers
4. Aligns development with production environments

## Required Azure Resources

The Emma AI Platform relies on these Azure services:

1. **Azure PostgreSQL**: Primary relational database for structured data
   - Organizations, agents, user accounts, subscriptions
   - Structured interaction data with fixed schema
   - Data requiring ACID transactions

2. **Azure Cosmos DB**: For all AI-based data
   - Full-text conversation history
   - Voice recordings and transcriptions
   - AI agent interaction data
   - RAG (Retrieval Augmented Generation) content
   - Data with variable schema requirements

3. **Azure OpenAI**: Core AI capabilities for natural language processing

4. **Azure AI Foundry**: Extended AI capabilities and agentic workflows

5. **Azure Storage**: File and blob storage
6. **Azure Search**: Search capabilities (optional)

## Setup Instructions

### 1. Set Up Azure PostgreSQL

1. Log in to the [Azure Portal](https://portal.azure.com)
2. Create a new Azure Database for PostgreSQL - Flexible Server
   - Basic tier is sufficient for development
   - Configure firewall rules to allow your IP address
   - Create a database called `emma_dev`
   - Enable public access from your IP address
   - Create a database named 'emma'

### 2. Configure Local Environment

1. Run the cloud setup script:
   ```powershell
   .\dev-setup-cloud.ps1
   ```

2. The script will:
   - Check for the dotnet-ef tool and install if needed
   - Verify your Azure credentials in the .env file
   - Prompt for Azure PostgreSQL details if not configured
   - Restore NuGet packages
   - Apply EF Core migrations to your Azure PostgreSQL database

### 3. Development Workflow

The development workflow remains the same as before, but now without Docker or local service dependencies:

1. Make code changes
2. Run `dotnet build` to build the solution
3. Run `dotnet run` from the Emma.Api directory to start the API
4. Run frontend as needed (e.g., `npm start` from Emma.Web)

## Connection Strings and Configuration

Your `.env` file should contain connection details for all Azure services:

```bash
# Azure PostgreSQL
ConnectionStrings__PostgreSql=psql -h your-server.postgres.database.azure.com -U your_username -d emma_dev;Username=yourusername;Password=yourpassword;SslMode=Require

# Azure Cosmos DB
COSMOSDB__ACCOUNTENDPOINT=https://your-cosmos.documents.azure.com:443/
COSMOSDB__ACCOUNTKEY=yourkey
COSMOSDB__DATABASENAME=emma-agent
COSMOSDB__CONTAINERNAME=messages

# Azure OpenAI and other services
...
```

## Security Considerations

1. Never commit the `.env` file to source control
2. Consider using Azure Key Vault for more secure credential management
3. Restrict Azure PostgreSQL access to specific IP addresses

## Transitioning from Local PostgreSQL

If you were previously using local PostgreSQL:

1. Ensure you have a backup of any important data
2. Run the cloud setup script to update your connection strings
3. The script will handle updating your environment and applying migrations

## Troubleshooting

If you encounter issues with the cloud setup:

1. Verify your Azure credentials in the `.env` file
2. Check network connectivity to Azure services
3. Ensure your IP address is allowed in Azure PostgreSQL firewall rules
4. Run `dotnet ef database update --verbose` for detailed migration logs

---

This approach aligns with our decision to move away from Docker dependencies while providing a consistent, cloud-based development environment for the Emma AI Platform.
