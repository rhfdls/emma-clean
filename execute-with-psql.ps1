# Emma AI Platform - Execute SQL Script with psql
# This script executes the migration SQL script using psql

# Load environment variables from .env
Write-Host "Loading connection string from .env..." -ForegroundColor Yellow
if (Test-Path ".env") {
    $envContent = Get-Content ".env" | Where-Object { $_ -match "ConnectionStrings__PostgreSql=" }
    if ($envContent) {
        $connString = $envContent.Split('=', 2)[1]
        $maskedConnString = $connString -replace "(Password=)[^;]*", '$1********'
        Write-Host "Connection string: $maskedConnString" -ForegroundColor Green
    } else {
        Write-Host "ERROR: PostgreSQL connection string not found in .env" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Parse connection string
$pgHost = $pgPort = $pgDb = $pgUser = $pgPass = ""

$connString.Split(';') | ForEach-Object {
    $parts = $_.Split('=')
    if ($parts.Length -eq 2) {
        $key = $parts[0].Trim()
        $value = $parts[1].Trim()
        
        switch ($key) {
            "Host" { $pgHost = $value }
            "Port" { $pgPort = $value }
            "Database" { $pgDb = $value }
            "Username" { $pgUser = $value.Split('@')[0] }
            "Password" { $pgPass = $value }
        }
    }
}

# Check if script exists
if (-not (Test-Path "emma-db-script.sql")) {
    Write-Host "ERROR: SQL script not found (emma-db-script.sql)" -ForegroundColor Red
    exit 1
}

# Create a temporary PGPASSFILE to avoid password prompt
$pgPassFile = Join-Path $env:TEMP "pgpass.conf"
"$pgHost`:$pgPort`:$pgDb`:$pgUser`:$pgPass" | Out-File -FilePath $pgPassFile -Encoding ASCII
$env:PGPASSFILE = $pgPassFile

# Set environment variable for psql
$env:PGSSLMODE = "require"

Write-Host "Executing SQL script using psql..." -ForegroundColor Yellow
Write-Host "This may take a few moments..." -ForegroundColor Yellow

try {
    # Check if psql is available
    $psqlCheck = Get-Command psql -ErrorAction SilentlyContinue
    
    if ($psqlCheck) {
        # Execute the SQL script
        psql -h $pgHost -p $pgPort -d $pgDb -U $pgUser -f "emma-db-script.sql"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SUCCESS: SQL script executed successfully!" -ForegroundColor Green
            Write-Host "The Emma AI Platform database schema has been created." -ForegroundColor Green
            
            # Verify tables were created
            Write-Host "`nVerifying tables..." -ForegroundColor Yellow
            $verifyQuery = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'"
            psql -h $pgHost -p $pgPort -d $pgDb -U $pgUser -c $verifyQuery
        } else {
            Write-Host "ERROR: Failed to execute SQL script (exit code: $LASTEXITCODE)" -ForegroundColor Red
        }
    } else {
        Write-Host "ERROR: psql command not found. Please install PostgreSQL client tools." -ForegroundColor Red
        Write-Host "You can install psql using: choco install postgresql" -ForegroundColor Yellow
    }
} finally {
    # Clean up the temporary PGPASSFILE
    if (Test-Path $pgPassFile) {
        Remove-Item $pgPassFile -Force
    }
    Remove-Item Env:\PGPASSFILE -ErrorAction SilentlyContinue
    Remove-Item Env:\PGSSLMODE -ErrorAction SilentlyContinue
}
