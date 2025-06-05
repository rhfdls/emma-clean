# Emma AI Platform - Database Tables Creation Script
# This script manually creates EF Migrations History table and runs migrations

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
}

# Load environment variables from .env file
Write-ColorMessage "Loading environment variables from .env file..." -ForegroundColor Yellow
if (Test-Path ".env") {
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
        Write-ColorMessage "Connection string loaded: $maskedConnString" -ForegroundColor Green
        
        # Parse connection string parameters
        $matches = @{}
        if ($connString -match "Host=([^;]+)") { $pgHost = $matches[1] }
        if ($connString -match "Port=([^;]+)") { $pgPort = $matches[1] }
        if ($connString -match "Database=([^;]+)") { $pgDb = $matches[1] }
        if ($connString -match "Username=([^;]+)") { $pgUser = $matches[1] }
        if ($connString -match "Password=([^;]+)") { $pgPass = $matches[1] }
        
        Write-ColorMessage "Extracted connection parameters" -ForegroundColor Yellow
    } else {
        Write-ColorMessage "ERROR: PostgreSQL connection string not found in .env file" -ForegroundColor Red
        exit 1
    }
} else {
    Write-ColorMessage "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Make sure EF CLI tools are installed
Write-ColorMessage "Checking for EF Core tools..." -ForegroundColor Yellow
try {
    $efVersion = dotnet ef --version
    Write-ColorMessage "EF Core tools found: $efVersion" -ForegroundColor Green
} catch {
    Write-ColorMessage "Installing EF Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-ColorMessage "ERROR: Failed to install EF Core tools" -ForegroundColor Red
        exit 1
    }
}

# Navigate to API project
$currentDir = Get-Location
$apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"
if (Test-Path $apiDir) {
    Set-Location -Path $apiDir
    Write-ColorMessage "Changed directory to $apiDir" -ForegroundColor Yellow
} else {
    Write-ColorMessage "ERROR: Emma.Api directory not found" -ForegroundColor Red
    exit 1
}

# Clean, restore and build
Write-ColorMessage "Cleaning, restoring and building project..." -ForegroundColor Yellow
dotnet clean
dotnet restore
dotnet build

# Manually create migrations history table if it doesn't exist
Write-ColorMessage "Creating EF Migrations History table if needed..." -ForegroundColor Yellow

# Create SQL script file
$sqlFilePath = Join-Path -Path $currentDir -ChildPath "create-migrations-table.sql"
@"
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
"@ | Set-Content -Path $sqlFilePath

Write-ColorMessage "SQL script for migrations table created at: $sqlFilePath" -ForegroundColor Green

# Run migrations with detailed output
Write-ColorMessage "Running EF Core migrations..." -ForegroundColor Yellow
Write-ColorMessage "This will create all necessary tables for the Emma AI Platform" -ForegroundColor Yellow

$env:DOTNET_EF_VERBOSE = "1" # Set verbose flag for more output
dotnet ef database update --verbose

if ($LASTEXITCODE -ne 0) {
    Write-ColorMessage "ERROR: Migration failed" -ForegroundColor Red
    
    # Show detailed troubleshooting steps
    Write-ColorMessage "`nTroubleshooting Suggestions:" -ForegroundColor Yellow
    Write-ColorMessage "1. Check Azure PostgreSQL firewall settings to allow your IP address" -ForegroundColor White
    Write-ColorMessage "2. Verify correct connection string format in .env" -ForegroundColor White
    Write-ColorMessage "3. Try manually creating tables using SQL scripts from migrations" -ForegroundColor White
    Write-ColorMessage "4. Check if the user has permissions to create tables" -ForegroundColor White
} else {
    Write-ColorMessage "SUCCESS: Migrations applied successfully" -ForegroundColor Green
    Write-ColorMessage "The Emma AI Platform database schema has been created" -ForegroundColor Green
    Write-ColorMessage "Refresh your database connection in VS Code to see the tables" -ForegroundColor Yellow
}

# Return to original directory
Set-Location -Path $currentDir
