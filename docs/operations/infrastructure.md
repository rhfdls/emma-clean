# Emma AI Platform Infrastructure

## Database Architecture

The Emma AI Platform uses a dual-database approach for optimal performance and scalability:

### Azure PostgreSQL
- Primary relational database for structured data
- Stores core application entities:
  - Organizations
  - Agents
  - User accounts
  - Subscriptions
  - Structured interaction data

### Azure Cosmos DB
- Used for all AI-based data:
  - Full-text conversation history
  - Voice recordings
  - AI agent interaction data
  - RAG (Retrieval Augmented Generation) content
  - Synchronization data
- Provides flexible schema and high-performance querying for AI workloads

## Database Selection Guidelines

When developing new features:

1. **Use PostgreSQL for**:
   - Relational data with fixed schema
   - Data requiring ACID transactions
   - User account information
   - Subscription/billing data

2. **Use Cosmos DB for**:
   - AI-related content
   - Large text or binary data
   - Data with variable schema
   - High-throughput read scenarios
   - RAG content stores

## Local Development

For local development, we connect directly to Azure services:
- Azure PostgreSQL (no local PostgreSQL needed)
- Azure Cosmos DB

All connection strings are managed through the `.env` file.
