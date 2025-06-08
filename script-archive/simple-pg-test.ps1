# Very simple direct PostgreSQL connection test for Emma AI Platform
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Set password directly (will be removed after testing)
$env:PGPASSWORD = 'GOGdb54321'

Write-Host "Attempting simple connection to Emma AI Platform database..."

# Simplest possible PSQL command with explicit parameters
& psql -h "emma-db-server.postgres.database.azure.com" -p 5432 -d "emma" -U "emmaadmin@emma-db-server" -c "SELECT version();" -w

if ($LASTEXITCODE -eq 0) {
    Write-Host "Connection successful!"
} else {
    Write-Host "Connection failed with exit code: $LASTEXITCODE"
    Write-Host "`nTroubleshooting suggestions:"
    Write-Host "1. Verify the password is correct in Azure Portal"
    Write-Host "2. Confirm your IP ($($env:ClientIPAddress)) is added to the firewall rules"
    Write-Host "3. Check if the server has SSL requirements"
}

# Clear password from environment
$env:PGPASSWORD = $null
