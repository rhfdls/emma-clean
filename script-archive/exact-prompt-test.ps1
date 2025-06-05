# Test Emma AI Platform PostgreSQL connection with exact syntax from diagnostic prompt
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Clear any existing PostgreSQL environment variables
$env:PGPASSWORD = $null
$env:PGHOST = $null
$env:PGUSER = $null
$env:PGSSLMODE = $null
$env:PGSSLROOTCERT = $null

# Set password
$env:PGPASSWORD = "EmmaDb2025Test"

Write-Host "Emma AI Platform PostgreSQL Connection Test"
Write-Host "=======================================`n"

# Attempt exactly as shown in the diagnostic prompt - NO extra arguments
Write-Host "Attempting connection with exact syntax from diagnostic prompt..."
$command = "psql ""host=emma-db-server.postgres.database.azure.com port=5432 dbname=emma user=emmaadmin@emma-db-server sslmode=require sslrootcert=./certs/BaltimoreCyberTrustRoot.crt.pem"""
Write-Host "Command: $command`n"

# Execute the command
Invoke-Expression $command

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nConnection successful! You are now connected to the Emma AI Platform PostgreSQL database."
    Write-Host "You can run SQL commands directly in the psql console above."
} else {
    Write-Host "`nConnection failed with exit code: $LASTEXITCODE"
    Write-Host "Check that:"
    Write-Host "1. Your IP address (174.114.187.8) is added to the Azure PostgreSQL firewall rules"
    Write-Host "2. The password is correct in Azure"
    Write-Host "3. The SSL certificate path is correct (./certs/BaltimoreCyberTrustRoot.crt.pem)"
}

# Clean up
$env:PGPASSWORD = $null
