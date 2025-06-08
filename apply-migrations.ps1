# Emma AI Platform - Apply Migrations Script
# This script applies EF Core migrations to the Azure PostgreSQL database

# Load environment variables from .env
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

# Navigate to API project
Set-Location -Path "Emma.Api"
Write-Host "Changed to Emma.Api directory" -ForegroundColor Yellow

# Clean and build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build

# Apply migrations
Write-Host "Applying database migrations..." -ForegroundColor Cyan
Write-Host "This may take a few moments..." -ForegroundColor Cyan
dotnet ef database update

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
