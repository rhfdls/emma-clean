# Verifies required 8.0.8 packages exist in the local feed and performs a forced restore
$ErrorActionPreference = "Stop"

$Feed = Join-Path (Get-Location) "nuget-local-cache"
$pkgDriver = Join-Path $Feed "Npgsql.8.0.8.nupkg"
$pkgEf = Join-Path $Feed "npgsql.entityframeworkcore.postgresql.8.0.8.nupkg"

if (-not (Test-Path $Feed)) { throw "Local feed not found: $Feed" }

$missing = @()
if (-not (Test-Path $pkgDriver)) { $missing += $pkgDriver }
if (-not (Test-Path $pkgEf)) { $missing += $pkgEf }

if ($missing.Count -gt 0) {
  Write-Host "Missing packages:" -ForegroundColor Red
  $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
  throw "One or more required packages are missing."
}

Write-Host "All required packages present in local feed." -ForegroundColor Green

Write-Host "Clearing NuGet caches..." -ForegroundColor Cyan
 dotnet nuget locals all --clear

Write-Host "Restoring solution with forced evaluation..." -ForegroundColor Cyan
 dotnet restore --force-evaluate

Write-Host "Done." -ForegroundColor Green
