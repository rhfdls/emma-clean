# Emma AI Platform - Generate SQL Migration Script
# This script generates the SQL to create all database tables

# Create output directory if it doesn't exist
$outputDir = "sql-scripts"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Create timestamped filename
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputFile = Join-Path -Path $outputDir -ChildPath "emma-migrations-$timestamp.sql"

# Navigate to Emma.Data project
Set-Location -Path "Emma.Data"
Write-Host "Generating SQL migration script from Emma.Data project..." -ForegroundColor Yellow

# Generate idempotent SQL script (works regardless of current database state)
dotnet ef migrations script --startup-project ../Emma.Api --output "../$outputFile" --idempotent

# Return to root directory
Set-Location -Path ".."

# Check if file was created successfully
if (Test-Path $outputFile) {
    $fileSize = (Get-Item $outputFile).Length
    
    Write-Host "`nSUCCESS: SQL migration script generated!" -ForegroundColor Green
    Write-Host "File: $outputFile" -ForegroundColor Green
    Write-Host "Size: $fileSize bytes" -ForegroundColor Green
    
    if ($fileSize -gt 0) {
        Write-Host "`nTo apply this SQL script:" -ForegroundColor Cyan
        Write-Host "1. Go to Azure Portal > PostgreSQL server > Query editor" -ForegroundColor White
        Write-Host "2. Connect using your admin credentials" -ForegroundColor White
        Write-Host "3. Copy and paste the contents of the SQL file" -ForegroundColor White
        Write-Host "4. Run the query to create all Emma AI Platform tables" -ForegroundColor White
        
        # Preview the first few lines of the SQL
        Write-Host "`nScript preview:" -ForegroundColor Yellow
        Get-Content $outputFile -TotalCount 20 | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
        Write-Host "..." -ForegroundColor Gray
    } else {
        Write-Host "`nWARNING: Generated file is empty. No migrations may exist." -ForegroundColor Red
    }
} else {
    Write-Host "`nERROR: Failed to generate migration script" -ForegroundColor Red
}
