# Emma AI Platform - CosmosDB Connection Test Script
Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
Write-Host "   Emma AI Platform - CosmosDB Connection Validator" -ForegroundColor Cyan
Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
Write-Host "Testing connection to Azure Cosmos DB using account credentials."
Write-Host "This validates the CosmosDB setup for context models (NBA/AEA)."
Write-Host ""

# Read credentials from .env file
$envFile = Get-Content -Path "../.env" -ErrorAction SilentlyContinue
if (-not $envFile) {
    Write-Host "ERROR: Could not find .env file in parent directory." -ForegroundColor Red
    exit 1
}

# Extract CosmosDB credentials
$endpoint = ($envFile | Where-Object { $_ -match "COSMOSDB__ACCOUNTENDPOINT=(.*)" } | ForEach-Object { $matches[1] })
$key = ($envFile | Where-Object { $_ -match "COSMOSDB__ACCOUNTKEY=(.*)" } | ForEach-Object { $matches[1] })
$database = ($envFile | Where-Object { $_ -match "COSMOSDB__DATABASENAME=(.*)" } | ForEach-Object { $matches[1] })
$container = ($envFile | Where-Object { $_ -match "COSMOSDB__CONTAINERNAME=(.*)" } | ForEach-Object { $matches[1] })

# Display connection info
Write-Host "CosmosDB Connection Details:"
Write-Host "  Endpoint: $endpoint"
Write-Host "  Database: $database"
Write-Host "  Container: $container"
Write-Host ""

# Test connection using Invoke-RestMethod
try {
    # Build the date string for authorization header
    $date = [System.DateTime]::UtcNow.ToString("R")
    
    # The resource path we want to access (just getting database info)
    $resourceType = "dbs"
    $resourceLink = ""
    
    # Create the authorization token
    $keyBytes = [System.Convert]::FromBase64String($key)
    $hmacSha256 = New-Object System.Security.Cryptography.HMACSHA256 -ArgumentList $keyBytes
    $verb = "GET"
    $resourceId = $resourceLink
    $stringToSign = "$verb`n$resourceType`n$resourceId`n$date`n`n"
    $stringToSignBytes = [System.Text.Encoding]::UTF8.GetBytes($stringToSign.ToLowerInvariant())
    $signature = $hmacSha256.ComputeHash($stringToSignBytes)
    $signature = [System.Convert]::ToBase64String($signature)
    
    # URL encode the authorization token
    $authHeader = [System.Web.HttpUtility]::UrlEncode("type=master&ver=1.0&sig=$signature")
    
    # Build the headers
    $headers = @{
        "Authorization" = $authHeader
        "x-ms-date" = $date
        "x-ms-version" = "2018-12-31"
    }
    
    # Make the request to list databases
    $uri = "$($endpoint)dbs"
    Write-Host "Testing connection by listing databases..." -ForegroundColor Yellow
    
    $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers
    
    # Check if our database exists
    $databaseExists = $false
    foreach ($db in $response.Databases) {
        if ($db.id -eq $database) {
            $databaseExists = $true
            break
        }
    }
    
    if ($databaseExists) {
        Write-Host "✓ Successfully connected to CosmosDB!" -ForegroundColor Green
        Write-Host "✓ Database '$database' exists" -ForegroundColor Green
        Write-Host ""
        Write-Host "The Emma AI Platform is ready to use CosmosDB for context models." -ForegroundColor Green
    } else {
        Write-Host "✓ Successfully connected to CosmosDB!" -ForegroundColor Green
        Write-Host "! Database '$database' was not found - may need to be created" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Failed to connect to CosmosDB: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Verify the connection string and key in .env are correct" -ForegroundColor Yellow
    Write-Host "2. Check if your IP is allowed in the CosmosDB firewall" -ForegroundColor Yellow
    Write-Host "3. Confirm the Azure subscription is active" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "---------------------------------------------------------" -ForegroundColor Cyan
