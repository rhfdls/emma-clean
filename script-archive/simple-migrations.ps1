# Emma AI Platform - Simple Migrations Script
# Direct approach to creating database tables

# 1. Load environment variables
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

# 2. Go to Emma.Api project directory
Set-Location -Path "Emma.Api"
Write-Host "Changed to Emma.Api directory" -ForegroundColor Yellow

# 3. Run migration directly with minimal output
Write-Host "Running EF migration..." -ForegroundColor Yellow
dotnet ef database update

# 4. Report status
if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS: Migration completed successfully" -ForegroundColor Green
    Write-Host "Refresh your database connection to see the tables" -ForegroundColor Yellow
} else {
    Write-Host "ERROR: Migration failed" -ForegroundColor Red
}

# 5. Return to original directory
Set-Location -Path ".."
