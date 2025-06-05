# Basic PostgreSQL client check for Emma AI Platform
# This script checks for psql availability and tries to find it

Write-Host "Emma AI Platform - PostgreSQL Client Check" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Check current PATH
Write-Host "Current PATH environment variable:" -ForegroundColor Yellow
$env:Path -split ";" | ForEach-Object { Write-Host "  $_" }

# Try to find psql in common locations
Write-Host "`nSearching for psql.exe in common locations..." -ForegroundColor Yellow

$commonLocations = @(
    "C:\Program Files\PostgreSQL\16\bin",
    "C:\Program Files\PostgreSQL\15\bin",
    "C:\Program Files\PostgreSQL\14\bin",
    "C:\Program Files\PostgreSQL\13\bin",
    "C:\Program Files\PostgreSQL\12\bin",
    "C:\Program Files (x86)\PostgreSQL\16\bin",
    "C:\Program Files (x86)\PostgreSQL\15\bin"
)

$psqlFound = $false

foreach ($location in $commonLocations) {
    $psqlPath = Join-Path -Path $location -ChildPath "psql.exe"
    if (Test-Path $psqlPath) {
        Write-Host "✅ Found psql.exe at: $psqlPath" -ForegroundColor Green
        $psqlFound = $true
        
        # Test running psql --version
        Write-Host "`nTesting psql.exe from this location:" -ForegroundColor Yellow
        try {
            $version = & $psqlPath --version 2>&1
            Write-Host "✅ psql version: $version" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Error running psql: $_" -ForegroundColor Red
        }
    }
}

if (-not $psqlFound) {
    Write-Host "❌ Could not find psql.exe in common locations" -ForegroundColor Red
    
    # Check if PostgreSQL is installed via Get-WmiObject
    Write-Host "`nChecking installed applications..." -ForegroundColor Yellow
    try {
        $installedApps = Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -like "*PostgreSQL*" }
        if ($installedApps) {
            Write-Host "Found PostgreSQL-related applications:" -ForegroundColor Green
            $installedApps | ForEach-Object { Write-Host "  $($_.Name) - $($_.Version)" }
        } else {
            Write-Host "No PostgreSQL applications found in installed programs." -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error checking installed applications: $_" -ForegroundColor Red
    }
}

Write-Host "`nTroubleshooting Steps for Emma AI Platform:" -ForegroundColor Cyan
Write-Host "1. Install PostgreSQL client tools if not already installed" -ForegroundColor White
Write-Host "2. Add the PostgreSQL bin directory to your PATH environment variable" -ForegroundColor White
Write-Host "3. Restart your PowerShell session after updating PATH" -ForegroundColor White
Write-Host "4. Try running 'psql --version' to verify installation" -ForegroundColor White
