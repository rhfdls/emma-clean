# Pull Request

## Description
<!-- Briefly explain what this PR does, why it's needed, and any context. -->

### Schema changes (if applicable)
<!-- If this PR includes EF Core migrations under src/Emma.Infrastructure/Migrations/, reference the ADR here -->
- ADR reference: ADR-____ (e.g., ADR-0007)
- Related ADR file (optional): docs/adr/____-________.md

## Related Issue(s)
- Closes #[issue-number]

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Refactor
- [ ] Documentation update

## Checklist
- [ ] Locked restore/build/tests pass (Windows + Linux runners)
- [ ] AI calls go through orchestrator/service; no direct SDKs in business code
- [ ] Validation → override → execution intact; if rejected, at least one safer alternative is proposed
- [ ] Tenant + subscription checks present; Postgres scoped by OrganizationId/TenantId (or RLS); Cosmos includes /tenantId
- [ ] Structured outputs validate against JSON Schemas (Category=Schema tests updated)
- [ ] Telemetry updated: traceId, tenantId, orgId, model info, token counts, cost, decision, overrideMode, aiConfidenceScore, durationMs
- [ ] Secrets/PII check: no secrets or raw PII in code, tests, prompts, or dumps
- [ ] ADR added/updated for any significant architectural decision
- [ ] If this PR includes EF Core migrations: PR description references an ADR (e.g., ADR-0007) AND `docs/architecture/AppDbContextSchema.md` + `docs/architecture/EMMA-DATA-DICTIONARY.md` are updated
- [ ] `CHANGELOG.md` updated and docs touched if behavior changed
- [ ] I have reviewed my changes in dev/staging and attached screenshots if UI

## Screenshots (if applicable)

## Additional Notes
<!-- Add anything else reviewers should know. -->

