# Simple direct connection test for Azure PostgreSQL
# Add PostgreSQL bin to path
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Set connection parameters directly
$pghost = "emma-db-server.postgres.database.azure.com"
$port = "5432"
$database = "emma"
$user = "emmaadmin@emma-db-server"

# Set password directly in environment variable
# NOTE: Password will be removed from script after successful testing
$env:PGPASSWORD = 'GOGdb54321'

Write-Host "Testing connection to Azure PostgreSQL..."
Write-Host "Host: $pghost"
Write-Host "Database: $database"
Write-Host "User: $user"

# Try a simple connection with proper syntax
Write-Host "`nAttempting connection to the Emma AI Platform database..."

# Use PowerShell here-string for better SQL command formatting
$testQuery = @"
SELECT 'Connected successfully to Emma AI Platform database!' AS status;
"@

# Execute the connection test
& psql -h $pghost -p $port -d $database -U $user -c $testQuery --set=sslmode=require

# Display result
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful!"
    
    # Once connected, try to run our test script for the Emma AI Platform
    Write-Host "`nExecuting test_entities table creation script..."
    & psql -h $pghost -p $port -d $database -U $user -f "create_test_table.sql" --set=sslmode=require
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTest table creation script executed successfully!"
    } else {
        Write-Host "`nError executing test table script. Exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "`nConnection failed with exit code: $LASTEXITCODE"
}

# Clear password
$env:PGPASSWORD = $null
