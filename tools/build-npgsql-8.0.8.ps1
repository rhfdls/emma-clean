# Builds a private Npgsql 8.0.7 NuGet package and places it in the repo-local feed
# Usage: run from solution root or anywhere: powershell -ExecutionPolicy Bypass -File .\tools\build-npgsql-8.0.8.ps1

# Parameters (must appear before any executable statements)
param(
  [string]$Version = "8.0.7",   # Driver package version to build
  [string]$Tag = "v8.0.7",      # Git tag to checkout
  [switch]$AddLocalSource        # If set, register nuget-local-cache as a NuGet source
)

$ErrorActionPreference = "Stop"

# Settings
$RepoRoot = Join-Path $env:USERPROFILE "source\\npgsql"
$LocalFeed = Join-Path (Get-Location) "nuget-local-cache"

function Ensure-Tool($name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    throw "Required tool not found on PATH: $name"
  }
}

# Preconditions
Ensure-Tool git
Ensure-Tool dotnet

# Ensure local feed folder exists
if (-not (Test-Path $LocalFeed)) { New-Item -ItemType Directory -Path $LocalFeed | Out-Null }

# Clone or update repo
if (-not (Test-Path $RepoRoot)) {
  Write-Host "Cloning Npgsql repository..." -ForegroundColor Cyan
  git clone https://github.com/npgsql/npgsql.git $RepoRoot
}

Push-Location $RepoRoot
try {
  Write-Host "Fetching tags..." -ForegroundColor Cyan
  git fetch --tags

  # Validate tag exists
  $tagExists = git tag --list $Tag
  if (-not $tagExists) { throw "Git tag not found: $Tag" }

  Write-Host "Checking out tag $Tag ..." -ForegroundColor Cyan
  git checkout $Tag

  Write-Host "Restoring and packing Npgsql $Version from $Tag ..." -ForegroundColor Cyan
  dotnet restore
  # Build package with explicit version to ensure .nupkg name matches 8.0.8
  dotnet pack src/Npgsql/Npgsql.csproj -c Release -p:Version=$Version

  $nupkgPath = Join-Path $RepoRoot "src/Npgsql/bin/Release/Npgsql.$Version.nupkg"
  if (-not (Test-Path $nupkgPath)) { throw "Package not found: $nupkgPath" }

  Write-Host "Copying package to local feed: $LocalFeed" -ForegroundColor Cyan
  Copy-Item $nupkgPath $LocalFeed -Force
}
finally {
  Pop-Location
}

Write-Host "Clearing NuGet caches..." -ForegroundColor Cyan
 dotnet nuget locals all --clear

if ($AddLocalSource) {
  Write-Host "Ensuring local NuGet source is registered..." -ForegroundColor Cyan
  $sources = dotnet nuget list source | Out-String
  if ($sources -notmatch "LocalNpgsql") {
    dotnet nuget add source $LocalFeed -n LocalNpgsql | Out-Null
  }
  Write-Host "Forcing restore to re-evaluate sources..." -ForegroundColor Cyan
  dotnet restore --force-evaluate | Out-Null
}

Write-Host ("Done. Placed: {0}" -f (Join-Path $LocalFeed ("Npgsql.{0}.nupkg" -f $Version))) -ForegroundColor Green
Write-Host "Next: ensure Directory.Packages.props pins Npgsql=$Version (or desired) and restore with: dotnet restore --force-evaluate" -ForegroundColor Yellow
