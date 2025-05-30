# Emma AI Platform Unified Schema

This document defines the unified, CRM-agnostic schema for Contacts, Interactions, and Users/Agents in the Emma AI Platform. This schema is the foundation for all integrations, business logic, and AI workflows.

---

## Entity Relationship Overview

- **Contact**: The central entity representing a person or organization.
- **Interaction**: Linked to a Contact by `contactId`. Represents communication or activity related to the contact.
- **User/Agent**: Represents a system user or agent. May own Contacts and perform Interactions.

```
User/Agent (owns) → Contact (referenced by) ← Interaction
```

- Each `Interaction` references a single `Contact` (via `contactId`).
- Each `Contact` is owned by a `User/Agent` (via `ownerId`).
- `User/Agent` may own multiple Contacts and perform Interactions.

---

## Contact

### Field Descriptions
| Field         | Type                               | Required | Description                                                                 | Example |
|---------------|------------------------------------|----------|-----------------------------------------------------------------------------|---------|
| id            | string                             | Yes      | Emma Platform unique ID                                                     | "abc123" |
| externalIds   | object (map)                       | No       | Map of CRM/system name to external ID                                       | {"fub":"fubid","hubspot":"h1"} |
| firstName     | string                             | Yes      | First name of the contact                                                   | "Jane" |
| lastName      | string                             | Yes      | Last name of the contact                                                    | "Doe" |
| emails        | array of objects                   | No       | List of email addresses                                                     | [{"address":"jane@ex.com","type":"work","verified":true}] |
| phones        | array of objects                   | No       | List of phone numbers                                                       | [{"number":"555-1234","type":"mobile","verified":true}] |
| address       | object                             | No       | Mailing address                                                             | {"street":"1 Main","city":"NYC"} |
| tags          | array of strings                   | No       | General segmentation (e.g., VIP, Buyer, Region)                             | ["VIP"] |
| leadSource    | string                             | No       | Source of the lead (do not include 'CRM', 'PERSONAL', or 'PRIVATE')         | "Website" |
| ownerId       | string                             | Yes      | Agent/organization owner                                                    | "agent123" |
| createdAt     | string (ISO8601)                   | Yes      | Creation timestamp                                                          | "2024-01-01T12:00:00Z" |
| updatedAt     | string (ISO8601)                   | No       | Last updated timestamp                                                      | "2024-01-02T12:00:00Z" |
| customFields  | object (map)                       | No       | Extensible for CRM-specific fields                                          | {"custom1":"val"} |
| privacyLevel  | enum: public/private/restricted    | Deprecated | Contact-level privacy (use Interaction-level tags for privacy/business logic) | "public" |

### Example Contact Object
```json
{
  "id": "abc123",
  "externalIds": {"fub": "fubid", "hubspot": "h1"},
  "firstName": "Jane",
  "lastName": "Doe",
  "emails": [{"address": "jane@ex.com", "type": "work", "verified": true}],
  "phones": [{"number": "555-1234", "type": "mobile", "verified": true}],
  "address": {"street": "1 Main", "city": "NYC", "state": "NY", "postalCode": "10001", "country": "USA"},
  "tags": ["VIP"],
  "leadSource": "Website",
  "ownerId": "agent123",
  "createdAt": "2024-01-01T12:00:00Z",
  "updatedAt": "2024-01-02T12:00:00Z",
  "customFields": {"custom1": "val"},
  "privacyLevel": "public"
}
```

### Original Schema
```json
{
  "id": "string",                  // Emma Platform unique ID
  "externalIds": {                  // Map of CRM/system name to external ID
    "fub": "string",
    "hubspot": "string",
    "salesforce": "string"
  },
  "firstName": "string",
  "lastName": "string",
  "emails": [
    {
      "address": "string",
      "type": "primary|work|personal|other",
      "verified": true
    }
  ],
  "phones": [
    {
      "number": "string",
      "type": "mobile|work|home|other",
      "verified": true
    }
  ],
  "address": {
    "street": "string",
    "city": "string",
    "state": "string",
    "postalCode": "string",
    "country": "string"
  },
  "tags": ["string"],              // General segmentation only (e.g., VIP, Buyer, Seller, Region)
  "leadSource": "string", // Do NOT include 'CRM', 'PERSONAL', or 'PRIVATE' here.
  // All privacy and business logic must reference tags on the Interaction entity.
  "ownerId": "string",             // Agent/organization owner
  "createdAt": "ISO8601 string",
  "updatedAt": "ISO8601 string",
  "customFields": {                // Extensible for CRM-specific fields
    "key": "value"
  },
  "privacyLevel": "public|private|restricted"
  // Contact-level privacy is deprecated; use Interaction-level tags for privacy/business logic.
}
```

