# Simple SSL connection to Emma AI Platform PostgreSQL database
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Clean environment variables first
$env:PGPASSWORD = $null
$env:PGSSLMODE = $null
$env:PGSSLROOTCERT = $null

# Basic connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "emma"
$pgUser = "emmaadmin@emma-db-server"
$pgPassword = "EmmaDb2025Test" # This should be reset in Azure Portal

# Set only essential environment variables
$env:PGPASSWORD = $pgPassword

Write-Host "Simple Emma AI Platform PostgreSQL Connection Test"
Write-Host "================================================="
Write-Host "Server: $pgServer"
Write-Host "Database: $pgDatabase"
Write-Host "User: $pgUser"

# Try connection with minimal parameters and sslmode in the connection string
Write-Host "`nAttempting connection with minimal SSL settings..."

# Use a simpler connection approach with sslmode in connection string
& psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require" -c "SELECT 'Connected to Emma AI Platform!' AS status;"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful!"
    
    # Execute the test table creation script
    Write-Host "`nCreating test_entities table..."
    & psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require" -f "create_test_table.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTest table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`nError creating test table. Exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "`nConnection failed with exit code: $LASTEXITCODE"
    Write-Host "`nRemaining troubleshooting steps:"
    Write-Host "1. Reset the PostgreSQL server password in Azure Portal to 'EmmaDb2025Test'"
    Write-Host "2. Try connecting directly from Azure Cloud Shell as a test"
    Write-Host "3. Consider temporarily disabling SSL requirement on the server for testing"
    Write-Host "   (via require_secure_transport=OFF server parameter)"
}

# Clear password from environment
$env:PGPASSWORD = $null
