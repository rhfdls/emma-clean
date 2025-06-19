# Emma AI Platform - Cloud Development Setup

> **Latest Update**: 2025-06-19 - Updated for multi-tenant architecture and security best practices

## Overview

The Emma AI Platform development environment supports a direct connection to Azure cloud services, ensuring consistency across development, staging, and production environments. This approach:

1. **Multi-tenant Architecture**: Supports isolated tenant environments with shared infrastructure
2. **Security First**: Implements least-privilege access and secure connectivity
3. **Consistency**: Eliminates "works on my machine" issues
4. **Scalability**: Cloud-native services scale with your development needs
5. **Compliance**: Built with enterprise security and compliance in mind

## Required Azure Resources

The Emma AI Platform leverages these Azure services in a multi-tenant architecture:

### 1. **Azure PostgreSQL - Flexible Server**

- **Purpose**: Primary relational database for tenant metadata and structured data
- **Key Data**:
  - Tenant configurations and isolation
  - User identities and role-based access control (RBAC)
  - Subscription and billing information
  - Audit logs and compliance records
- **Multi-tenancy**: Row-level security and schema-per-tenant patterns

### 2. **Azure Cosmos DB**

- **Purpose**: Multi-model database for AI and unstructured data
- **Key Data**:
  - AI/ML model outputs and vector embeddings
  - Conversation history with full-text search
  - Document storage with vector search capabilities
  - Real-time analytics and telemetry
- **Multi-tenancy**: Partitioned by tenant ID with dedicated throughput

### 3. **Azure OpenAI Service**

- **Purpose**: Core AI/ML capabilities
- **Features**:
  - GPT-4 and beyond for natural language understanding
  - Custom fine-tuned models for specific domains
  - Content moderation and safety filters

### 4. **Azure Blob Storage**

- **Purpose**: Secure file and object storage
- **Usage**:
  - Document and media file storage
  - Model artifacts and snapshots
  - Export/import data dumps
  - Backup and restore points

### 5. **Azure Key Vault**

- **Purpose**: Centralized secrets management
- **Stores**:
  - Database connection strings
  - API keys and credentials
  - Certificates and encryption keys

### 6. **Azure Monitor & Application Insights**

- **Purpose**: Comprehensive monitoring and diagnostics
- **Features**:
  - Application performance monitoring
  - Log analytics and diagnostics
  - Alerting and notifications

### 7. **Azure Private Link & Networking**

- **Purpose**: Secure network connectivity
- **Features**:
  - Private endpoints for PaaS services
  - Network security groups (NSGs)
  - DDoS protection

## Setup Instructions

### 1. Prerequisites

Before you begin, ensure you have:

- **Azure Subscription** with Owner or Contributor access
- **Azure CLI** installed and authenticated
- **.NET 8.0 SDK** or later
- **Node.js** LTS version
- **Git** for version control
- **Azure Developer CLI (optional)** for simplified setup

### 2. Set Up Azure PostgreSQL

