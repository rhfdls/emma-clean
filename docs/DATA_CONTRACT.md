# Emma AI Platform Data Contract: Interactions

This document defines the data contract for the `Interaction` entity as implemented in both PostgreSQL and CosmosDB, including which fields are shared and which are specific to each data store. This contract supports dual-database, cross-sync, and future-proofing for AI/RAG workflows.

---

## Field Table

| Field            | Type           | PostgreSQL | CosmosDB | Description / Notes                  |
|------------------|----------------|:----------:|:--------:|--------------------------------------|
| id               | string / uuid  |     ✔      |    ✔     | Unique interaction/document ID       |
| agentId          | string / uuid  |     ✔      |    ✔     | Agent GUID                          |
| contactId        | string / uuid  |     ✔      |    ✔     | Contact GUID                        |
| organizationId   | string / uuid  |     ✔      |    ✔     | Organization GUID (nullable)        |
| type             | string         |     ✔      |    ✔     | Interaction type (call, email, etc.)|
| content          | string         |     ✔      |    ✔     | Full text content                   |
| timestamp        | datetime       |     ✔      |    ✔     | UTC timestamp                       |
| metadata         | json/dict      |     ✔      |    ✔     | Flexible metadata                   |
| embedding        | float[]        |            |    ✔     | AI-generated embedding vector        |
| embeddingModel   | string         |            |    ✔     | Embedding model name                |
| embeddingDate    | datetime       |            |    ✔     | When the embedding was generated     |
| tags             | string[]       |            |    ✔     | Tags for AI/RAG/lead classification |

- ✔ = Field exists in that DB
- CosmosDB may have additional fields for AI/agent workflows not present in PostgreSQL.
- PostgreSQL may have additional fields for reporting/constraints not present in CosmosDB.

---

## Synchronization Strategy

- All fields marked as shared (✔ in both columns) must be kept in sync between PostgreSQL and CosmosDB.
- CosmosDB-specific fields (embedding, embeddingModel, embeddingDate, tags) are not synchronized to PostgreSQL unless future vector search is enabled (e.g., with pgvector).
- Use a service/background job to propagate changes for shared fields.

---

## Example CosmosDB Interaction Document

```json
{
  "id": "string",
  "agentId": "string",
  "contactId": "string",
  "organizationId": "string",
  "type": "call|email|sms|note|etc",
  "timestamp": "ISO8601 string",
  "content": "string",
  "metadata": { "source": "AI", "confidence": "high" },
  "embedding": [0.123, 0.456, ...],
  "embeddingModel": "openai-ada-002",
  "embeddingDate": "2024-05-28T10:20:30Z",
  "tags": ["AI", "RAG", "lead"]
}
```

---

## Notes
- For future vector search in PostgreSQL, consider enabling the pgvector extension and mirroring the `embedding` field.
- Always document any schema changes here and in code comments for maintainability.
