# Emma AI Platform: Finalized Architecture

This document outlines the finalized, EMMA-centric CRM architecture for the Emma AI Platform. All development and integrations should adhere to this structure.

## Architecture Diagram (Text Outline)

```
[GoG App]      [Twilio]      [Email] 
     \             |             /
      \            |            /
        ----> [Emma.Api] <-----
                    |         \
                    |          \
       [AI/Agentic Workflows]   [CRM Connectors/Adapters]
                    |                    |
             [Emma Data Store]     [FUB|HubSpot|Salesforce|...]
```

## Key Principles
- **Emma AI Platform** is the single source of truth for all contacts and interactions.
- **Contact tags** are reserved for general segmentation only (e.g., VIP, Region).
- **Emma.Api** is the sole gateway for all access, enforcing access control, privacy, and business logic.
- **First-party data** (GoG app, Twilio, email, etc.) flows directly into Emma.
- **Third-party CRMs** (FUB, HubSpot, Salesforce, etc.) are integrated through modular connectors/adapters.
- **AI and agentic workflows** (AI Foundry, etc.) interact only with Emma.Api.
- **All administration** (user management, OAuth/email config, subscription, etc.) is centralized in Emma.Api and surfaced in the `/settings` UI.
- **All privacy and business logic must reference tags on the Interaction entity, not Contact.**

## Refactor Guidance
- All integrations (Deepgram, Twilio, OpenAI, CRMs) should use the unified schema.
- All new features and refactors must pass through Emma.Api for business logic and security.
- Adapters for new CRMs or data sources should be modular and stateless.
- Document mapping logic for each CRM/adapter.
