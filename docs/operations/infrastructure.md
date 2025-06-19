# Emma AI Platform Infrastructure

> **Latest Update**: 2025-06-19 - Updated for multi-tenant architecture and security best practices

## Architecture Overview

The Emma AI Platform is built on a secure, multi-tenant cloud architecture designed for enterprise-scale AI operations. The platform leverages Azure's cloud services to provide a scalable, reliable, and secure environment for AI-powered interactions.

### Core Components

1. **Multi-tenant Web Application**
   - Built with .NET 8 and React
   - Tenant-aware middleware for request routing
   - Role-based access control (RBAC) with Azure AD integration
   - Real-time communication via SignalR

2. **AI Services Layer**
   - Azure OpenAI integration with GPT-4 and beyond
   - Custom RAG implementation with vector search
   - Multi-tenant model deployment and versioning
   - Prompt engineering and management

3. **Data Layer**

   - **Azure PostgreSQL**: Tenant metadata, user management, and structured data
   - **Azure Cosmos DB**: Unstructured/semi-structured data with vector search
   - **Azure Blob Storage**: File storage and document management
   - **Azure Cache for Redis**: Caching layer for performance optimization

4. **Integration Layer**

   - RESTful APIs with OpenAPI/Swagger documentation
   - Webhook support for external system integration
   - Event Grid for asynchronous event processing
   - Service Bus for reliable message queuing

## Multi-tenant Architecture

### Tenant Isolation

1. **Database Level**
   - Separate schema per tenant in PostgreSQL
   - Partitioned containers in Cosmos DB using tenant ID as partition key
   - Row-level security policies for data access control
   - Cross-tenant data isolation enforced at the application layer

2. **Application Level**
   - Tenant context middleware for request processing
   - Dependency injection scoped per-tenant
   - Distributed caching with tenant isolation
   - Tenant-specific configuration management

3. **Authentication & Authorization**
   - Azure AD B2C for identity management
   - JWT tokens with tenant claims
   - Custom policies for tenant onboarding and offboarding
   - Fine-grained permission management

## Data Layer Architecture

### Azure PostgreSQL

- **Purpose**: Primary relational database for structured data
- **Key Features**:
  - Row-level security for tenant isolation
  - High availability with read replicas
  - Point-in-time recovery
  - Automated backups
- **Data Stored**:
  - Tenant metadata and configuration
  - User accounts and authentication
  - Role-based access control (RBAC)
  - Subscription and billing data
  - Audit logs and compliance records
  - User override configurations

### Azure Cosmos DB

- **Purpose**: Unstructured/semi-structured data with AI capabilities
- **Key Features**:
  - Multi-region distribution
  - Automatic scaling
  - Native vector search
  - Change feed for real-time processing
- **Data Stored**:
  - AI/ML model outputs and embeddings
  - Conversation history and chat logs
  - Document storage with vector search
  - Real-time analytics and telemetry
  - Cached AI responses
  - User override history and audit trails

### Azure Blob Storage

- **Purpose**: File and object storage
- **Key Features**:
  - Hot, cool, and archive tiers
  - Immutable storage for compliance
  - Lifecycle management
  - Private endpoints
- **Data Stored**:
  - File attachments and media
  - Model artifacts
  - Export/import data dumps
  - Backup and restore points

## Security Architecture

### Identity and Access Management

- **Azure AD B2C** for customer identity and access management
- **Managed Identities** for service-to-service authentication
- **Conditional Access** policies for enhanced security
- **Privileged Identity Management** for just-in-time access

### Network Security

- **Azure Private Link** for private connectivity
- **Network Security Groups (NSGs)** for traffic filtering
- **Web Application Firewall (WAF)** for application protection
- **DDoS Protection** for network layer security

### Data Protection

- **Encryption at rest**: All data is encrypted using customer-managed keys
- **Encryption in transit**: TLS 1.2+ for all communications
- **Azure Key Vault** for secrets management
- **Azure Information Protection** for data classification

## Database Selection Guidelines

When developing new features:

1. **Use PostgreSQL for**:
   - Relational data with fixed schema
   - Data requiring ACID transactions
   - User account information
   - Subscription/billing data

2. **Use Cosmos DB for**:
   - AI-related content with vector search requirements
   - Large text or binary data (recordings, documents)
   - Data with variable schema (agent definitions, telemetry)
   - High-throughput read scenarios (validation lookups)
   - RAG content stores with semantic search
   - Agent blueprint storage and versioning
   - Real-time telemetry and monitoring data

