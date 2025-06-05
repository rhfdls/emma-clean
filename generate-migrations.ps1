# Emma AI Platform - Generate SQL Migration Script
# Simple script to generate EF Core migrations SQL

# Navigate to Emma.Data
Set-Location -Path "Emma.Data"

# Generate the SQL script
Write-Host "Generating migration SQL script from Emma.Data..." -ForegroundColor Yellow
dotnet ef migrations script --startup-project ../Emma.Api --output ../emma-migrations.sql --idempotent

# Return to root directory
Set-Location -Path ".."

# Check the generated file
if (Test-Path "emma-migrations.sql") {
    $fileSize = (Get-Item "emma-migrations.sql").Length
    Write-Host "SQL script generated: emma-migrations.sql ($fileSize bytes)" -ForegroundColor Green
} else {
    Write-Host "Failed to generate SQL script" -ForegroundColor Red
}
