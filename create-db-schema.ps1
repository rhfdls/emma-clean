# Emma AI Platform - Database Schema Creation Script
# This script focuses solely on applying migrations to the Azure PostgreSQL database

# Load environment variables from .env file
Write-Host "Loading environment variables..." -ForegroundColor Cyan
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
        }
    }
    Write-Host "Environment variables loaded successfully" -ForegroundColor Green
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

# Create the EF Migrations History table manually if needed
Write-Host "Ensuring migrations history table exists..." -ForegroundColor Yellow
$createMigrationsHistoryTable = @"
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
"@

# Extract database connection details
$connString -match "Host=([^;]+)" | Out-Null
$pgHost = $matches[1]
$connString -match "Username=([^;]+)" | Out-Null
$pgUser = $matches[1]
$connString -match "Password=([^;]+)" | Out-Null
$pgPass = $matches[1]
$connString -match "Database=([^;]+)" | Out-Null
$pgDbName = $matches[1]

# Set PGPASSWORD environment variable for psql
$env:PGPASSWORD = $pgPass

# Check if psql is available
$psqlExists = $null
try {
    $psqlExists = Get-Command psql -ErrorAction SilentlyContinue
} catch {
    # Command not found
}

if ($psqlExists) {
    Write-Host "Using psql to create migrations history table..." -ForegroundColor Yellow
    psql -h $pgHost -U $pgUser -d $pgDbName -c $createMigrationsHistoryTable
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Migrations history table created or already exists" -ForegroundColor Green
    } else {
        Write-Host "Warning: Could not create migrations history table. Continuing anyway..." -ForegroundColor Yellow
    }
} else {
    Write-Host "psql not found. Skipping manual table creation." -ForegroundColor Yellow
    Write-Host "Will rely on EF Core to create the migrations history table." -ForegroundColor Yellow
}

# Clear PGPASSWORD for security
$env:PGPASSWORD = ""

# Apply migrations
Write-Host "`nApplying Entity Framework migrations..." -ForegroundColor Yellow
$currentDir = Get-Location
$apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"

if (Test-Path $apiDir) {
    Set-Location -Path $apiDir
    
    # Clean and rebuild to ensure everything is in order
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    dotnet clean
    
    Write-Host "Restoring packages..." -ForegroundColor Yellow
    dotnet restore
    
    Write-Host "Building project..." -ForegroundColor Yellow
    dotnet build
    
    # Apply migrations with verbose output
    Write-Host "Running migrations..." -ForegroundColor Yellow
    dotnet ef database update --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n[SUCCESS] Database schema created successfully!" -ForegroundColor Green
    } else {
        Write-Host "`n[ERROR] Failed to apply migrations" -ForegroundColor Red
        Write-Host "Try running the command manually:" -ForegroundColor Yellow
        Write-Host "cd Emma.Api" -ForegroundColor White
        Write-Host "dotnet ef database update --verbose" -ForegroundColor White
    }
    
    # Return to original directory
    Set-Location -Path $currentDir
} else {
    Write-Host "[ERROR] Could not find Emma.Api directory" -ForegroundColor Red
}

Write-Host "`nScript completed. Check the output above for any errors." -ForegroundColor Cyan
