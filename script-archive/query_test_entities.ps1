# Load environment variables
. .\load-env.ps1

# Extract connection string components
$connectionString = $env:ConnectionStrings__PostgreSql
$components = $connectionString -split ";"

$host_value = ($components | Where-Object { $_ -match "^Host=" }) -replace "^Host=", ""
$port_value = ($components | Where-Object { $_ -match "^Port=" }) -replace "^Port=", ""
$database_value = ($components | Where-Object { $_ -match "^Database=" }) -replace "^Database=", ""
$username_value = ($components | Where-Object { $_ -match "^Username=" }) -replace "^Username=", ""
$password_value = ($components | Where-Object { $_ -match "^Password=" }) -replace "^Password=", ""

# Create the PGPASSWORD environment variable for the current session
$env:PGPASSWORD = $password_value

# Display information (without showing the password)
Write-Host "Connecting to PostgreSQL database:"
Write-Host "Host: $host_value"
Write-Host "Database: $database_value"
Write-Host "Username: $username_value"
Write-Host "Port: $port_value"

# Define the SQL query using a PowerShell here-string for better readability
$query = @"
SELECT * FROM test_entities ORDER BY id;
"@

# Run the query using the psql command-line tool with SSL mode required
Write-Host "`nRunning query: $query"
try {
    # Note that we're using the Azure PostgreSQL flexible server connection format
    & psql "host=$host_value port=$port_value dbname=$database_value user=$username_value sslmode=require" -c $query
} catch {
    Write-Host "Error executing query: $_"
}

# Clear the password from environment for security
$env:PGPASSWORD = $null
