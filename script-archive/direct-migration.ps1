# Emma AI Platform - Direct Migration from Data Project
# This script ensures environment variables are set correctly before running migrations

# Start from root folder
$rootDir = Get-Location

# Load connection string from .env
Write-Host "Loading connection string from .env..." -ForegroundColor Yellow
if (Test-Path ".env") {
    $envContent = Get-Content ".env" | Where-Object { $_ -match "ConnectionStrings__PostgreSql=" }
    if ($envContent) {
        $connString = $envContent.Split('=', 2)[1]
        $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
        Write-Host "Connection string: $maskedConnString" -ForegroundColor Green
        
        # Set environment variable
        [Environment]::SetEnvironmentVariable("ConnectionStrings__PostgreSql", $connString, [System.EnvironmentVariableTarget]::Process)
    } else {
        Write-Host "ERROR: PostgreSQL connection string not found in .env" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Set verbose logging
[Environment]::SetEnvironmentVariable("DOTNET_EF_VERBOSE", "1", [System.EnvironmentVariableTarget]::Process)

# Check migration files
Write-Host "`nExamining migration files in Emma.Data..." -ForegroundColor Yellow
Set-Location -Path "Emma.Data"
$migrationFiles = Get-ChildItem -Path "Migrations" -File
Write-Host "Found $($migrationFiles.Count) migration files:" -ForegroundColor Cyan
foreach ($file in $migrationFiles) {
    Write-Host " - $($file.Name)" -ForegroundColor Gray
}

# Run migrations
Write-Host "`nRunning database migrations from Emma.Data..." -ForegroundColor Yellow
Write-Host "Using Emma.Api as startup project..." -ForegroundColor Yellow
Write-Host "Connection string is set in environment variables" -ForegroundColor Yellow
Write-Host "This may take a few moments..." -ForegroundColor Yellow

dotnet ef database update --startup-project ../Emma.Api

# Check result
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS: Database migrations completed!" -ForegroundColor Green
    Write-Host "The Emma AI Platform database schema has been created" -ForegroundColor Green
    Write-Host "Refresh your database connection to see the tables" -ForegroundColor Yellow
} else {
    Write-Host "`nERROR: Migration command failed with exit code $LASTEXITCODE" -ForegroundColor Red
}

# Return to root directory
Set-Location -Path $rootDir