1. Log in to the [Azure Portal](https://portal.azure.com)
2. Create a new Azure Database for PostgreSQL - Flexible Server:
   - **Server Name**: emma-dev-{your-initials}
   - **Region**: Select a region close to you
   - **Version**: PostgreSQL 14 or later
   - **Compute + Storage**:
     - Dev/Test workload
     - Standard_B1ms (1 vCore, 2GB RAM) for development
   - **Authentication**: PostgreSQL authentication only
   - **Credentials**: Create a secure admin username and password
   - **Networking**:
     - Public access: Allow access from Azure services and resources within Azure
     - Add your current IP address
     - Enable "Allow public access from any Azure service within Azure to this server"
   - **Additional Settings**:
     - Enable high availability: No (for development)
     - Backup retention: 7 days
     - Geo-redundant backup: Disabled (for development)
3. After deployment:
   - Create a database named `emma_dev`
   - Note the connection details for later use

### 3. Configure Local Environment

1. Clone the repository and navigate to the project root:

   ```bash
   git clone https://github.com/your-org/emma.git
   cd emma
   ```

2. Create and configure the environment file:

   ```bash
   cp .env.example .env
   ```

3. Update the `.env` file with your Azure service credentials:
   - PostgreSQL connection string
   - Cosmos DB connection details
   - Azure AD B2C configuration
   - Application Insights instrumentation key

4. Run the cloud setup script:

   ```powershell
   .\dev-setup-cloud.ps1
   ```

   The script will:
   - Install required .NET tools and dependencies
   - Configure local development certificates
   - Set up the development database schema
   - Seed initial data
   - Validate Azure service connections
   - Generate API documentation
   - Start the development services

### 4. Development Workflow

#### Backend Development

1. Start the backend API:

   ```bash
   cd src/Emma.Api
   dotnet watch run
   ```

   This enables hot reload for C# code changes.

2. The API will be available at: `https://localhost:5001`

#### Frontend Development

1. Start the frontend development server:

   ```bash
   cd src/Emma.Web
   npm install
   npm start
   ```

2. The frontend will be available at: `http://localhost:3000`

#### Testing

Run unit tests:

```bash
dotnet test
```

Run integration tests (requires Azure services):

```bash
dotnet test --filter "Category=Integration"
```

#### Debugging

- Use VS Code or Visual Studio with the .NET Core debugger
- Attach to the running API process for debugging
- View logs in the console or in Application Insights

## Configuration Reference

### Environment Variables

Your `.env` file should contain the following configuration:

```ini
# Application
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://localhost:5001;http://localhost:5000

# Database (PostgreSQL)
ConnectionStrings__PostgreSql=Host=your-server.postgres.database.azure.com;Database=emma_dev;Username=your_username;Password=your_password;SslMode=Require;Trust Server Certificate=true

# Azure Cosmos DB
COSMOSDB__ACCOUNTENDPOINT=https://your-cosmos-account.documents.azure.com:443/
COSMOSDB__ACCOUNTKEY=your-account-key
COSMOSDB__DATABASENAME=emma
COSMOSDB__CONTAINERNAME=messages

# Azure AD B2C Authentication
AZURE_AD_B2C__INSTANCE=https://your-tenant.b2clogin.com
AZURE_AD_B2C__DOMAIN=your-tenant.onmicrosoft.com
AZURE_AD_B2C__CLIENT_ID=your-client-id
AZURE_AD_B2C__SIGNUPSIGNIN_POLICY_ID=B2C_1A_SIGNUP_SIGNIN
AZURE_AD_B2C__RESETPASSWORD_POLICY_ID=B2C_1A_PASSWORDRESET
AZURE_AD_B2C__EDITPROFILE_POLICY_ID=B2C_1A_PROFILEEDIT

# Azure OpenAI
AZURE_OPENAI_SERVICE_NAME=your-openai-service
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
AZURE_OPENAI_API_VERSION=2023-05-15

# Azure Application Insights
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=your-instrumentation-key

# Feature Flags
FEATURE_FLAG__MULTI_TENANCY=true
FEATURE_FLAG__ADVANCED_ANALYTICS=true
```

### Configuration Sources

Configuration is loaded in the following order:

1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment-specific overrides
3. Environment variables - For sensitive data and local overrides
4. Azure Key Vault - For production secrets (configured in Azure App Service)

## Security Best Practices

### Development Environment

1. **Secrets Management**
   - Never commit secrets to source control (add `.env` to `.gitignore`)
   - Use Azure Key Vault for production secrets
   - Rotate credentials regularly

2. **Network Security**
   - Use Azure Private Link for PaaS services in production
   - Configure NSGs to restrict traffic
   - Enable DDoS protection for production workloads

3. **Authentication & Authorization**
   - Use Azure AD B2C for identity management
   - Implement role-based access control (RBAC)
   - Enable multi-factor authentication for admin accounts

4. **Data Protection**
   - Enable encryption at rest and in transit
   - Use customer-managed keys for encryption
   - Implement data retention and backup policies

### Production Considerations

1. **Infrastructure as Code**
   - Use Terraform or Bicep for infrastructure provisioning
   - Store state files securely with access controls
   - Implement CI/CD pipelines with security scanning

2. **Monitoring & Logging**
   - Enable Azure Monitor and Application Insights
   - Set up alerts for suspicious activities
   - Implement centralized logging with log retention policies

3. **Compliance**
   - Follow Microsoft's Well-Architected Framework
   - Implement security controls for relevant compliance standards
   - Conduct regular security assessments and penetration testing

## Data Migration

### From Local PostgreSQL to Azure

1. **Preparation**
   - Audit existing data and clean up if necessary
   - Document the current schema and dependencies
   - Create a rollback plan

2. **Migration Steps**
   - Create a backup of the local database
   - Set up Azure Database for PostgreSQL
   - Use `pg_dump` and `pg_restore` for migration
   - Update connection strings in the application
   - Test thoroughly before switching traffic

3. **Validation**
   - Verify data integrity after migration
   - Run integration tests
   - Monitor performance and optimize as needed

## Troubleshooting

### Common Issues

1. **Connection Failures**
   - Verify network connectivity and firewall rules
   - Check service endpoints and private links
   - Validate credentials and permissions

2. **Performance Issues**
   - Review query performance
   - Check for connection pooling configuration
   - Monitor resource utilization

3. **Authentication Problems**
   - Verify Azure AD B2C configuration
   - Check token validation settings
   - Review application permissions

### Getting Help

1. **Documentation**
   - [Azure Documentation](https://docs.microsoft.com/azure/)
   - [.NET Core Documentation](https://docs.microsoft.com/dotnet/core/)
   - [React Documentation](https://reactjs.org/docs/getting-started.html)

2. **Support Channels**
   - Azure Support Portal
   - GitHub Issues
   - Internal Slack/Discord channels

3. **Escalation Path**
   - Team lead or architect
   - Microsoft Premier Support (if applicable)
   - Security team for security-related issues

1. Verify your Azure credentials in the `.env` file
2. Check network connectivity to Azure services
3. Ensure your IP address is allowed in Azure PostgreSQL firewall rules
4. Run `dotnet ef database update --verbose` for detailed migration logs

---

This approach aligns with our decision to move away from Docker dependencies while providing a consistent, cloud-based development environment for the Emma AI Platform.
