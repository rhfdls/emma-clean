# Run-Migration.ps1
param (
    [string]$MigrationName,
    [switch]$UpdateDatabase,
    [switch]$CreateMigration
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to write colored output
function Write-Info {
    param($Message)
    Write-Host "`n[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor Cyan
}

# Function to check if running in Docker
function Test-IsInContainer {
    return (Test-Path "/.dockerenv") -or ($env:DOTNET_RUNNING_IN_CONTAINER -eq "true")
}

try {
    # Set environment
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    
    # Determine server based on environment
    $isInContainer = Test-IsInContainer
    $server = if ($isInContainer) { "postgres" } else { "localhost" }
    $connectionString = "Server=$server;Port=5432;Database=emma;User Id=postgres;Password=postgres;"
    
    Write-Info "Environment: $(if ($isInContainer) { 'Docker' } else { 'Host' })"
    Write-Info "Using server: $server"
    
    # Set project paths
    $solutionRoot = Split-Path -Parent $PSScriptRoot
    $dataProject = Join-Path $solutionRoot "Emma.Data\Emma.Data.csproj"
    $apiProject = Join-Path $solutionRoot "Emma.Api\Emma.Api.csproj"
    
    # Ensure appsettings.Development.json exists in Data project
    $apiSettings = Join-Path (Split-Path $apiProject) "appsettings.Development.json"
    $dataSettings = Join-Path (Split-Path $dataProject) "appsettings.Development.json"
    
    if ((Test-Path $apiSettings) -and (-not (Test-Path $dataSettings))) {
        Write-Info "Copying appsettings.Development.json to Data project..."
        Copy-Item $apiSettings -Destination $dataSettings -Force
    }
    
    # Build the solution first
    Write-Info "Building solution..."
    dotnet build
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    # Create migration if requested
    if ($CreateMigration) {
        if ([string]::IsNullOrEmpty($MigrationName)) {
            $MigrationName = "Migration_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        }
        
        Write-Info "Creating migration: $MigrationName"
        dotnet ef migrations add $MigrationName `
            --project $dataProject `
            --startup-project $apiProject `
            --configuration Debug
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create migration"
        }
    }
    
    # Update database if requested
    if ($UpdateDatabase -or $CreateMigration) {
        Write-Info "Applying migrations to database..."
        dotnet ef database update `
            --project $dataProject `
            --startup-project $apiProject `
            --connection "$connectionString"
            
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to update database"
        }
    }
    
    Write-Info "Operation completed successfully" -ForegroundColor Green
}
catch {
    Write-Host "`nERROR: $_" -ForegroundColor Red
    Write-Host "`nStack Trace:" -ForegroundColor DarkGray
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
    exit 1
}