## Interaction

### Field Descriptions
| Field            | Type                               | Required | Description                                                        | Example |
|------------------|------------------------------------|----------|--------------------------------------------------------------------|---------|
| id               | string                             | Yes      | Unique ID for the interaction                                      | "int001" |
| contactId        | string                             | Yes      | Emma Platform Contact ID (links to Contact)                        | "abc123" |
| externalIds      | object (map)                       | No       | Map of CRM/system name to external ID                              | {"fub":"fubint1"} |
| type             | enum                               | Yes      | Type of interaction                                                | "call" |
| direction        | enum                               | Yes      | Direction of interaction                                           | "inbound" |
| timestamp        | string (ISO8601)                   | Yes      | Time of interaction                                                | "2024-01-02T15:00:00Z" |
| agentId          | string                             | No       | Agent/user responsible for the interaction                         | "agent123" |
| content          | string                             | No       | Message body, note, etc.                                           | "Left voicemail" |
| channel          | enum                               | No       | Channel of interaction                                             | "twilio" |
| status           | enum                               | No       | Status of interaction                                              | "completed" |
| relatedEntities  | array of objects                   | No       | Related entities (deal, property, etc.)                            | [{"type":"deal","id":"d1"}] |
| tags             | array of strings                   | No       | Tags for privacy/business logic                                    | ["CRM"] |
| customFields     | object (map)                       | No       | Extensible for custom fields                                       | {"custom1":"val"} |

### Example Interaction Object
```json
{
  "id": "int001",
  "contactId": "abc123",
  "externalIds": {"fub": "fubint1"},
  "type": "call",
  "direction": "inbound",
  "timestamp": "2024-01-02T15:00:00Z",
  "agentId": "agent123",
  "content": "Left voicemail",
  "channel": "twilio",
  "status": "completed",
  "relatedEntities": [{"type": "deal", "id": "d1"}],
  "tags": ["CRM"],
  "customFields": {"custom1": "val"}
}
```

### Original Schema
```json
{
  "id": "string",
  "contactId": "string",             // Emma Platform Contact ID
  "externalIds": {
    "fub": "string",
    "hubspot": "string",
    "salesforce": "string"
  },
  "type": "call|email|sms|meeting|note|task|other",
  "direction": "inbound|outbound|system",
  "timestamp": "ISO8601 string",
  "agentId": "string",
  "content": "string",               // Message body, note, etc.
  "channel": "twilio|email|gog|crm|other",
  "status": "completed|pending|failed|scheduled",
  "relatedEntities": [
    {
      "type": "deal|property|listing|task|other",
      "id": "string"
    }
  ],
  "tags": ["string"],              // e.g., 'CRM', 'PERSONAL', 'PRIVATE', and any custom tags for privacy/business logic
  "customFields": {
    "key": "value"
  }
}
```

## User/Agent

### Field Descriptions
| Field           | Type                             | Required | Description                                 | Example |
|-----------------|----------------------------------|----------|---------------------------------------------|---------|
| id              | string                           | Yes      | Unique ID for the user/agent                | "agent123" |
| externalIds     | object (map)                     | No       | Map of external authentication IDs           | {"google":"g1"} |
| name            | string                           | Yes      | Name of the user/agent                      | "Alice Smith" |
| email           | string                           | Yes      | Email address                               | "alice@ex.com" |
| roles           | array of enums                   | Yes      | Roles assigned                              | ["admin"] |
| organizationId  | string                           | No       | Organization this user belongs to            | "org1" |
| status          | enum                             | Yes      | Account status                              | "active" |
| createdAt       | string (ISO8601)                 | Yes      | Creation timestamp                          | "2024-01-01T12:00:00Z" |

### Example User/Agent Object
```json
{
  "id": "agent123",
  "externalIds": {"google": "g1"},
  "name": "Alice Smith",
  "email": "alice@ex.com",
  "roles": ["admin"],
  "organizationId": "org1",
  "status": "active",
  "createdAt": "2024-01-01T12:00:00Z"
}
```

### Original Schema
```json
{
  "id": "string",
  "externalIds": {
    "google": "string",
    "microsoft": "string",
    "fub": "string"
  },
  "name": "string",
  "email": "string",
  "roles": ["admin", "agent", "viewer", "integration"],
  "organizationId": "string",
  "status": "active|inactive|pending",
  "createdAt": "ISO8601 string"
}
```
