# Emma AI Platform - Run EF Core Migrations Script
# This script loads environment variables from .env and runs migrations

# Load environment variables from .env file
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
        }
    }
    Write-Host "Environment variables loaded from .env file" -ForegroundColor Green
} else {
    Write-Host "ERROR: .env file not found!" -ForegroundColor Red
    exit 1
}

# Display connection string (masked password)
$connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
if (-not [string]::IsNullOrWhiteSpace($connString)) {
    $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
    Write-Host "Using connection string: $maskedConnString" -ForegroundColor Cyan
} else {
    Write-Host "ERROR: PostgreSQL connection string not found in environment variables" -ForegroundColor Red
    exit 1
}

# Navigate to the API project directory
$apiDir = Join-Path -Path (Get-Location) -ChildPath "Emma.Api"
if (Test-Path $apiDir) {
    Set-Location -Path $apiDir
    
    # Run EF Core migrations
    Write-Host "Running Entity Framework migrations..." -ForegroundColor Yellow
    dotnet ef database update
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS: Database migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "ERROR: Failed to apply database migrations" -ForegroundColor Red
    }
    
    # Return to original directory
    Set-Location -Path (Join-Path -Path (Get-Location) -ChildPath "..")
} else {
    Write-Host "ERROR: Could not find Emma.Api directory" -ForegroundColor Red
}
