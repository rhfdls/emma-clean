# Emma AI Platform — TODO List

## [Open Action Items & Next Steps]

### Configuration & Secrets Management
- [ ] Remove all secrets (API keys, connection strings) from `docker-compose.yml`
- [ ] Create proper `.env` and `.env.local` structure; ensure `.env.local` is in `.gitignore`
- [ ] Standardize all environment variable naming conventions (e.g., UPPERCASE_DOUBLE_UNDERSCORE)
- [ ] Implement startup EnvironmentValidator in `Program.cs` for required env vars
- [ ] Add/Update `SECRETS_MANAGEMENT.md` to document secret handling, onboarding, and rotation
- [ ] Integrate Azure Key Vault for managing production secrets
- [ ] Add environment variable validation to CI/CD pipeline
- [ ] Document order of precedence for env variables (`docker-compose` > `.env` > `appsettings.json`)

### Code Quality & Workflow
- [ ] Enforce creation/updating of automated tests for all significant code changes
- [ ] Provide summary/changelog entry after each major edit (`CHANGELOG.md`)
- [ ] Propose branch name and/or draft PR for collaborative changes, including prefilled description and validation steps
- [ ] Always state current LLM model and warn if switching could lose context
- [ ] Automatically scan for security/compliance risks when editing authentication, secrets, or integrations
- [ ] Reference and update `TODO.md` and `TECH_DEBT.md` after major code changes

### Technical Debt & Decisions (see also `TECH_DEBT.md`)
- [ ] Resolve all duplicate or conflicting env variable definitions between `.env`, `docker-compose`, and codebase
- [ ] Eliminate inconsistent env var naming (e.g., `COSMOSDB__` vs. `CosmosDb__`)
- [ ] Implement script/check for potential environment variable shadowing/conflicts
- [ ] Track all major design/architecture decisions and known tech debt in `TECH_DEBT.md`

---

## [Completed]
- [x] Added PR template and `CHANGELOG.md` to repo
- [x] Added initial audit of configuration and secrets management

---

## **TECH_DEBT.md** (Example — add to your repo)

```markdown
# Emma AI Platform — Technical Debt & Design Decisions

## [2024-06-01]
- Multiple env variable naming styles detected (`COSMOSDB__`, `CosmosDb__`). Will standardize to UPPERCASE_DOUBLE_UNDERSCORE.
- Some secrets and API keys have previously been committed to config files—full secret rotation and audit needed.
- Current CI/CD process does not validate environment variables; will add startup validator and pipeline checks.
- Known risk of variable shadowing between `.env`, system, and Docker Compose—add script to detect/prevent.
- No documentation existed for secret onboarding or rotation; now tracked in `SECRETS_MANAGEMENT.md`.
```
