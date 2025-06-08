# Emma AI Platform - Final Migration Check
# Comprehensive script to verify migrations and troubleshoot issues

# Create logs directory
$logsDir = "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

# Create timestamp for log files
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$migrationScriptPath = Join-Path -Path $logsDir -ChildPath "migration-script-$timestamp.sql"
$logFilePath = Join-Path -Path $logsDir -ChildPath "migration-log-$timestamp.txt"

# Write to both console and log file
function Write-Log {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
    Add-Content -Path $logFilePath -Value $Message
}

# Load environment variables
Write-Log "Emma AI Platform - Final Migration Check" -ForegroundColor Cyan
Write-Log "=======================================" -ForegroundColor Cyan
Write-Log "Loading environment variables from .env..." -ForegroundColor Yellow

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
        Write-Log "Using connection string: $maskedConnString" -ForegroundColor Green
    } else {
        Write-Log "ERROR: PostgreSQL connection string not found in .env" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Log "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Navigate to Data project and set diagnostic flags
Set-Location -Path "Emma.Data"
Write-Log "Changed to Emma.Data directory" -ForegroundColor Yellow

# Set environment variables for maximum diagnostics
[Environment]::SetEnvironmentVariable("DOTNET_EF_VERBOSE", "1", [System.EnvironmentVariableTarget]::Process)
[Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", [System.EnvironmentVariableTarget]::Process)

# List available migrations
Write-Log "Checking available migrations..." -ForegroundColor Yellow
$migrationsOutput = dotnet ef migrations list --startup-project ../Emma.Api 2>&1
Add-Content -Path $logFilePath -Value $migrationsOutput
Write-Log $migrationsOutput -ForegroundColor Gray

# Generate migrations script to see what should be created
Write-Log "Generating SQL migration script..." -ForegroundColor Yellow
dotnet ef migrations script --startup-project ../Emma.Api --output $migrationScriptPath --idempotent

if (Test-Path $migrationScriptPath) {
    $scriptSize = (Get-Item $migrationScriptPath).Length
    Write-Log "Generated migration script ($scriptSize bytes): $migrationScriptPath" -ForegroundColor Green
    
    # Display first few lines of the script to confirm it has content
    $scriptPreview = Get-Content $migrationScriptPath -TotalCount 20
    Add-Content -Path $logFilePath -Value "Migration Script Preview:"
    Add-Content -Path $logFilePath -Value $scriptPreview
    
    if ($scriptSize -gt 0) {
        Write-Log "Script contains SQL commands to create tables" -ForegroundColor Green
    } else {
        Write-Log "WARNING: Generated script is empty" -ForegroundColor Red
    }
} else {
    Write-Log "ERROR: Failed to generate migration script" -ForegroundColor Red
}

# Apply migrations again with diagnostic info
Write-Log "Applying migrations with maximum verbosity..." -ForegroundColor Yellow
Write-Log "This may take a few minutes..." -ForegroundColor Yellow

try {
    $migrationOutput = dotnet ef database update --startup-project ../Emma.Api --verbose 2>&1
    Add-Content -Path $logFilePath -Value $migrationOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Log "SUCCESS: Migrations applied successfully!" -ForegroundColor Green
    } else {
        Write-Log "ERROR: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Write-Log "See log file for details: $logFilePath" -ForegroundColor Yellow
    }
} catch {
    Write-Log "EXCEPTION during migration: $_" -ForegroundColor Red
    Add-Content -Path $logFilePath -Value $_.Exception.ToString()
}

# Return to original directory
Set-Location -Path ".."

Write-Log "`nTroubleshooting Steps:" -ForegroundColor Cyan
Write-Log "1. Check log file for detailed error messages: $logFilePath" -ForegroundColor White
Write-Log "2. Review migration script to see what tables should be created: $migrationScriptPath" -ForegroundColor White
Write-Log "3. Refresh database connection in VS Code to see if tables now exist" -ForegroundColor White
Write-Log "4. If tables still don't exist, you may need to run the SQL script manually in Azure Portal" -ForegroundColor White
