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

function Test-AzurePostgresConnection {
    try {
        # Check if PostgreSQL connection string is set in environment
        $connStr = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
        if ([string]::IsNullOrEmpty($connStr)) {
            Write-Host "⚠️ Azure PostgreSQL connection string not found in environment variables." -ForegroundColor Yellow
            return $false
        }
        
        # Extract host from connection string for basic connectivity check
        if ($connStr -match 'Host=([^;]+)') {
            $pgHost = $matches[1]
            Write-Host "Verifying connectivity to Azure PostgreSQL at $pgHost..." -ForegroundColor Yellow
            
            # Simple TCP connection test to verify host is reachable
            $tcpClient = New-Object System.Net.Sockets.TcpClient
            $tcpClient.Connect($pgHost, 5432)
            $tcpClient.Close()
            
            Write-Host "✅ Azure PostgreSQL is accessible" -ForegroundColor Green
            return $true
        } else {
            Write-Host "⚠️ Could not parse Azure PostgreSQL host from connection string." -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "⚠️ Azure PostgreSQL is not accessible: $_" -ForegroundColor Yellow
        return $false
    }
}

function Import-EnvVariables {
    $envFile = "C:\Users\david\GitHub\WindsurfProjects\emma\.env"
    
    if (Test-Path $envFile) {
        Write-Host "Loading environment variables from $envFile" -ForegroundColor Yellow
        
        # Use load-env.ps1 script to load variables
        if (Test-Path ".\load-env.ps1") {
            & .\load-env.ps1
        } else {
            # Fallback if script doesn't exist
            $envContent = Get-Content $envFile
            
            foreach ($line in $envContent) {
                # Skip comments and empty lines
                if ($line.Trim().StartsWith("#") -or [string]::IsNullOrWhiteSpace($line)) {
                    continue
                }
                
                # Parse key=value format
                $parts = $line.Split('=', 2)
                if ($parts.Length -eq 2) {
                    $key = $parts[0].Trim()
                    $value = $parts[1].Trim()
                    
                    # Set the environment variable
                    [Environment]::SetEnvironmentVariable($key, $value, "Process")
                    Write-Host "  Set $key" -ForegroundColor Green
                }
            }
        }
        
        Write-Host "✅ Environment variables loaded successfully" -ForegroundColor Green
    } 
    else {
        Write-Host "⚠️ .env file not found at $envFile" -ForegroundColor Yellow
        Write-Host "  Please ensure the .env file exists at the specified path" -ForegroundColor Yellow
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
    if (Test-AzurePostgresConnection) {
        Write-Host "Applying EF Core migrations to Azure PostgreSQL..." -ForegroundColor Yellow
        try {
            dotnet ef database update --project Emma.Data --startup-project Emma.Api
            Write-Host "✅ EF Core migrations applied successfully" -ForegroundColor Green
        } catch {
            Write-Host "❌ Failed to apply EF Core migrations: $_" -ForegroundColor Red
            Write-Host "Please check your Azure PostgreSQL connection string and database configuration" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Skipping migrations as Azure PostgreSQL is not accessible." -ForegroundColor Yellow
        Write-Host "Please verify your .env file contains the correct Azure PostgreSQL connection string." -ForegroundColor Yellow
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

# Load environment variables
Write-Section "SETTING UP ENVIRONMENT"
Import-EnvVariables

# Restore NuGet packages
Write-Section "RESTORING PACKAGES"
Invoke-DotnetRestore

# Apply EF Core migrations
Write-Section "APPLYING DATABASE MIGRATIONS"
Invoke-EfMigrations

# Show next steps
Show-NextSteps
