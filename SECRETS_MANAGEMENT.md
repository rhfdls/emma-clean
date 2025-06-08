# Emma AI Platform: Secrets Management

This document outlines the official practices for managing secrets and configuration in the Emma AI Platform. All team members should follow these guidelines to ensure security, consistency, and maintainability across environments.

> **TRANSITION PERIOD NOTICE:** The Emma AI Platform is currently using a hybrid configuration approach where values can come from both docker-compose.yml and .env/.env.local files. This document describes the target state we're gradually moving toward.

## Configuration Precedence

### Current Hybrid Approach (Transition Period)

During the transition period, the Emma AI Platform uses this precedence order:

1. Docker Compose `environment:` entries in docker-compose.yml (current source of most values)
2. `.env.local` file values (if present, not in version control)
3. `.env` file values (if present, can be in version control)
4. `appsettings.*.json` values (lowest precedence)

### Target Future State

After the transition period, we'll use this cleaner precedence:

1. Docker Compose `environment:` entries (minimal, non-sensitive overrides only)
2. `.env.local` file values (all secrets and environment-specific values)
3. `.env` file values (default values, placeholders only)
4. `appsettings.*.json` values (lowest precedence)

**Important:** Never define the same variable in multiple places for the same service to avoid confusion and shadowing issues.

## Adding New Secrets

1. Add the placeholder to `.env.example`:
   ```
   NEW_SECRET=placeholder
   ```

2. Create or update `.env.local` (not in version control) with the real value:
   ```
   NEW_SECRET=RealSecretValue
   ```

3. Add the secret to the `EnvironmentValidator.cs` file to ensure it's checked on startup:
   ```csharp
   // In EnvironmentValidator.cs
   var requiredVariables = new Dictionary<string, string>
   {
       // ...
       { "NEW_SECRET", "Description of the new secret" },
   };
   ```

4. For production, update your CI/CD pipeline or deployment process with the new secret

## Updating Existing Secrets

1. Update your local `.env.local` file
2. For production, update through your secure CI/CD pipeline or deployment process

## Secret Rotation Procedures

1. **Preparation Phase**:
   - Generate new secrets in the respective service (Azure, etc.)
   - Document both old and new values in a secure location (NOT in code)

2. **Deployment Phase**:
   - Update secrets in Azure Key Vault 
   - For Azure App Services: Update app settings via Azure portal or Azure CLI
   - For containerized environments: Update the environment variables in your container orchestration platform

3. **Verification Phase**:
   - Deploy to staging environment first
   - Confirm all health checks pass
   - Verify all affected functionality

4. **Production Rollout**:
   - Use deployment slots for zero-downtime rotation if available
   - Monitor application logs and metrics during rotation
   - Be prepared to rollback if issues arise

5. **Cleanup Phase**:
   - Securely delete old secrets after a grace period
   - Document the rotation in the security log

## Environment Setup for New Developers

1. Clone the repository
2. Copy `.env.example` to `.env.local`
3. Fill in the real values for your development environment
4. Run validation: `pwsh ./scripts/validate-env.ps1` or `bash ./scripts/validate-env.sh`
5. Start the application with `docker-compose up`

## Secrets in CI/CD

### Azure DevOps

```yaml
# azure-pipelines.yml
variables:
  - group: emma-platform-secrets # Variable group defined in Azure DevOps

steps:
  - script: |
      # Use variables from the variable group
      echo "##vso[task.setvariable variable=AZUREAIFOUNDRY__APIKEY;issecret=true]$(AZURE_AI_FOUNDRY_KEY)"
```

### GitHub Actions

```yaml
# .github/workflows/deploy.yml
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        env:
          AZUREAIFOUNDRY__APIKEY: ${{ secrets.AZURE_AI_FOUNDRY_KEY }}
```

### Key Vault References in App Service

For the most secure production setup, use Key Vault references in Azure App Service:

```
AZUREAIFOUNDRY__APIKEY=@Microsoft.KeyVault(SecretUri=https://emma-keyvault.vault.azure.net/secrets/azureaifoundry-apikey/)
```

This approach ensures secrets are never stored directly in environment variables.

## Security Guidelines

- NEVER commit `.env.local` or any file containing real secrets
- Use Azure Key Vault or equivalent for production secrets
- Rotate secrets regularly following the security policy
- For local development, use `.env.local` which is in `.gitignore`
- All secrets should have an expiration policy
- Use the principle of least privilege when assigning permissions
- Enable audit logging for all secrets access

## Environment Variable Naming Convention

For consistency across the Emma AI Platform, all environment variables should:

1. Use UPPERCASE with double underscores for section separators
   ```
   AZUREAIFOUNDRY__APIKEY=value
   ```

2. Use consistent prefixes for related variables:
   ```
   COSMOSDB__ACCOUNTENDPOINT=value
   COSMOSDB__ACCOUNTKEY=value
   ```

3. Never embed environment-specific information in variable names

## Troubleshooting

If your application fails to start with environment validation errors:

1. Check that `.env.local` exists and contains all required secrets
2. Verify that variables use the correct naming convention (case-sensitive)
3. Run the environment validation script:
   - Windows: `pwsh ./scripts/validate-env.ps1`
   - Linux/Mac: `bash ./scripts/validate-env.sh`
4. Check for variable shadowing with:
   - Windows: `pwsh ./scripts/check-env-conflicts.ps1`
   - Linux/Mac: `bash ./scripts/check-env-conflicts.sh`
