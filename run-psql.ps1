# Load environment variables for the Emma AI Platform
. .\load-env.ps1

# Add PostgreSQL bin to path if not already present
$pgBinPath = "C:\Program Files\PostgreSQL\16\bin"
if (-not $env:Path.Contains($pgBinPath)) {
    $env:Path += ";$pgBinPath"
}

# Extract the connection string
$connectionString = $env:ConnectionStrings__PostgreSql

# Parse the connection string components using regex patterns
$hostMatch = [regex]::Match($connectionString, 'Host=([^;]+)')
$portMatch = [regex]::Match($connectionString, 'Port=([^;]+)')
$dbMatch = [regex]::Match($connectionString, 'Database=([^;]+)')
$userMatch = [regex]::Match($connectionString, 'Username=([^;]+)')
$pwdMatch = [regex]::Match($connectionString, 'Password=([^;]+)')

# Extract values if matches were found
$host = if ($hostMatch.Success) { $hostMatch.Groups[1].Value } else { $null }
$port = if ($portMatch.Success) { $portMatch.Groups[1].Value } else { $null }
$dbname = if ($dbMatch.Success) { $dbMatch.Groups[1].Value } else { $null }
$user = if ($userMatch.Success) { $userMatch.Groups[1].Value } else { $null }
$password = if ($pwdMatch.Success) { $pwdMatch.Groups[1].Value } else { $null }

# Verify extraction
Write-Host "Connection parameters:"
Write-Host "Host: $host"
Write-Host "Port: $port"
Write-Host "Database: $dbname"
Write-Host "User: $user"
Write-Host "Password: [HIDDEN]"

# Set password as environment variable (recommended approach for psql)
$env:PGPASSWORD = $password

# Check if create_test_table.sql exists
$sqlFile = ".\create_test_table.sql"
if (-not (Test-Path $sqlFile)) {
    Write-Host "Error: SQL file not found at $sqlFile"
    exit 1
}

Write-Host "`nSQL file contents:"
Get-Content $sqlFile | ForEach-Object { Write-Host "  $_" }

# Execute the SQL file with proper handling
Write-Host "`nExecuting SQL script..."
& psql -h $host -p $port -d $dbname -U $user --set="sslmode=require" -f $sqlFile

# Check the result
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSQL script executed successfully!"
} else {
    Write-Host "`nError executing SQL script. Exit code: $LASTEXITCODE"
}

# Clear the password from environment for security
$env:PGPASSWORD = $null

Write-Host "`nOperation complete."
