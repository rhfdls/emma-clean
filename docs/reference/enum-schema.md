# Enum Schema Reference

This document describes the required and optional fields for enum configuration entries stored in Cosmos DB (`enumConfig` container) for EMMA.

## General Structure
- Each enum entry is a JSON document with a `type` property (partition key).
- Enum types include: `Plan`, `IndustryProfile`, `AgentCapability`, `ContactStage`.
- Each document may represent a single enum value or an array of values (for batch operations).

---

## Plan Enum

```json
{
  "type": "Plan",
  "key": "GOG_PRO_MONTHLY",
  "label": "Pro",
  "description": "Full AI orchestration, notes, and next best actions",
  "price": 750,
  "features": ["ask_emma", "notes", "nba"]
}
```

- **Required fields:**
  - `type`: Must be `Plan`
  - `key`: Unique string identifier
  - `label`: Display name
  - `price`: Numeric (per seat/month)
  - `features`: Array of feature keys
- **Optional fields:**
  - `description`: String
  - `metadata`: Object (for future extensibility)

---

## IndustryProfile Enum

```json
{
  "type": "IndustryProfile",
  "key": "REAL_ESTATE",
  "label": "Real Estate",
  "description": "For real estate professionals and brokerages"
}
```
- **Required fields:**
  - `type`: Must be `IndustryProfile`
  - `key`: Unique string identifier
  - `label`: Display name
- **Optional fields:**
  - `description`: String

---

## AgentCapability Enum

```json
{
  "type": "AgentCapability",
  "key": "ask_emma",
  "label": "Ask EMMA",
  "description": "Conversational Q&A agent"
}
```
- **Required fields:**
  - `type`: Must be `AgentCapability`
  - `key`: Unique string identifier
  - `label`: Display name
- **Optional fields:**
  - `description`: String

---

## ContactStage Enum

```json
{
  "type": "ContactStage",
  "key": "NEW_LEAD",
  "label": "New Lead",
  "description": "Initial inquiry or contact"
}
```
- **Required fields:**
  - `type`: Must be `ContactStage`
  - `key`: Unique string identifier
  - `label`: Display name
- **Optional fields:**
  - `description`: String

---

## Notes
- All enum documents must include a `type` property for partitioning.
- Keys must be unique within each enum type.
- Additional metadata can be added for future extensibility.
- Enum validation logic should enforce required fields on read.
