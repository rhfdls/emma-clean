# Load environment variables for the Emma AI Platform
. .\load-env.ps1

# Display the connection string (with password hidden)
$connectionString = $env:ConnectionStrings__PostgreSql
$maskPassword = $connectionString -replace "(Password=)[^;]*", "$1***"
Write-Host "Connection string: $maskPassword"

# Parse the connection string to extract components

# Extract each component using regex to ensure proper extraction
$host_pattern = "Host=([^;]+)"
$port_pattern = "Port=([^;]+)"
$database_pattern = "Database=([^;]+)"
$username_pattern = "Username=([^;]+)"
$password_pattern = "Password=([^;]+)"

# Use regex to extract each component
if ($connectionString -match $host_pattern) { $host_value = $matches[1] }
if ($connectionString -match $port_pattern) { $port_value = $matches[1] }
if ($connectionString -match $database_pattern) { $database_value = $matches[1] }
if ($connectionString -match $username_pattern) { $username_value = $matches[1] }
if ($connectionString -match $password_pattern) { $password_value = $matches[1] }

# Set the PGPASSWORD environment variable for this session
$env:PGPASSWORD = $password_value

# Add PostgreSQL bin to path if not already present
$pgBinPath = "C:\Program Files\PostgreSQL\16\bin"
if (-not $env:Path.Contains($pgBinPath)) {
    $env:Path += ";$pgBinPath"
}

Write-Host "Connecting to Emma AI Platform Azure PostgreSQL database..."
Write-Host "Host: $host_value"
Write-Host "Database: $database_value"
Write-Host "Username: $username_value"

# Use the SQL file to execute against the database
$sqlFile = ".\create_test_table.sql"

if (Test-Path $sqlFile) {
    Write-Host "`nExecuting SQL script: $sqlFile"
    
    # Run the SQL file using psql with proper parameters
    & psql -h "$host_value" -p "$port_value" -d "$database_value" -U "$username_value" -f "$sqlFile" -v "ON_ERROR_STOP=1" --set="sslmode=require"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nSQL script executed successfully!"
    } else {
        Write-Host "`nError executing SQL script. Exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "Error: SQL file not found at $sqlFile"
}

# Clear the password from environment for security
$env:PGPASSWORD = $null

Write-Host "`nDone."
