# Emma AI Platform Data Contract: Interactions

This document defines the data contract for the `Interaction` entity as implemented in both PostgreSQL and CosmosDB, including which fields are shared and which are specific to each data store. This contract supports dual-database, cross-sync, and future-proofing for AI/RAG workflows.

---

## Entity Relationship Overview

- **Interaction**: Central entity representing communication or activity in the Emma platform.
- **Links**:
  - `agentId` links to a User/Agent.
  - `contactId` links to a Contact.
  - `organizationId` (optional) links to an Organization.

```
User/Agent (agentId) →
                     \
                      → Interaction ← (contactId) Contact
                      → (organizationId) Organization (optional)
```

---

## Field Table (with Required/Optional and Examples)

| Field            | Type           | Required | PostgreSQL | CosmosDB | Description / Notes                  | Example |
|------------------|----------------|:--------:|:----------:|:--------:|--------------------------------------|---------|
| Field            | Type           | Required | PostgreSQL | CosmosDB | Description / Notes                  | Example |
|------------------|----------------|:--------:|:----------:|:--------:|--------------------------------------|---------|
| id               | string / uuid  |   Yes    |     ✔      |    ✔     | Unique interaction/document ID       | "int001" |
| agentId          | string / uuid  |   Yes    |     ✔      |    ✔     | Agent GUID                          | "agent123" |
| contactId        | string / uuid  |   Yes    |     ✔      |    ✔     | Contact GUID                        | "contact456" |
| organizationId   | string / uuid  |   No     |     ✔      |    ✔     | Organization GUID (nullable)        | "org789" |
| type             | string         |   Yes    |     ✔      |    ✔     | Interaction type (call, email, etc.)| "call" |
| content          | string         |   Yes    |     ✔      |    ✔     | Full text content                   | "Left voicemail" |
| timestamp        | datetime       |   Yes    |     ✔      |    ✔     | UTC timestamp                       | "2024-05-28T10:20:30Z" |
| metadata         | json/dict      |   No     |     ✔      |    ✔     | Flexible metadata                   | {"source":"AI"} |
| 
### AI-Specific Fields
| aiConfidenceScore | float         |   No     |     ✔      |    ✔     | Confidence score (0-1) of AI analysis | 0.87 |
| modelVersion      | string        |   No     |     ✔      |    ✔     | Version of AI model used             | "gpt-4-0613" |
| promptTemplateId  | string        |   No     |     ✔      |    ✔     | ID of prompt template used           | "property-recommendation-v2" |
| aiMetadata        | json/dict     |   No     |     ✔      |    ✔     | AI-specific metadata                 | {"tokens_used": 123, "model": "gpt-4"} |
| 
### Vector Embeddings
| embedding         | float[]       |   No     |            |    ✔     | AI-generated embedding vector        | [0.123, 0.456] |
| embeddingModel    | string        |   No     |            |    ✔     | Embedding model name                | "text-embedding-ada-002" |
| embeddingDate     | datetime      |   No     |            |    ✔     | When the embedding was generated     | "2024-05-28T10:20:30Z" |
| 
### RAG & Context
| contextWindow     | string[]      |   No     |            |    ✔     | Document IDs used for RAG context   | ["doc123", "doc456"] |
| citations        | json/dict     |   No     |            |    ✔     | Source citations for AI responses    | {"sources": ["doc123#p2"]} |
| 
### Classification & Tags
| tags              | string[]      |   No     |     ✔      |    ✔     | Tags for AI/RAG/lead classification | ["AI","RAG"] |
| aiCategories     | string[]      |   No     |     ✔      |    ✔     | AI-generated categories             | ["sales", "follow_up"] |
| sentiment        | string        |   No     |     ✔      |    ✔     | AI-detected sentiment              | "positive" |

- **Required**: Must be present in every valid interaction object.
- ✔ = Field exists in that DB
- CosmosDB may have additional fields for AI/agent workflows not present in PostgreSQL.
- PostgreSQL may have additional fields for reporting/constraints not present in CosmosDB.

---

## Field Descriptions

