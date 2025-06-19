# Test Emma AI Platform PostgreSQL connection with new password
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Set environment variables for psql
$env:PGHOST = "emma-db-server.postgres.database.azure.com"
$env:PGPORT = "5432"
$env:PGDATABASE = "emma"
$env:PGUSER = "emmaadmin@emma-db-server"
$env:PGPASSWORD = "EmmaDb2025Test"  # Use the new password after resetting in Azure Portal
$env:PGSSLMODE = "require"

Write-Host "Testing Emma AI Platform PostgreSQL connection with new password..."
Write-Host "Host: $env:PGHOST"
Write-Host "Database: $env:PGDATABASE"
Write-Host "User: $env:PGUSER"
Write-Host "SSL Mode: $env:PGSSLMODE"

# Test connection with single SQL statement
Write-Host "`nAttempting connection..."
psql -c 'SELECT current_database() AS database, current_user AS user;'

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful with new password!"
    
    # Execute the test table creation script
    Write-Host "`nCreating test_entities table..."
    psql -f 'create_test_table.sql'
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTest table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`nError creating test table. Exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "`nConnection failed with exit code: $LASTEXITCODE"
}

# Clear sensitive environment variables
$env:PGPASSWORD = $null
$env:PGUSER = $null
$env:PGHOST = $null
$env:PGDATABASE = $null
