# Emma AI Platform - Database Migration Debug Script
# This script applies migrations with verbose logging

# Create logs directory if it doesn't exist
$logsDir = ".\logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path -Path $logsDir -ChildPath "migration-$timestamp.log"

function Log-Message {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    
    Write-Host $Message -ForegroundColor $ForegroundColor
    Add-Content -Path $logFile -Value $Message
}

Log-Message "Emma AI Platform - Migration Debug Log ($timestamp)" -ForegroundColor Cyan
Log-Message "=================================================" -ForegroundColor Cyan

# Load .env file
Log-Message "Loading environment variables from .env file..." -ForegroundColor Yellow
if (Test-Path ".env") {
    $envContent = Get-Content ".env"
    Log-Message "Found .env file with $($envContent.Count) lines" -ForegroundColor Gray
    
    foreach ($line in $envContent) {
        if ($line -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            
            # Skip logging passwords
            if ($key -like "*password*" -or $key -like "*key*") {
                $maskedValue = "********"
                Log-Message "Setting environment variable: $key = $maskedValue" -ForegroundColor Gray
            } else {
                Log-Message "Setting environment variable: $key = $value" -ForegroundColor Gray
            }
            
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
        }
    }
    
    # Check connection string specifically
    $connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    if (-not [string]::IsNullOrWhiteSpace($connString)) {
        $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
        Log-Message "Connection string is set: $maskedConnString" -ForegroundColor Green
    } else {
        Log-Message "ERROR: PostgreSQL connection string not found in .env file" -ForegroundColor Red
        exit 1
    }
} else {
    Log-Message "ERROR: .env file not found!" -ForegroundColor Red
    exit 1
}

# Navigate to API project
$currentDir = Get-Location
$apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"
if (Test-Path $apiDir) {
    Set-Location -Path $apiDir
    Log-Message "Changed directory to $apiDir" -ForegroundColor Yellow
} else {
    Log-Message "ERROR: Emma.Api directory not found" -ForegroundColor Red
    exit 1
}

# Get and print dotnet version
$dotnetVersion = dotnet --version
Log-Message "Using .NET SDK version: $dotnetVersion" -ForegroundColor Gray

# Restore packages
Log-Message "Restoring packages..." -ForegroundColor Yellow
$restoreOutput = dotnet restore 2>&1
Log-Message $restoreOutput -ForegroundColor Gray

# Build
Log-Message "Building project..." -ForegroundColor Yellow
$buildOutput = dotnet build 2>&1
Log-Message $buildOutput -ForegroundColor Gray

# List migrations
Log-Message "Available migrations:" -ForegroundColor Yellow
$migrationsListOutput = dotnet ef migrations list 2>&1
Log-Message $migrationsListOutput -ForegroundColor Gray

# Apply migrations with maximum verbosity
Log-Message "Applying migrations to Azure PostgreSQL..." -ForegroundColor Yellow
Log-Message "This may take several minutes..." -ForegroundColor Yellow

try {
    # Set environment variable to ensure maximum diagnostic info
    [Environment]::SetEnvironmentVariable("DOTNET_EF_VERBOSE", "1", [System.EnvironmentVariableTarget]::Process)
    
    # Run with diagnostic logging and capture all output
    $migrateOutput = dotnet ef database update --verbose 2>&1
    Log-Message $migrateOutput -ForegroundColor Gray
    
    Log-Message "Migration command completed" -ForegroundColor Green
} catch {
    Log-Message "ERROR: Exception occurred during migration: $_" -ForegroundColor Red
    Log-Message $_.ScriptStackTrace -ForegroundColor Red
}

# Return to original directory
Set-Location -Path $currentDir

Log-Message "`nMigration completed. Check log file for details: $logFile" -ForegroundColor Cyan
Log-Message "If migrations were successful, refresh your database explorer to see tables" -ForegroundColor Yellow
