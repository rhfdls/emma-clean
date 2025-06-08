# Test Emma AI Platform PostgreSQL connection with environment variables
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Set environment variables directly
$env:PGHOST = "emma-db-server.postgres.database.azure.com"
$env:PGPORT = "5432"
$env:PGDATABASE = "emma"
$env:PGUSER = "emmaadmin@emma-db-server"
$env:PGPASSWORD = "EmmaDb2025Test"

# Use the specific Baltimore certificate
$certPath = "./certs/BaltimoreCyberTrustRoot.crt.pem"
$env:PGSSLROOTCERT = $certPath
$env:PGSSLMODE = "verify-ca"

Write-Host "Emma AI Platform PostgreSQL Connection Test with Environment Variables"
Write-Host "==============================================================="
Write-Host "Host: $env:PGHOST"
Write-Host "Database: $env:PGDATABASE"
Write-Host "User: $env:PGUSER"
Write-Host "SSL Mode: $env:PGSSLMODE"
Write-Host "Certificate: $env:PGSSLROOTCERT"

Write-Host "`nAttempting connection..."
psql -c "SELECT 'Connected to Emma AI Platform!' AS status;"

# If the above fails, try with a different SSL mode
if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFirst attempt failed. Trying with sslmode=require..."
    $env:PGSSLMODE = "require"
    psql -c "SELECT 'Connected with sslmode=require!' AS status;"
    
    # If that also fails, try with SSL disabled (only for testing)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nSecond attempt failed. Trying with SSL disabled (for testing password only)..."
        $env:PGSSLMODE = "disable"
        psql -c "SELECT 'Connected with SSL disabled!' AS status;"
    }
}

# Clean up environment variables
$env:PGPASSWORD = $null
$env:PGHOST = $null
$env:PGDATABASE = $null
$env:PGUSER = $null
$env:PGPORT = $null
$env:PGSSLMODE = $null
$env:PGSSLROOTCERT = $null
