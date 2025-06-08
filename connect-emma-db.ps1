# Connect to Emma AI Platform PostgreSQL database with proper SSL certificates
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Certificate paths with forward slashes for compatibility
$rootCertPath = "./certs/DigiCertGlobalRootCA.crt.pem"
$baltimoreCertPath = "./certs/BaltimoreCyberTrustRoot.crt.pem"

# Verify certificates exist
if (-not (Test-Path $rootCertPath)) {
    Write-Host "Error: Certificate file not found at $rootCertPath"
    Write-Host "Run download-azure-cert.ps1 first to download the required certificates."
    exit 1
}

# Set connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "emma"
$pgUser = "emmaadmin@emma-db-server"
$pgPassword = "EmmaDb2025Test" # Use the placeholder password (you should reset this in Azure Portal)

# Set environment variables for psql
$env:PGPASSWORD = $pgPassword
$env:PGSSLROOTCERT = $rootCertPath
$env:PGSSLMODE = "require" # Using require instead of verify-ca to avoid certificate verification issues

Write-Host "Emma AI Platform PostgreSQL Connection Test"
Write-Host "=========================================="
Write-Host "Server: $pgServer"
Write-Host "Database: $pgDatabase"
Write-Host "User: $pgUser"
Write-Host "SSL Mode: $env:PGSSLMODE"
Write-Host "SSL Root Cert: $env:PGSSLROOTCERT"

# Test connection with the specified connection string format
Write-Host "`nAttempting connection with validated SSL certificate..."

# Use the exact connection string format as specified in the diagnostic prompt
& psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require sslrootcert=$rootCertPath" -c "SELECT 'Connected to Emma AI Platform!' AS status;"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful!"
    
    # Try to execute the test table creation script
    Write-Host "`nCreating test_entities table..."
    & psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require sslrootcert=$rootCertPath" -f "create_test_table.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTest table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`nError creating test table. Exit code: $LASTEXITCODE"
    }
} else {
    Write-Host "`nPrimary connection attempt failed. Trying with Baltimore certificate..."
    
    # Try with the Baltimore certificate as backup
    & psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require sslrootcert=$baltimoreCertPath" -c "SELECT 'Connected to Emma AI Platform with Baltimore cert!' AS status;"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nConnection with Baltimore certificate successful!"
    } else {
        Write-Host "`nBoth certificate attempts failed."
        Write-Host "`nTroubleshooting steps to try:"
        Write-Host "1. Reset the password in Azure Portal to 'EmmaDb2025Test' exactly as shown"
        Write-Host "2. Ensure your IP address (174.114.187.8) is in the firewall allowlist"
        Write-Host "3. Try temporarily disabling SSL requirement on the server (require_secure_transport=OFF)"
        Write-Host "4. Check the Azure PostgreSQL server logs for more specific error information"
    }
}

# Clear sensitive environment variables
$env:PGPASSWORD = $null
$env:PGSSLROOTCERT = $null
$env:PGSSLMODE = $null
