# Emma AI Platform Unified Schema

This document defines the unified, CRM-agnostic schema for Contacts, Interactions, and Users/Agents in the Emma AI Platform. This schema is the foundation for all integrations, business logic, and AI workflows.

## Contact
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
