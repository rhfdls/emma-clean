# Test password authentication for Emma AI Platform PostgreSQL
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Set connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "emma"
$pgUser = "emmaadmin@emma-db-server"
$pgPassword = "GOGdb54321"

# Set password as environment variable
$env:PGPASSWORD = $pgPassword

Write-Host "Testing password authentication for Emma AI Platform database..."
Write-Host "Server: $pgServer"
Write-Host "User: $pgUser"
Write-Host "Password: [MASKED]"

# Try connection with minimal parameters and sslmode=require
Write-Host "`nAttempting connection with sslmode=require..."
& psql "host=$pgServer port=$pgPort dbname=$pgDatabase user=$pgUser sslmode=require" -c "SELECT version();" -w

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPassword authentication successful!"
    Write-Host "You can now proceed with executing SQL scripts."
} else {
    Write-Host "`nPassword authentication failed. Error code: $LASTEXITCODE"
    Write-Host "`nRecommendations:"
    Write-Host "1. Reset the password in Azure Portal to a simpler value without special characters"
    Write-Host "2. Update your .env file with the new password"
    Write-Host "3. Try connecting again with the new password"
}

# Clear password from environment
$env:PGPASSWORD = $null