- **id**: Unique identifier for the interaction (string/uuid). Required.
- **agentId**: Unique identifier of the agent/user responsible for the interaction. Required.
- **contactId**: Unique identifier of the contact involved in the interaction. Required.
- **organizationId**: Organization GUID (nullable). Optional.
- **type**: Type of interaction (e.g., call, email, sms, note, etc.). Required.
- **content**: Full text content of the interaction. Required.
- **timestamp**: UTC timestamp (ISO8601 string). Required.
- **metadata**: Flexible metadata as a JSON/dict. Optional.
- **embedding**: AI-generated vector representation of content (float array). CosmosDB only. Optional.
- **embeddingModel**: Name of the model used to generate embedding. CosmosDB only. Optional.
- **embeddingDate**: When the embedding was generated (datetime/ISO8601). CosmosDB only. Optional.
- **tags**: Tags for AI/RAG/lead classification (array of strings). CosmosDB only. Optional.

---

## Synchronization Strategy

- All fields marked as shared (✔ in both columns) must be kept in sync between PostgreSQL and CosmosDB.
- CosmosDB-specific fields (embedding, embeddingModel, embeddingDate, tags) are not synchronized to PostgreSQL unless future vector search is enabled (e.g., with pgvector).
- Use a service/background job to propagate changes for shared fields.

---

## Contact Entity Notes
- `tags` is for segmentation only (VIP, Buyer, Region, etc.). DO NOT use for privacy/business logic (CRM, PERSONAL, PRIVATE, etc.).
- `privacyLevel` is DEPRECATED. All privacy/business logic must be enforced via Interaction.Tags.
- Migration note: Legacy Contact.PrivacyLevel and any privacy/business logic in Contact.Tags are not used; use Interaction.Tags instead.

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

---

# Onboarding & Invitations API

This section defines the contracts for organization creation, invitations, registration from invitation, and email verification.

## Create Organization

- Method: POST
- Path: `/api/organization`
- Auth: Required (creator must be valid user)
- Body: `OrganizationCreateDto`

```json
{
  "name": "Acme Inc",
  "email": "owner@acme.com",
  "ownerUserId": "00000000-0000-0000-0000-000000000000",
  "planType": "pro",
  "seatCount": 10
}
```

Response: 201 Created → `OrganizationReadDto`

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "orgGuid": "22222222-2222-2222-2222-222222222222",
  "name": "Acme Inc",
  "email": "owner@acme.com",
  "planType": "pro",
  "seatCount": 10
}
```

Errors:
- 400 Bad Request (missing/invalid fields, unknown ownerUserId)

## Create Invitation (OrgOwnerOrAdmin)

- Method: POST
- Path: `/api/organization/{organizationId}/invitations`
- Auth: Required, policy `OrgOwnerOrAdmin`
- Body:

```json
{
  "email": "user@invitee.com",
  "role": "OrgAdmin"
}
```

Response: 201 Created

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "email": "user@invitee.com",
  "role": "OrgAdmin",
  "token": "opaque-token",
  "expiresAtUtc": "2025-08-20T00:00:00Z"
}
```

Errors:
- 401/403 (missing/insufficient permissions)
- 400 (invalid email)

## Get Invitation by Token

- Method: GET
- Path: `/api/organization/invitations/{token}`
- Auth: Anonymous
- Response: 200 OK

```json
{
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "email": "user@invitee.com",
  "role": "OrgAdmin",
  "isExpired": false
}
```

Errors:
- 404 (not found or expired)

## Register From Invitation

- Method: POST
- Path: `/api/organization/invitations/{token}/register`
- Auth: Anonymous
- Body:

```json
{
  "firstName": "Taylor",
  "lastName": "Lee",
  "password": "P@ssw0rd!"
}
```

Response: 201 Created

```json
{
  "userId": "44444444-4444-4444-4444-444444444444",
  "organizationId": "11111111-1111-1111-1111-111111111111",
  "accountStatus": "PendingVerification"
}
```

Side effects:
- Generates `verificationToken` and sends a verification email using `VERIFY_URL_BASE`

Errors:
- 400 (invalid token, weak password)
- 409 (email already registered)

## Verify Email

- Method: POST
- Path: `/api/auth/verify-email`
- Auth: Anonymous
- Body:

```json
{ "token": "verification-token" }
```

Response:
- 204 No Content (success)

Errors:
- 400 (invalid/expired token)
