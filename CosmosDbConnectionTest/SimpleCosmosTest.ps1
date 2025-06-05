# Emma AI Platform - Simple CosmosDB Connection Test
Write-Host "Emma AI Platform - CosmosDB Connection Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host

# CosmosDB credentials from .env file
$endpoint = "https://emma-cosmos.documents.azure.com:443/"
$key = "LgUJy8nQWgJ1Cl3SQB0CNLPhNcpS70tAMRsdeIXpZBlncpveoLaZX9RRtjpJl6ETXsRZrHFQ2J5kACDbrjmIoA=="
$databaseName = "emma-agent"
$containerName = "messages"

Write-Host "Testing connection to:" -ForegroundColor Yellow
Write-Host "  Endpoint: $endpoint"
Write-Host "  Database: $databaseName"
Write-Host "  Container: $containerName"
Write-Host

# Test basic connection using REST API
try {
    # Create auth date
    $date = [DateTime]::UtcNow.ToString("r")
    
    # Generate auth signature
    $hmacSha256 = New-Object System.Security.Cryptography.HMACSHA256
    $hmacSha256.Key = [Convert]::FromBase64String($key)
    
    # Build signature string (GET operation on dbs resource)
    $verb = "GET"
    $resourceType = "dbs"
    $resourceId = ""
    $stringToSign = "$verb`n$resourceType`n$resourceId`n$date`n`n"
    
    # Compute signature
    $signature = $hmacSha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($stringToSign.ToLowerInvariant()))
    $signature = [Convert]::ToBase64String($signature)
    
    # Create auth header
    $authHeader = [Uri]::EscapeDataString("type=master&ver=1.0&sig=$signature")
    
    # Set up headers
    $headers = @{
        "Authorization" = $authHeader
        "x-ms-date" = $date
        "x-ms-version" = "2018-12-31"
    }
    
    # Make request to list databases
    Write-Host "Attempting to connect to CosmosDB..." -ForegroundColor Yellow
    $uri = "${endpoint}dbs"
    
    $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers
    
    Write-Host "SUCCESS: Connected to CosmosDB successfully!" -ForegroundColor Green
    Write-Host "Available databases:" -ForegroundColor Green
    
    foreach ($db in $response.Databases) {
        Write-Host "- $($db.id)" -ForegroundColor Green
        
        if ($db.id -eq $databaseName) {
            Write-Host "  > Database '$databaseName' exists and is accessible ✓" -ForegroundColor Green
        }
    }
    
    Write-Host
    Write-Host "The Emma AI Platform can connect to CosmosDB for context models." -ForegroundColor Cyan
}
catch {
    Write-Host "ERROR: Failed to connect to CosmosDB" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Verify the connection string and key in .env are correct" -ForegroundColor Yellow
    Write-Host "2. Check if your IP is allowed in the CosmosDB firewall" -ForegroundColor Yellow
    Write-Host "3. Ensure the Azure account and CosmosDB instance are active" -ForegroundColor Yellow
}
