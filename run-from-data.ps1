# Emma AI Platform - Run Migrations from Data Project
# This script runs migrations from the Emma.Data project where they are defined

# Load environment variables
Write-Host "Loading environment variables from .env..." -ForegroundColor Yellow
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
        }
    }
    
    # Verify connection string
    $connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    if (-not [string]::IsNullOrWhiteSpace($connString)) {
        $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
        Write-Host "Using connection string: $maskedConnString" -ForegroundColor Green
    } else {
        Write-Host "ERROR: PostgreSQL connection string not found in .env" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Navigate to Data project
Set-Location -Path "Emma.Data"
Write-Host "Changed to Emma.Data directory" -ForegroundColor Yellow

# List available migrations
Write-Host "Available migrations in Emma.Data:" -ForegroundColor Cyan
dotnet ef migrations list --startup-project ../Emma.Api

# Apply migrations using Emma.Api as the startup project
Write-Host "Applying migrations using Emma.Api as startup project..." -ForegroundColor Yellow
dotnet ef database update --startup-project ../Emma.Api

# Check result
if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Migrations successfully applied!" -ForegroundColor Green
    Write-Host "The Emma AI Platform database schema has been created." -ForegroundColor Green
    Write-Host "Refresh your database connection to see the tables." -ForegroundColor Yellow
} else {
    Write-Host "ERROR: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
}

# Return to original directory
Set-Location -Path ".."
