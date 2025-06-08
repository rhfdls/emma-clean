# Emma AI Platform - Migration Verification Script
# Creates a log file with detailed output to diagnose issues

# Create logs directory
$logsDir = "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

# Create log file with timestamp
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = Join-Path -Path $logsDir -ChildPath "migration-log-$timestamp.txt"

# Write to both console and log file
function Write-Log {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
    Add-Content -Path $logFile -Value $Message
}

Write-Log "Emma AI Platform - Migration Verification Script" -ForegroundColor Cyan
Write-Log "=============================================" -ForegroundColor Cyan
Write-Log "Log file: $logFile" -ForegroundColor Gray
Write-Log "Timestamp: $(Get-Date)" -ForegroundColor Gray
Write-Log "" 

# Load environment variables
Write-Log "Loading environment variables..." -ForegroundColor Yellow
if (Test-Path ".env") {
    $envContent = Get-Content ".env"
    $envVarsFound = 0
    
    foreach ($line in $envContent) {
        if ($line -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            
            # Don't log sensitive values
            if ($key -like "*password*" -or $key -like "*key*" -or $key -like "*secret*") {
                Write-Log "Setting environment variable: $key = ********" -ForegroundColor Gray
            } else {
                Write-Log "Setting environment variable: $key = $value" -ForegroundColor Gray
            }
            
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
            $envVarsFound++
        }
    }
    
    Write-Log "Loaded $envVarsFound environment variables" -ForegroundColor Green
    
    # Check PostgreSQL connection string
    $pgConnString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    if (-not [string]::IsNullOrWhiteSpace($pgConnString)) {
        $maskedConnString = $pgConnString -replace "(Password=)[^;]*", '$1********'
        Write-Log "PostgreSQL connection string: $maskedConnString" -ForegroundColor Green
        
        # Parse connection string to extract details
        $pgServer = if ($pgConnString -match "Host=([^;]+)") { $matches[1] } else { "unknown" }
        $pgDatabase = if ($pgConnString -match "Database=([^;]+)") { $matches[1] } else { "unknown" }
        $pgUser = if ($pgConnString -match "Username=([^;]+)") { $matches[1] } else { "unknown" }
        
        Write-Log "Server: $pgServer, Database: $pgDatabase, User: $pgUser" -ForegroundColor Gray
    } else {
        Write-Log "ERROR: PostgreSQL connection string not found in environment variables" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Log "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Check if we're in the correct directory
$currentDir = Get-Location
$apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"
if (Test-Path $apiDir) {
    Set-Location -Path $apiDir
    Write-Log "Changed directory to Emma.Api" -ForegroundColor Yellow
} else {
    Write-Log "ERROR: Emma.Api directory not found at $apiDir" -ForegroundColor Red
    exit 1
}

# Clean and build project
Write-Log "Cleaning project..." -ForegroundColor Yellow
dotnet clean | Out-File -FilePath $logFile -Append
Write-Log "Restoring packages..." -ForegroundColor Yellow
dotnet restore | Out-File -FilePath $logFile -Append
Write-Log "Building project..." -ForegroundColor Yellow
dotnet build | Out-File -FilePath $logFile -Append

# Check for migrations
Write-Log "Checking for existing migrations..." -ForegroundColor Yellow
$migrations = dotnet ef migrations list
Add-Content -Path $logFile -Value $migrations
$migrationsCount = ($migrations | Measure-Object -Line).Lines - 1
Write-Log "Found $migrationsCount migrations" -ForegroundColor Green

# Apply migrations
Write-Log "Applying migrations to database..." -ForegroundColor Yellow
Write-Log "This may take a few minutes..." -ForegroundColor Yellow
Write-Log "Detailed output will be saved to log file" -ForegroundColor Gray

try {
    $migrationOutput = dotnet ef database update --verbose 2>&1
    Add-Content -Path $logFile -Value $migrationOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Log "SUCCESS: Migrations applied successfully" -ForegroundColor Green
        Write-Log "The Emma AI Platform database schema has been created" -ForegroundColor Green
    } else {
        Write-Log "ERROR: Migration failed with exit code $LASTEXITCODE" -ForegroundColor Red
        Write-Log "Check the log file for details: $logFile" -ForegroundColor Yellow
    }
} catch {
    Write-Log "EXCEPTION: $_" -ForegroundColor Red
    Add-Content -Path $logFile -Value $_.Exception.ToString()
    Write-Log "Check the log file for details: $logFile" -ForegroundColor Yellow
}

# Return to original directory
Set-Location -Path $currentDir

Write-Log "" 
Write-Log "Migration process completed. Check log file for details:" -ForegroundColor Cyan
Write-Log $logFile -ForegroundColor Cyan
Write-Log "Refresh your database explorer in VS Code to see if tables were created" -ForegroundColor Yellow
