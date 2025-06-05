# Test Azure PostgreSQL connection with proper SSL certificates for Emma AI Platform
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Create a fresh certificates directory
$certsDir = ".\certs"
if (-not (Test-Path $certsDir)) {
    New-Item -Path $certsDir -ItemType Directory | Out-Null
    Write-Host "Created directory: $certsDir"
}

# Download certificates properly
Write-Host "Downloading required root certificates for secure connection..."
$msRSARootUrl = "https://www.microsoft.com/pkiops/certs/Microsoft%20RSA%20Root%20Certificate%20Authority%202017.crt"
$msRSARootPath = "$certsDir\MicrosoftRSARootCertificateAuthority2017.crt"
Invoke-WebRequest -Uri $msRSARootUrl -OutFile $msRSARootPath

$digiCertG2Url = "https://cacerts.digicert.com/DigiCertGlobalRootG2.crt"
$digiCertG2Path = "$certsDir\DigiCertGlobalRootG2.crt"
Invoke-WebRequest -Uri $digiCertG2Url -OutFile $digiCertG2Path

$digiCertRootUrl = "https://cacerts.digicert.com/DigiCertGlobalRootCA.crt"
$digiCertRootPath = "$certsDir\DigiCertGlobalRootCA.crt"
Invoke-WebRequest -Uri $digiCertRootUrl -OutFile $digiCertRootPath

Write-Host "Certificates downloaded successfully."

# Database connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "emma"
$pgUser = "emmaadmin@emma-db-server"
$pgPassword = "GOGdb54321"

# Set password as environment variable for psql
$env:PGPASSWORD = $pgPassword

Write-Host "`nTesting connection to Emma AI Platform PostgreSQL database with SSL verification..."
Write-Host "Server: $pgServer"
Write-Host "Database: $pgDatabase"
Write-Host "User: $pgUser"
Write-Host "Using root certificates for SSL verification"

# Set PGSSLROOTCERT to the Microsoft RSA Root Certificate
$env:PGSSLROOTCERT = $msRSARootPath
$env:PGSSLMODE = "verify-full"

# Try connection with verify-full SSL mode
Write-Host "`nAttempting connection with verify-full SSL mode (recommended by Azure)..."
& psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -c "SELECT 'Connected successfully with verify-full!' AS status;" 

if ($LASTEXITCODE -ne 0) {
    # If verify-full fails, try with require mode
    Write-Host "`nVerify-full mode failed, trying with require mode..."
    $env:PGSSLMODE = "require"
    & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -c "SELECT 'Connected successfully with require mode!' AS status;"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nBoth SSL modes failed. Trying one more approach with sslmode in connection string..."
        & psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require" -c "SELECT 'Connected with connection string!' AS status;"
    }
}

# Clear sensitive environment variables
$env:PGPASSWORD = $null
$env:PGSSLROOTCERT = $null
$env:PGSSLMODE = $null

Write-Host "`nConnection testing complete."