## Local Development

For local development, we connect directly to Azure services:

- Azure PostgreSQL (no local PostgreSQL needed)
- Azure Cosmos DB with the following containers:
  - `agent-blueprints` - Agent definitions and templates
  - `rag-prompts` - RAG content for overrides/validations
  - `telemetry` - Agent performance and validation metrics
  - `conversations` - Chat history and context

All connection strings are managed through the `.env` file.

## Telemetry Integration

The platform integrates with:

- **Application Insights**: For application performance monitoring
- **Azure Monitor**: For infrastructure metrics and alerts
- **Custom Telemetry Pipeline**: For agent-specific metrics
  - Validation success/failure rates
  - Override patterns and frequencies
  - Agent response times and quality scores
  - RAG retrieval effectiveness

## RAG Implementation Details

### Storage Structure

```text
/rag-content
  /industry/
    /real-estate/
      validation-rules.json
      override-templates/
    /mortgage/
      validation-rules.json
      override-templates/
  /global/
    common-validations.json
    compliance-rules.json
```

### Validation Prompt Flow

1. User input received
2. System retrieves relevant validation rules from RAG
3. Rules are applied with contextual awareness
4. Override suggestions are generated when applicable
5. Audit trail is recorded in Cosmos DB

## Backup and Disaster Recovery

### Automated Backups

- **Azure PostgreSQL**:
  - Automated daily backups with 35-day retention
  - Point-in-time restore capability
  - Geo-redundant storage for critical data

- **Azure Cosmos DB**:
  - Continuous backup with point-in-time restore
  - 30-day retention period
  - Multi-region writes for high availability

### Disaster Recovery Plan

1. **Recovery Point Objective (RPO)**: 5 minutes
2. **Recovery Time Objective (RTO)**: 30 minutes
3. **Key Components**:
   - Geo-redundant storage
   - Automated failover groups
   - Regular disaster recovery testing

## Security and Compliance

### Data Security

- **Encryption at rest**: All data is encrypted using Azure Storage Service Encryption
- **Encryption in transit**: TLS 1.2+ for all communications
- **Network Security**:
  - Private endpoints for all PaaS services
  - Network Security Groups (NSGs) for traffic filtering
  - Azure DDoS Protection Standard

### Compliance and Governance Framework

- **Certifications**:
  - SOC 2 Type II compliant
  - GDPR and CCPA ready
- **Security Practices**:
  - Regular security audits
  - Penetration testing
  - Comprehensive audit logging
- **Governance**:
  - Access control policies
  - Change management
  - Incident response planning

## Monitoring and Alerts

### Key Metrics Monitored

- API response times and error rates
- Database performance and throughput
- AI model latency and token usage
- Storage capacity and performance
- Network latency and availability

### Alerting

- Real-time alerts for critical issues
- Escalation policies for different severity levels
- Integration with on-call rotation systems
- Business hours vs. after-hours alert routing

## Scaling and Performance

### Horizontal Scaling

- App Service auto-scaling based on CPU/memory metrics
- Cosmos DB automatic throughput management
- Azure Cache for Redis for session state and caching
- Content Delivery Network (CDN) for static assets

### Performance Optimization

- Database query optimization and indexing
- Caching strategies for frequently accessed data
- Asynchronous processing for long-running operations
- Content compression and minification

## Cost Management

### Cost Optimization

- Right-sizing of resources based on usage patterns
- Scheduled shutdown of non-production environments
- Reserved capacity for predictable workloads
- Budget alerts and spending limits

### Cost Allocation

- Resource tagging for cost tracking by department/project
- Monthly cost reports and analysis
- Showback/chargeback capabilities
- Cost anomaly detection

## Documentation and Training

### Internal Documentation

- Architecture decision records (ADRs)
- Runbooks for common operations
- Troubleshooting guides
- Knowledge base articles

### Training

- New hire onboarding materials
- Role-based training programs
- Regular knowledge sharing sessions
- Documentation of lessons learned

## Maintenance and Updates

### Patch Management

- Regular security patches and updates
- Maintenance windows for planned updates
- Rollback procedures for failed updates
- Change management process

### End-of-Life Planning

- Technology lifecycle management
- Deprecation notices and timelines
- Migration paths for deprecated components
- Sunset policies for retired features
