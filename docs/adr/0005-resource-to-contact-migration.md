# ADR 0005: Resource → Contact-Centric Model Migration Completion

Status: Accepted
Date: 2025-09-09

## Context

Legacy documentation and some design artifacts referenced a separate Resource model and related concepts (e.g., ResourceAssignment, ResourceRecommendation, AssignedResources). As part of the unified, contact-centric architecture, all people and service providers are modeled as `Contact` with `RelationshipState.ServiceProvider` (or other roles), and assignments are represented via `ContactAssignment`.

This ADR confirms that the migration is complete in documentation and establishes deprecation guidance for any remaining references.

## Decision

- Confirm the contact-centric model as the authoritative pattern for representing service providers and assignments.
- Deprecate all references to:
  - Resource, ResourceAssignment, ResourceRecommendation
  - AssignedResources / ResourceAssignments navigation properties
  - ResourceAgent naming in the agent inventory
- Adopt equivalent contact-centric terms:
  - `Contact` with `RelationshipState.ServiceProvider`
  - `ContactAssignment` for assignment semantics
  - Rename guidance for `ResourceAgent` → `ServiceProviderAgent`

## Changes Implemented

- docs/api/EMMA-API.md
  - Base URL aligned to `http://localhost:5000` and collaborator note corrected (Guid).
- docs/api/openapi.yaml
  - Server URL set to `http://localhost:5000`.
- docs/architecture/RESOURCE_ASSIGNMENT_SCHEMA.md
  - Kept as OBSOLETE; fixed cross-links to migration/lexicon references.
- docs/architecture/SQL-CONTEXT-INTEGRATION.md
  - Updated agent context to reflect `ContactAssignment` naming for assigned service providers.
- docs/README.md
  - Architecture section flags `RESOURCE_ASSIGNMENT_SCHEMA.md` as OBSOLETE and points to `UNIFIED_SCHEMA.md`.
- docs/development/designs/resource-management-ui-design.md
  - Contact-centric banner added; `ResourceAssignment` interface replaced with `ContactAssignment`; component renamed to `ContactAssignments.tsx`.
- docs/reference/ENUMS-REFERENCE.md
  - `ResourceRelationshipType` and `ResourceAssignmentStatus` marked Deprecated with contact-centric guidance.
- docs/operations/README-cosmos-env.md
  - Added Required Seeding section; referenced `update-enum-references.ps1`.
- docs/reference/EMMA-DATA-DICTIONARY.md
  - Added OBSOLETE notes for `AssignedResources` and `ResourceAssignments` under Contact navigation properties.
- docs/reference/AppDbContextSchema.md
  - Added OBSOLETE notes for `AssignedResources` and `ResourceAssignments` under Contact navigation properties.
- docs/agents/AGENTIC-FRAMEWORK-INVENTORY.md
  - Deprecated `ResourceAgent` and added rename guidance to `ServiceProviderAgent`.

## Consequences

- Developers must use the contact-centric entities and APIs exclusively:
  - Model providers as `Contact` with `RelationshipState.ServiceProvider`.
  - Use `ContactAssignment` for any assignment semantics.
- Any remaining code/designs referencing Resource-based entities should be migrated or removed.
- SQL context, AI validation, and override flows remain unchanged; updates only align naming.

## References

- docs/architecture/UNIFIED_SCHEMA.md
- docs/architecture/ARCHITECTURE.md
- docs/architecture/SQL-CONTEXT-INTEGRATION.md
- docs/development/TERMINOLOGY-MIGRATION-GUIDE.md
- docs/reference/EMMA-DATA-DICTIONARY.md
- docs/reference/AppDbContextSchema.md
- docs/agents/AGENTIC-FRAMEWORK-INVENTORY.md

## Follow-Ups

- Optional: run a markdownlint cleanup pass across docs to fix spacing/code fence warnings.
- Optional: verify that any code comments or samples in tests do not reference Resource-based types.
