# Emma AI Platform - Database Fix Script
# This script directly creates database tables using EF Core migrations

Write-Host "Emma AI Platform - Database Migration Fix" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Load .env file and set environment variables
if (Test-Path ".env") {
    Write-Host "Loading environment variables from .env file..." -ForegroundColor Yellow
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
        }
    }
    
    # Verify connection string is loaded
    $connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    if (-not [string]::IsNullOrWhiteSpace($connString)) {
        $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
        Write-Host "Connection string loaded: $maskedConnString" -ForegroundColor Green
    } else {
        Write-Host "ERROR: PostgreSQL connection string not found in .env file" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Navigate to API project
$currentDir = Get-Location
$apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"
if (Test-Path $apiDir) {
    Set-Location -Path $apiDir
    Write-Host "Changed directory to $apiDir" -ForegroundColor Yellow
} else {
    Write-Host "ERROR: Emma.Api directory not found" -ForegroundColor Red
    exit 1
}

# Clean build to ensure fresh start
Write-Host "Cleaning project..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Clean operation failed, continuing anyway" -ForegroundColor Yellow
}

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed" -ForegroundColor Red
    Set-Location -Path $currentDir
    exit 1
}

# Build project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Set-Location -Path $currentDir
    exit 1
}

# Verify EF migrations are available
Write-Host "Checking available migrations..." -ForegroundColor Yellow
$migrationsOutput = dotnet ef migrations list --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to list migrations: $migrationsOutput" -ForegroundColor Red
    Set-Location -Path $currentDir
    exit 1
}
Write-Host $migrationsOutput -ForegroundColor Gray

# Run migrations
Write-Host "Applying migrations to Azure PostgreSQL database..." -ForegroundColor Yellow
Write-Host "This may take a few minutes, please be patient..." -ForegroundColor Yellow
$migrationOutput = dotnet ef database update --no-build --verbose 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Migration failed" -ForegroundColor Red
    Write-Host $migrationOutput -ForegroundColor Gray
    
    # Provide detailed error information
    Write-Host "`nTroubleshooting information:" -ForegroundColor Yellow
    Write-Host "1. Check if your Azure PostgreSQL server firewall allows your IP address" -ForegroundColor White
    Write-Host "2. Verify the connection string format in .env file" -ForegroundColor White
    Write-Host "3. Ensure Azure PostgreSQL server is running" -ForegroundColor White
    Write-Host "4. Check if database 'emma' exists in Azure PostgreSQL" -ForegroundColor White
} else {
    Write-Host "SUCCESS: Database migrations applied successfully" -ForegroundColor Green
    Write-Host "The Emma AI Platform database schema has been created" -ForegroundColor Green
    
    # Output success information
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Refresh your database explorer in VS Code to see tables" -ForegroundColor White
    Write-Host "2. Run the Emma API: dotnet run" -ForegroundColor White
    Write-Host "3. Verify the Emma AI Platform connects to Azure PostgreSQL" -ForegroundColor White
}

# Return to original directory
Set-Location -Path $currentDir
