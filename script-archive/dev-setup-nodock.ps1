# Emma AI Platform Local Development Setup - No Docker Version
# This script sets up a local development environment without Docker dependencies

# Helper Functions
function Write-Section {
    param([string]$Title)
    Write-Host "`n===================================================" -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host "===================================================" -ForegroundColor Cyan
}

function Test-DotnetInstalled {
    $dotnetExists = $null -ne (Get-Command "dotnet" -ErrorAction SilentlyContinue)
    if (-not $dotnetExists) {
        Write-Host "⚠️ .NET SDK is not installed. Please install it from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }
    
    $dotnetVersion = (dotnet --version)
    Write-Host "✅ .NET SDK is installed (Version: $dotnetVersion)" -ForegroundColor Green
    return $true
}

function Install-DotnetEfIfNeeded {
    $dotnetEfInstalled = dotnet tool list -g | Select-String "dotnet-ef"
    if (-not $dotnetEfInstalled) {
        Write-Host "Installing dotnet-ef tool..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef --version 8.0.0 --ignore-failed-sources
        Write-Host "✅ dotnet-ef installed successfully" -ForegroundColor Green
    } else {
        Write-Host "✅ dotnet-ef is already installed" -ForegroundColor Green
    }
}

function Test-PostgresRunning {
    try {
        # Using proper PowerShell SQL command formatting with single quotes per best practices
        psql -U postgres -d emma_dev -c 'SELECT 1' 2>$null
        Write-Host "✅ PostgreSQL is running and accessible" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "⚠️ PostgreSQL is not running or accessible. Please start PostgreSQL before applying migrations." -ForegroundColor Yellow
        return $false
    }
}

function New-EnvFile {
    $envFile = ".\.env"
    
    if (-not (Test-Path $envFile)) {
        Write-Host "Creating .env file..." -ForegroundColor Yellow
        
        $envContent = @"
# Emma AI Platform Configuration

# Azure OpenAI Configuration
# IMPORTANT: Store these securely and never commit them to version control
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com/
AZURE_OPENAI_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=your-deployment-name

# Database Connection (PostgreSQL example)
CONNECTION_STRING=Host=localhost;Port=5432;Database=emma_dev;Username=postgres;Password=postgres

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
"@
        Set-Content -Path $envFile -Value $envContent
        
        Write-Host "✅ Created .env file at $envFile" -ForegroundColor Green
        Write-Host "  Please update it with your configuration" -ForegroundColor Yellow
        Write-Host "  IMPORTANT: Keep your API keys secure and never commit them to version control" -ForegroundColor Red
    } 
    else {
        Write-Host "✅ .env file already exists at $envFile" -ForegroundColor Green
    }
}

function Invoke-DotnetRestore {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore
    Write-Host "✅ NuGet packages restored successfully" -ForegroundColor Green
}

function Invoke-EfMigrations {
    if (Test-PostgresRunning) {
        Write-Host "Applying EF Core migrations to PostgreSQL..." -ForegroundColor Yellow
        try {
            dotnet ef database update --project Emma.Data --startup-project Emma.Api
            Write-Host "✅ EF Core migrations applied successfully" -ForegroundColor Green
        } catch {
            Write-Host "❌ Failed to apply EF Core migrations: $_" -ForegroundColor Red
            Write-Host "Please check your PostgreSQL connection and database configuration" -ForegroundColor Yellow
        }
    }
}

function Show-NextSteps {
    Write-Section "SETUP COMPLETED"
    
    Write-Host "NEXT STEPS:" -ForegroundColor Cyan
    Write-Host "1. Update the .env file with your Azure OpenAI and PostgreSQL settings"
    Write-Host "2. Build the solution: dotnet build"
    Write-Host "3. Run the tests: dotnet test"
    Write-Host "4. Start the application: dotnet run --project Emma.Api"
    Write-Host ""
    Write-Host "For more information, see the Emma AI Platform documentation."
}

# --- Main Script ---
Write-Host "Emma AI Platform - Local Development Setup (No Docker)" -ForegroundColor Green

# Check .NET SDK
Write-Section "CHECKING PREREQUISITES"
$dotnetInstalled = Test-DotnetInstalled
if (-not $dotnetInstalled) {
    exit 1
}

# Install dotnet-ef tool if needed
Install-DotnetEfIfNeeded

# Create .env file
Write-Section "SETTING UP ENVIRONMENT"
New-EnvFile

# Restore NuGet packages
Write-Section "RESTORING PACKAGES"
Invoke-DotnetRestore

# Apply EF Core migrations
Write-Section "APPLYING DATABASE MIGRATIONS"
Invoke-EfMigrations

# Show next steps
Show-NextSteps
