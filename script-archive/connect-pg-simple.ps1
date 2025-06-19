# Simple PostgreSQL Connection Script for Emma AI Platform
# Uses simple SSL approach with sslmode=require

# Add PostgreSQL bin directory to PATH (fix for missing PATH entry)
$env:Path = "C:\Program Files\PostgreSQL\16\bin;" + $env:Path

# Verify psql is available
Write-Host "Checking PostgreSQL client..." -ForegroundColor Yellow
$pgVersion = & psql --version
Write-Host "✅ Using: $pgVersion" -ForegroundColor Green

# Connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "postgres" # Starting with postgres system database
$pgUser = "emmaadmin"    # Just username without server suffix
$pgPassword = "GOGdb54321"

# Set password securely as environment variable
$env:PGPASSWORD = $pgPassword

Write-Host "Emma AI Platform PostgreSQL Connection"
Write-Host "======================================"
Write-Host "Connecting with simplified SSL approach (sslmode=require)..."

# Connect to database
& psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser

# Clean up environment variables
$env:PGPASSWORD = $null
