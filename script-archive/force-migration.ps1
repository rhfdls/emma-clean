# Emma AI Platform - Force Database Migration Script
# Direct approach to create database tables

# Set strict error handling
$ErrorActionPreference = "Stop"

# Load connection string from .env
$envContent = Get-Content ".env" | Where-Object { $_ -match "^ConnectionStrings__PostgreSql=" }
if ($envContent) {
    $connString = $envContent.Split('=', 2)[1]
    $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
    Write-Host "Found connection string: $maskedConnString" -ForegroundColor Green
    
    # Extract database name
    if ($connString -match "Database=([^;]+)") {
        $dbName = $matches[1]
        Write-Host "Database name: $dbName" -ForegroundColor Cyan
    } else {
        Write-Host "Error: Could not extract database name from connection string" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Error: Connection string not found in .env file" -ForegroundColor Red
    exit 1
}

# Set environment variable
[Environment]::SetEnvironmentVariable("ConnectionStrings__PostgreSql", $connString, [System.EnvironmentVariableTarget]::Process)
Write-Host "Set ConnectionStrings__PostgreSql environment variable" -ForegroundColor Green

# Go to Emma.Api project
Set-Location "Emma.Api"
Write-Host "Changed to Emma.Api directory" -ForegroundColor Yellow

# Clean build the project
Write-Host "Cleaning and building project..." -ForegroundColor Yellow
dotnet clean
dotnet restore
dotnet build --no-restore

# Apply migrations with additional steps to ensure success
Write-Host "Running migrations with maximum verbosity..." -ForegroundColor Yellow

# Try to create migrations history table if needed
Write-Host "Ensuring migrations history table exists..." -ForegroundColor Yellow

try {
    # Enable maximum diagnostic info
    [Environment]::SetEnvironmentVariable("DOTNET_EF_VERBOSE", "1", [System.EnvironmentVariableTarget]::Process)
    
    # Run migration with full verbosity
    dotnet ef database update --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS! Database migrations completed successfully." -ForegroundColor Green
        Write-Host "Refresh your database explorer to see the new tables." -ForegroundColor Yellow
    } else {
        Write-Host "Migration command failed with exit code $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "Error during migration: $_" -ForegroundColor Red
}

# Return to original directory
Set-Location ".."

Write-Host "`nPost-migration actions:" -ForegroundColor Cyan
Write-Host "1. Refresh your database connection in VS Code" -ForegroundColor White
Write-Host "2. Check if tables appeared under public schema" -ForegroundColor White
Write-Host "3. If tables still don't appear, there may be permission issues" -ForegroundColor White
