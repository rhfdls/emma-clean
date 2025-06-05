# Load environment variables
. .\load-env.ps1

# Set PostgreSQL bin to path if needed
$pgBinPath = "C:\Program Files\PostgreSQL\16\bin"
if (-not $env:Path.Contains($pgBinPath)) {
    $env:Path += ";$pgBinPath"
}

# Set the password as environment variable (will be used by psql)
$env:PGPASSWORD = "GOG54321%$"

Write-Host "Connecting to Emma AI Platform database in Azure PostgreSQL..."

# Direct psql command with clear parameters
$host = "emma-db-server.postgres.database.azure.com"
$port = "5432"
$dbname = "emma"
$user = "emmaadmin@emma-db-server"

# Test connection first
Write-Host "Testing connection..."
& psql -h $host -p $port -d $dbname -U $user -c "SELECT 'Connection successful!' AS status;"

# Now execute the SQL file
Write-Host "`nExecuting SQL script..."
& psql -h $host -p $port -d $dbname -U $user -f "create_test_table.sql" --set="sslmode=require" -v "ON_ERROR_STOP=1"

# Clear password from environment
$env:PGPASSWORD = $null

Write-Host "`nOperation complete."
