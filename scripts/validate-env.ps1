# Emma AI Platform Environment Variable Validation Script
# This script validates that all required environment variables are present

# Import environment variables from .env and .env.local if they exist
if (Test-Path ".env") {
    Get-Content .env | ForEach-Object {
        if (!$_.StartsWith("#") -and $_.Contains("=")) {
            $name, $value = $_.Split('=', 2)
            if (![string]::IsNullOrWhiteSpace($name) -and ![string]::IsNullOrWhiteSpace($value)) {
                [Environment]::SetEnvironmentVariable($name, $value)
            }
        }
    }
    Write-Host "✓ Loaded variables from .env" -ForegroundColor Green
} else {
    Write-Host "⚠ .env file not found" -ForegroundColor Yellow
}

if (Test-Path ".env.local") {
    Get-Content .env.local | ForEach-Object {
        if (!$_.StartsWith("#") -and $_.Contains("=")) {
            $name, $value = $_.Split('=', 2)
            if (![string]::IsNullOrWhiteSpace($name) -and ![string]::IsNullOrWhiteSpace($value)) {
                [Environment]::SetEnvironmentVariable($name, $value)
            }
        }
    }
    Write-Host "✓ Loaded variables from .env.local" -ForegroundColor Green
} else {
    Write-Host "⚠ .env.local file not found. Create it from .env.example to store your secrets." -ForegroundColor Yellow
}

# Define required environment variables
$requiredVariables = @{
    # Database connection
    "CONNECTION_STRINGS__POSTGRESQL" = "PostgreSQL Connection String";
    
    # CosmosDB (required for AI workflows)
    "COSMOSDB__ACCOUNTENDPOINT" = "CosmosDB Account Endpoint";
    "COSMOSDB__ACCOUNTKEY" = "CosmosDB Account Key";
    "COSMOSDB__DATABASENAME" = "CosmosDB Database Name";
    "COSMOSDB__CONTAINERNAME" = "CosmosDB Container Name";
    
    # Azure AI Foundry
    "AZUREAIFOUNDRY__ENDPOINT" = "Azure AI Foundry Endpoint";
    "AZUREAIFOUNDRY__APIKEY" = "Azure AI Foundry API Key";
    "AZUREAIFOUNDRY__DEPLOYMENTNAME" = "Azure AI Foundry Deployment Name";
}

$missingVariables = @()

foreach ($variable in $requiredVariables.GetEnumerator()) {
    $value = [Environment]::GetEnvironmentVariable($variable.Key)
    
    if ([string]::IsNullOrWhiteSpace($value)) {
        $missingVariables += "$($variable.Key) ($($variable.Value))"
    } else {
        # Mask sensitive values in output
        $maskedValue = $value
        if ($variable.Key -like "*KEY*" -or $variable.Key -like "*PASSWORD*" -or $variable.Key -like "*SECRET*") {
            $maskedValue = $value.Substring(0, [Math]::Min(4, $value.Length)) + "..." + $value.Substring([Math]::Max(0, $value.Length - 4))
        }
        
        Write-Host "✓ $($variable.Key) = $maskedValue" -ForegroundColor Green
    }
}

if ($missingVariables.Count -gt 0) {
    Write-Host "`n❌ Missing required environment variables:" -ForegroundColor Red
    foreach ($missing in $missingVariables) {
        Write-Host "  - $missing" -ForegroundColor Red
    }
    Write-Host "`nPlease set these variables in your .env.local file." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "`n✅ All required environment variables are present!" -ForegroundColor Green
}

# Check for shadowing issues
$variablesToCheck = @(
    @("COSMOSDB__ACCOUNTENDPOINT", "CosmosDb__AccountEndpoint"),
    @("COSMOSDB__ACCOUNTKEY", "CosmosDb__AccountKey"),
    @("AZUREAIFOUNDRY__ENDPOINT", "AzureAIFoundry__Endpoint"),
    @("AZUREAIFOUNDRY__APIKEY", "AzureAIFoundry__ApiKey")
)

$shadowingIssues = @()

foreach ($pair in $variablesToCheck) {
    $upperVar = $pair[0]
    $pascalVar = $pair[1]
    
    $upperValue = [Environment]::GetEnvironmentVariable($upperVar)
    $pascalValue = [Environment]::GetEnvironmentVariable($pascalVar)
    
    if ($upperValue -ne $null -and $pascalValue -ne $null -and $upperValue -ne $pascalValue) {
        $shadowingIssues += "Conflict between $upperVar and $pascalVar"
    }
}

if ($shadowingIssues.Count -gt 0) {
    Write-Host "`n⚠ Environment variable conflicts detected:" -ForegroundColor Yellow
    foreach ($conflict in $shadowingIssues) {
        Write-Host "  - $conflict" -ForegroundColor Yellow
    }
    Write-Host "The Emma AI Platform will use uppercase variable names if conflicting versions exist." -ForegroundColor Yellow
}

Write-Host "`nEnvironment validation completed!" -ForegroundColor Cyan
