# Emma AI Platform - SQL Migration Script Generator
# This script only generates the SQL file without applying migrations

# Load environment variables (just to ensure they're set)
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), [System.EnvironmentVariableTarget]::Process)
        }
    }
}

# Create output directory
$outputDir = "migration-scripts"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Generate timestamped SQL file
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputFile = Join-Path -Path $outputDir -ChildPath "migration-$timestamp.sql"

# Navigate to Emma.Data
Set-Location -Path "Emma.Data"
Write-Host "Generating SQL migration script from Emma.Data..." -ForegroundColor Yellow

# Generate idempotent migration script
dotnet ef migrations script --startup-project ../Emma.Api --output "../$outputFile" --idempotent

# Return to original directory
Set-Location -Path ".."

# Display results
if (Test-Path $outputFile) {
    $fileSize = (Get-Item $outputFile).Length
    Write-Host "Successfully generated migration script: $outputFile ($fileSize bytes)" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Review the generated SQL file to see what tables should be created" -ForegroundColor White
    Write-Host "2. Connect to your Azure PostgreSQL database through Azure Portal" -ForegroundColor White
    Write-Host "3. Run this SQL script to manually create all tables" -ForegroundColor White
    Write-Host "4. Refresh your database connection in VS Code to verify tables" -ForegroundColor White
} else {
    Write-Host "Failed to generate migration script" -ForegroundColor Red
}
