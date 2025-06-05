# Emma AI Platform - Database Schema Script Generator
# This script generates SQL to view database tables

# Load environment variables
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), [System.EnvironmentVariableTarget]::Process)
        }
    }
    $connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    Write-Host "Environment variables loaded" -ForegroundColor Green
} else {
    Write-Host "ERROR: .env file not found" -ForegroundColor Red
    exit 1
}

# Create SQL script to view schema
$scriptFile = "view-tables.sql"
@'
-- Emma AI Platform Database Schema Query
-- Run this in Azure Portal Query Editor or any PostgreSQL client

-- List all tables in public schema
SELECT table_name, table_type
FROM information_schema.tables 
WHERE table_schema = 'public'
ORDER BY table_name;

-- Check if migrations history table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = '__EFMigrationsHistory'
) AS migrations_table_exists;

-- List applied migrations if table exists
SELECT * FROM "__EFMigrationsHistory"
WHERE EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = '__EFMigrationsHistory'
);
'@ | Set-Content -Path $scriptFile

Write-Host "Generated SQL script to view database schema: $scriptFile" -ForegroundColor Green
Write-Host "`nTo check your database tables:" -ForegroundColor Cyan
Write-Host "1. Go to Azure Portal > PostgreSQL server > Query editor" -ForegroundColor White
Write-Host "2. Connect using your admin credentials" -ForegroundColor White
Write-Host "3. Copy and paste the SQL from $scriptFile" -ForegroundColor White
Write-Host "4. Run the query to see your tables" -ForegroundColor White

# Generate EF Core migrations script as backup plan
Write-Host "`nGenerating Entity Framework migrations script..." -ForegroundColor Yellow
Set-Location -Path "Emma.Api"
dotnet ef migrations script -o "migrations-script.sql"
Set-Location -Path ".."

if (Test-Path "Emma.Api\migrations-script.sql") {
    Write-Host "Generated migrations script: Emma.Api\migrations-script.sql" -ForegroundColor Green
    Write-Host "You can run this script manually in Azure Portal Query Editor if needed" -ForegroundColor Yellow
} else {
    Write-Host "Failed to generate migrations script" -ForegroundColor Red
}

Write-Host "`nEF Core migration commands to try:" -ForegroundColor Cyan
Write-Host "cd Emma.Api" -ForegroundColor Gray
Write-Host "dotnet ef database update" -ForegroundColor Gray
