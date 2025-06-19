# Emma AI Platform - Database Tables Check Script
# This script uses PowerShell best practices to check for database tables

# Load environment variables from .env file
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($key, $value, [System.EnvironmentVariableTarget]::Process)
        }
    }
    Write-Host "Environment variables loaded" -ForegroundColor Green
} else {
    Write-Host "ERROR: .env file not found!" -ForegroundColor Red
    exit 1
}

# Extract connection string parameters
$connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
if (-not [string]::IsNullOrWhiteSpace($connString)) {
    $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
    Write-Host "Connection string: $maskedConnString" -ForegroundColor Cyan
    
    # Parse connection string
    $connString -match "Host=([^;]+)" | Out-Null
    $pgHost = $matches[1]
    $connString -match "Username=([^;]+)" | Out-Null
    $pgUser = $matches[1]
    $connString -match "Password=([^;]+)" | Out-Null
    $pgPass = $matches[1]
    $connString -match "Database=([^;]+)" | Out-Null
    $pgDb = $matches[1]
} else {
    Write-Host "ERROR: PostgreSQL connection string not found" -ForegroundColor Red
    exit 1
}

# Set PGPASSWORD environment variable for psql
$env:PGPASSWORD = $pgPass

# Check if psql is installed
$psqlExists = $null
try {
    $psqlExists = Get-Command psql -ErrorAction SilentlyContinue
} catch {
    # Command not found
}

if ($psqlExists) {
    # List tables using PowerShell best practices for SQL commands
    Write-Host "`nChecking tables in '$pgDb' database..." -ForegroundColor Yellow
    
    # Use heredoc format for the SQL query as per best practices
    $query = @'
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;
'@
    
    # Execute query with proper quoting
    psql -h $pgHost -U $pgUser -d $pgDb -c $query
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nMigration history:" -ForegroundColor Yellow
        
        # Check migration history table
        $migrationQuery = @'
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
'@
        psql -h $pgHost -U $pgUser -d $pgDb -c $migrationQuery
    } else {
        Write-Host "Failed to query database tables" -ForegroundColor Red
    }
} else {
    Write-Host "`npsql command not found. Cannot check database tables." -ForegroundColor Red
    Write-Host "Please install PostgreSQL client tools to run this check." -ForegroundColor Yellow
}

# Clear password from environment
$env:PGPASSWORD = ""
