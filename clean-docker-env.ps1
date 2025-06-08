# Clean-Docker-Env.ps1
# This script removes Docker-related environment variables and ensures
# the Emma AI Platform uses direct host configurations

Write-Host "Cleaning Docker-related environment variables for Emma AI Platform..." -ForegroundColor Cyan

# Set environment to Local
$env:ASPNETCORE_ENVIRONMENT = "Local"
Write-Host "Set ASPNETCORE_ENVIRONMENT to: $env:ASPNETCORE_ENVIRONMENT" -ForegroundColor Green

# Clear any Docker-related connection strings
if ($env:ConnectionStrings__PostgreSql) {
    if ($env:ConnectionStrings__PostgreSql -like "*docker*" -or $env:ConnectionStrings__PostgreSql -like "*postgres:*") {
        $env:ConnectionStrings__PostgreSql = $null
        Write-Host "Cleared Docker-related PostgreSQL connection string" -ForegroundColor Green
    }
}

# Clear other Docker-related environment variables
$dockerEnvVars = @(
    "DOCKER_HOST",
    "DOCKER_CERT_PATH",
    "DOCKER_TLS_VERIFY",
    "DOCKER_MACHINE_NAME",
    "COMPOSE_CONVERT_WINDOWS_PATHS",
    "COMPOSE_PROJECT_NAME"
)

foreach ($var in $dockerEnvVars) {
    if (Test-Path env:$var) {
        Remove-Item env:$var
        Write-Host "Removed $var environment variable" -ForegroundColor Green
    }
}

# Verify that Docker container variables are not present
$dockerContainerVars = Get-ChildItem Env: | Where-Object { 
    $_.Name -like "*DOCKER*" -or 
    $_.Name -like "*CONTAINER*" -or 
    $_.Value -like "*docker*" -or 
    $_.Value -like "*container*" -or
    $_.Value -like "*postgres:*"
}

if ($dockerContainerVars) {
    Write-Host "`nRemaining Docker-related variables that might need manual review:" -ForegroundColor Yellow
    $dockerContainerVars | ForEach-Object {
        Write-Host "  - $($_.Name): $($_.Value)" -ForegroundColor Yellow
    }
} else {
    Write-Host "`nNo remaining Docker-related environment variables found." -ForegroundColor Green
}

# Remind about configuration files
Write-Host "`nRemember to review these configuration files for Docker references:" -ForegroundColor Cyan
Write-Host "  - Emma.Api/appsettings.json" -ForegroundColor White
Write-Host "  - Emma.Api/appsettings.Development.json" -ForegroundColor White
Write-Host "  - Emma.Data/appsettings.json" -ForegroundColor White
Write-Host "  - Emma.Data/appsettings.Development.json" -ForegroundColor White

Write-Host "`nEnvironment is now configured for direct host development of the Emma AI Platform" -ForegroundColor Green
