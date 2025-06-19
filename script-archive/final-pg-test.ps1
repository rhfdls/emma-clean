# Final test script for Emma AI Platform PostgreSQL connection
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Set connection parameters
$pghost = "emma-db-server.postgres.database.azure.com"
$pgport = "5432"
$pgdatabase = "emma"
$pguser = "emmaadmin@emma-db-server"
$pgpassword = "GOGdb54321"

# Set environment variables for psql
$env:PGHOST = $pghost
$env:PGPORT = $pgport
$env:PGDATABASE = $pgdatabase
$env:PGUSER = $pguser
$env:PGPASSWORD = $pgpassword
$env:PGSSLMODE = "require"

Write-Host "Testing connection to Emma AI Platform PostgreSQL database..."
Write-Host "Host: $pghost"
Write-Host "Database: $pgdatabase"
Write-Host "User: $pguser"
Write-Host "SSL Mode: require (enforced by environment variable)"

# Use PowerShell best practices for psql execution
# Use single quotes for SQL command to avoid interpretation
Write-Host "`nAttempting connection using environment variables..."
psql -c 'SELECT version();'

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful! Now attempting to execute test table creation script..."
    
    # Execute the test table creation script
    psql -f 'create_test_table.sql'
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTest table creation script executed successfully!"
    } else {
        Write-Host "`nError executing test table script. Exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "`nConnection failed with exit code: $LASTEXITCODE"
    Write-Host "`nTroubleshooting suggestions:"
    Write-Host "1. Verify the password is correct - consider resetting it in Azure Portal"
    Write-Host "2. Ensure your IP address is in the firewall rules"
    Write-Host "3. Check the 'require_secure_transport' setting on the server"
}

# Clear sensitive environment variables
$env:PGPASSWORD = $null
$env:PGUSER = $null
$env:PGHOST = $null
$env:PGDATABASE = $null
