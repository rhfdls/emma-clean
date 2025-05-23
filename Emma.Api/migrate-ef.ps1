<#
.SYNOPSIS
    Creates and applies a new Entity Framework Core migration with a timestamp.

.DESCRIPTION
    This script:
    1. Changes to the Emma.Api project directory if not already there
    2. Creates a timestamp in the format yyyyMMdd_HHmmss
    3. Creates a new EF Core migration with the timestamp
    4. Applies the migration to the database
#>

# Set error action preference
$ErrorActionPreference = "Stop"

try {
    # Get the current directory
    $currentDir = Get-Location
    
    # Check if we're in the Emma.Api directory by looking for the .csproj file
    $projectFile = Join-Path -Path $currentDir -ChildPath "Emma.Api.csproj"
    
    # If not found, try to navigate to the Emma.Api directory
    if (-not (Test-Path $projectFile)) {
        $apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"
        if (Test-Path $apiDir) {
            Set-Location -Path $apiDir
            Write-Host "Changed to directory: $apiDir" -ForegroundColor Green
        }
        else {
            Write-Host "Emma.Api directory not found. Please run this script from the solution root or the Emma.Api directory." -ForegroundColor Red
            exit 1
        }
    }

    # Generate timestamp
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $migrationName = "AutoMigration_$timestamp"
    
    Write-Host "Creating migration: $migrationName" -ForegroundColor Cyan
    
    # Add migration
    $migrationCommand = "dotnet ef migrations add `"$migrationName`""
    
    # If we're not in the project directory, specify the project file
    if ((Get-Location).Name -ne "Emma.Api") {
        $migrationCommand += " --project .\Emma.Api\Emma.Api.csproj"
    }
    
    Invoke-Expression $migrationCommand
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create migration"
    }
    
    Write-Host "Migration created successfully. Applying to database..." -ForegroundColor Green
    
    # Update database
    $updateCommand = "dotnet ef database update"
    
    if ((Get-Location).Name -ne "Emma.Api") {
        $updateCommand += " --project .\Emma.Api\Emma.Api.csproj"
    }
    
    Invoke-Expression $updateCommand
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to update database"
    }
    
    Write-Host "Database updated successfully!" -ForegroundColor Green
}
catch {
    Write-Host "An error occurred: $_" -ForegroundColor Red
    exit 1
}
finally {
    # Change back to the original directory
    Set-Location -Path $currentDir
}