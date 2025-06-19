# Final SSL connection script for Emma AI Platform PostgreSQL
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Clear any existing PostgreSQL environment variables
$env:PGPASSWORD = $null
$env:PGHOST = $null
$env:PGUSER = $null
$env:PGSSLMODE = $null
$env:PGSSLROOTCERT = $null

# Set password directly
$env:PGPASSWORD = "EmmaDb2025Test"

# Paths to certificates (with forward slashes)
$digicertPath = "./certs/DigiCertGlobalRootCA.crt.pem"
$baltimorePath = "./certs/BaltimoreCyberTrustRoot.crt.pem"

Write-Host "Emma AI Platform PostgreSQL SSL Connection Test"
Write-Host "=============================================="
Write-Host "IP Address: 174.114.187.8 (should be in Azure firewall rules)"
Write-Host "Trying multiple SSL connection approaches..."

# Approach 1: Use DigiCert with sslmode=require in connection string
Write-Host "`n[Approach 1] Using DigiCert with sslmode=require in connection string..."
& psql "host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require sslrootcert=$digicertPath" -c "SELECT version();"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Approach 1 succeeded! Using this configuration to create test table..."
    & psql "host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require sslrootcert=$digicertPath" -f "create_test_table.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Test table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`n❌ Error creating test table. Exit code: $LASTEXITCODE"
    }
    
    # Clean up and exit if successful
    $env:PGPASSWORD = $null
    exit 0
}

# Approach 2: Use Baltimore with sslmode=require in connection string
Write-Host "`n[Approach 2] Using Baltimore with sslmode=require in connection string..."
& psql "host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require sslrootcert=$baltimorePath" -c "SELECT version();"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Approach 2 succeeded! Using this configuration to create test table..."
    & psql "host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require sslrootcert=$baltimorePath" -f "create_test_table.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Test table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`n❌ Error creating test table. Exit code: $LASTEXITCODE"
    }
    
    # Clean up and exit if successful
    $env:PGPASSWORD = $null
    exit 0
}

# Approach 3: Use environment variables with verify-ca
Write-Host "`n[Approach 3] Using environment variables with verify-ca..."
$env:PGHOST = "emma-db-server.postgres.database.azure.com"
$env:PGPORT = "5432"
$env:PGDATABASE = "emma"
$env:PGUSER = "emmaadmin@emma-db-server"
$env:PGSSLMODE = "verify-ca"
$env:PGSSLROOTCERT = $baltimorePath

psql -c "SELECT version();"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Approach 3 succeeded! Using this configuration to create test table..."
    psql -f "create_test_table.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Test table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`n❌ Error creating test table. Exit code: $LASTEXITCODE"
    }
    
    # Clean up and exit if successful
    $env:PGPASSWORD = $null
    $env:PGHOST = $null
    $env:PGUSER = $null
    $env:PGSSLMODE = $null
    $env:PGSSLROOTCERT = $null
    exit 0
}

# Approach 4: Try minimal SSL connection
Write-Host "`n[Approach 4] Trying minimal SSL connection..."
& psql "host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require" -c "SELECT version();"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Approach 4 succeeded! Using this configuration to create test table..."
    & psql "host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require" -f "create_test_table.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ Test table created successfully in the Emma AI Platform database!"
    } else {
        Write-Host "`n❌ Error creating test table. Exit code: $LASTEXITCODE"
    }
    
    # Clean up and exit if successful
    $env:PGPASSWORD = $null
    exit 0
}

# If all approaches failed
Write-Host "`n❌ All SSL connection approaches failed. Next steps:"
Write-Host "1. Verify firewall rules in Azure Portal - your IP (174.114.187.8) must be allowed"
Write-Host "2. Double-check password in Azure Portal - reset it to 'EmmaDb2025Test' if needed"
Write-Host "3. Temporarily disable SSL requirement (require_secure_transport=OFF) for testing"
Write-Host "4. Check Azure PostgreSQL server logs for more specific error information"

# Clean up
$env:PGPASSWORD = $null
$env:PGHOST = $null
$env:PGUSER = $null
$env:PGSSLMODE = $null
$env:PGSSLROOTCERT = $null
