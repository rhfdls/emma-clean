# Load environment variables for the Emma AI Platform
. .\load-env.ps1

# Add PostgreSQL bin to path if not already present
$pgBinPath = "C:\Program Files\PostgreSQL\16\bin"
if (-not $env:Path.Contains($pgBinPath)) {
    $env:Path += ";$pgBinPath"
}

# Get the connection string
$connectionString = $env:ConnectionStrings__PostgreSql

# Extract connection parameters
if ($connectionString -match "Host=([^;]+)") { $host = $matches[1] }
if ($connectionString -match "Port=([^;]+)") { $port = $matches[1] }
if ($connectionString -match "Database=([^;]+)") { $dbname = $matches[1] }
if ($connectionString -match "Username=([^;]+)") { $user = $matches[1] }
if ($connectionString -match "Password=([^;]+)") { $password = $matches[1] }

# Print connection details (masking password)
Write-Host "Connection details:"
Write-Host "Host: $host"
Write-Host "Port: $port"
Write-Host "Database: $dbname"
Write-Host "Username: $user"
Write-Host "Password: [HIDDEN]"

# Set environment variable for PSQL password
$env:PGPASSWORD = $password

# Test a simple connection with a basic query
Write-Host "`nTesting connection to Azure PostgreSQL database..."
& psql -h $host -p $port -d $dbname -U $user -c "SELECT 1 AS connection_test;" --set=sslmode=require -v ON_ERROR_STOP=1

# Report result
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful!"
} else {
    Write-Host "`nConnection failed with exit code: $LASTEXITCODE"
}

# Clear password from environment
$env:PGPASSWORD = $null
