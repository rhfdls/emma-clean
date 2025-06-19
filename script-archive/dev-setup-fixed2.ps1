# Emma AI Platform Local Development Setup Script

# Set Error Action Preference
$ErrorActionPreference = 'Stop'

# Prints a colored section header for better readability
function Write-Section {
    param([string]$Title)
    Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

# Returns $true if a command exists in the current environment
function Test-CommandExists {
    param([string]$Command)
    return ($null -ne (Get-Command $Command -ErrorAction SilentlyContinue))
}

# Checks if PostgreSQL is running: tries psql if available, else falls back to port check
function Test-PostgresRunning {
    if (Test-CommandExists "psql") {
        try {
            # Using proper PowerShell SQL command formatting with single quotes per best practices
            psql -U postgres -d emma_dev -c 'SELECT 1' 2>$null
            return $true
        } catch {
            return $false
        }
    } else {
        try {
            $connection = Test-NetConnection -ComputerName "localhost" -Port 5432 -WarningAction SilentlyContinue
            return $connection.TcpTestSucceeded
        } catch {
            return $false
        }
    }
}

# Ensures dotnet-ef is installed (checks both command and global tool list)
function Install-DotnetEfIfNeeded {
    $dotnetEfInPath = Test-CommandExists "dotnet-ef"
    $dotnetEfInList = dotnet tool list -g | Select-String "dotnet-ef"
    if (-not ($dotnetEfInPath -or $dotnetEfInList)) {
        Write-Host "dotnet-ef not found, installing..." -ForegroundColor Yellow
        try {
            dotnet tool install --global dotnet-ef --version 8.0.0 --ignore-failed-sources
            Write-Host "✓ dotnet-ef installed successfully" -ForegroundColor Green
        } catch {
            Write-Error "Failed to install dotnet-ef: $_"
            throw
        }
    } else {
        Write-Host "✓ dotnet-ef is already installed" -ForegroundColor Green
    }
}

# --- Function Definitions ---

function Ensure-Admin {
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Error "This script requires administrator privileges. Please run as administrator."
        exit 1
    }
}

function Verify-RequiredEnvironmentVariables {
    Write-Host "Checking environment variables..." -ForegroundColor Yellow
    $missingVars = @()
    
    # Add any critical environment variables that should be checked
    # Example: Check for Azure OpenAI Key if needed
    # if ([string]::IsNullOrEmpty($env:AZURE_OPENAI_KEY)) {
    #     $missingVars += "AZURE_OPENAI_KEY"
    # }
    
    if ($missingVars.Count -gt 0) {
        Write-Warning "Missing required environment variables: $($missingVars -join ', ')"
        return $false
    }
    
    Write-Host "✓ Required environment variables are set" -ForegroundColor Green
    return $true
}

function Start-TranscriptIfNeeded {
    try {
        if ($Host.PrivateData.TranscriptFile) {
            Write-Host "Stopping existing transcript..." -ForegroundColor Yellow
            Stop-Transcript | Out-Null
        }
    } catch {
        # Silently continue if transcript can't be stopped
    }
}

function Install-DotnetSdk {
    if (Test-CommandExists "dotnet") {
        $dotnetVersion = (dotnet --version)
        Write-Host "✓ .NET SDK is installed (Version: $dotnetVersion)" -ForegroundColor Green
    } else {
        Write-Host "Installing .NET SDK..." -ForegroundColor Yellow
        try {
            $installerPath = "$env:TEMP\dotnet-sdk.exe"
            Invoke-WebRequest -Uri "https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-8.0-windows-x64-installer" -OutFile $installerPath
            Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -NoNewWindow
            Remove-Item -Path $installerPath -Force -ErrorAction SilentlyContinue
            Write-Host "✓ .NET SDK installed successfully" -ForegroundColor Green
        } catch {
            Write-Error "Failed to install .NET SDK: $_"
            throw
        }
    }
}

function Install-NodeJs {
    if (Test-CommandExists "node") {
        $nodeVersion = (node --version)
        Write-Host "✓ Node.js is installed (Version: $nodeVersion)" -ForegroundColor Green
    } else {
        Write-Host "Installing Node.js..." -ForegroundColor Yellow
        try {
            $installerPath = "$env:TEMP\node-installer.msi"
            Invoke-WebRequest -Uri "https://nodejs.org/dist/v20.13.1/node-v20.13.1-x64.msi" -OutFile $installerPath
            Start-Process -FilePath "msiexec.exe" -ArgumentList "/i", "`"$installerPath`"", "/qn", "/norestart" -Wait -NoNewWindow
            Remove-Item -Path $installerPath -Force -ErrorAction SilentlyContinue
            Write-Host '✓ Node.js installed successfully (restart your terminal if not detected)' -ForegroundColor Green
        } catch {
            Write-Error "Failed to install Node.js: $_"
            throw
        }
    }
}

function Install-AzureCli {
    if (-not (Test-CommandExists "az")) {
        Write-Host "Installing Azure CLI..." -ForegroundColor Yellow
        try {
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile AzureCLI.msi
            Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet /norestart'
            Remove-Item -Path AzureCLI.msi -Force -ErrorAction SilentlyContinue
            Write-Host "✓ Azure CLI installed successfully" -ForegroundColor Green
        } catch {
            Write-Error "Failed to install Azure CLI: $_"
            throw
        }
    } else {
        Write-Host "✓ Azure CLI is already installed" -ForegroundColor Green
    }
}

function Install-GlobalNpmPackages {
    Write-Host "Installing npm packages..." -ForegroundColor Yellow
    try {
        npm install -g @azure/storage-blob @azure/identity --loglevel=error
        Write-Host "✓ npm packages installed successfully" -ForegroundColor Green
    } catch {
        Write-Error "Failed to install npm packages: $_"
        throw
    }
}

function Restore-NugetPackages {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    try {
        dotnet restore
        Write-Host "✓ NuGet packages restored successfully" -ForegroundColor Green
    } catch {
        Write-Error "Failed to restore NuGet packages: $_"
        throw
    }
}

function Setup-EnvFile {
    $envFile = "$PSScriptRoot\.env"
    if (-not (Test-Path $envFile)) {
        Write-Host "Creating .env file..." -ForegroundColor Yellow
        @"
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
"@ | Out-File -FilePath $envFile -Encoding utf8
        Write-Host "Created .env file at $envFile" -ForegroundColor Green
        Write-Host "  Please update it with your configuration" -ForegroundColor Yellow
        Write-Host "  IMPORTANT: Keep your API keys secure and never commit them to version control" -ForegroundColor Red
    } else {
        Write-Host "✓ .env file already exists at $envFile" -ForegroundColor Green
    }
}

function Setup-TestConfig {
    $testConfigPath = "$PSScriptRoot\tests\Emma.Api.IntegrationTests\appsettings.Development.json"
    $testConfigTemplatePath = "$PSScriptRoot\tests\Emma.Api.IntegrationTests\appsettings.Development.template.json"
    if (-not (Test-Path $testConfigPath) -and (Test-Path $testConfigTemplatePath)) {
        Write-Host "Creating test configuration..." -ForegroundColor Yellow
        try {
            $testConfigDir = Split-Path -Path $testConfigPath -Parent
            if (-not (Test-Path $testConfigDir)) {
                New-Item -ItemType Directory -Path $testConfigDir -Force | Out-Null
            }
            Copy-Item -Path $testConfigTemplatePath -Destination $testConfigPath -Force
            Write-Host "✓ Created test configuration at $testConfigPath" -ForegroundColor Green
            Write-Host "  Please update it with your test settings" -ForegroundColor Yellow
        } catch {
            Write-Error "Failed to create test configuration: $_"
            throw
        }
    } elseif (Test-Path $testConfigPath) {
        Write-Host "✓ Test configuration already exists at $testConfigPath" -ForegroundColor Green
    }
}

function Apply-EfMigrations {
    Write-Section -Title "4. APPLYING EF CORE MIGRATIONS"
    if (Test-PostgresRunning) {
        Write-Host "Applying EF Core migrations to PostgreSQL..." -ForegroundColor Yellow
        try {
            # Note: Based on the memory, both direct dotnet commands and Docker commands are supported
            # Option 1: Direct command
            dotnet ef database update --project Emma.Data --startup-project Emma.Api
            
            # Option 2: Docker command (commented out but available)
            # docker-compose run --rm emmaapi dotnet ef database update --project Emma.Data --startup-project Emma.Api
            
            Write-Host "✓ EF Core migrations applied successfully" -ForegroundColor Green
        } catch {
            Write-Error "Failed to apply EF Core migrations: $_"
            throw
        }
    } else {
        Write-Error "PostgreSQL is not running or accessible. Please start PostgreSQL and re-run this script to apply migrations."
    }
}

function Show-NextSteps {
    Write-Section -Title "5. SETUP COMPLETED SUCCESSFULLY!"
    Write-Host "`nNEXT STEPS:" -ForegroundColor Cyan -BackgroundColor DarkBlue
    Write-Host "1. Update the .env file with your Azure OpenAI, PostgreSQL, and Cosmos DB settings"
    Write-Host "   - For Cosmos DB, ensure your connection string points to your remote instance."
    Write-Host "   - IMPORTANT: Remember to keep your API keys secure and never commit them to version control"
    Write-Host "2. Run 'dotnet build' to build the solution"
    Write-Host "3. Run 'dotnet test' to run all tests"
    Write-Host "4. To start the application:"
    Write-Host "   - Set your working directory to the API project"
    Write-Host "   - Run 'dotnet run'"
    Write-Host "   - Or press F5 in Visual Studio/VS Code"
    Write-Host "`nFor Azure OpenAI integration tests, ensure you have set up the required environment variables." -ForegroundColor Yellow
    Write-Host "`nLog file: $logFile" -ForegroundColor Gray
}

# --- Main Script ---

try {
    Ensure-Admin
    $logFile = "$PSScriptRoot\setup-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    Start-TranscriptIfNeeded
    Start-Transcript -Path $logFile -Force
    Write-Host "Logging to $logFile" -ForegroundColor Yellow
    
    Write-Section -Title "1. CHECKING PREREQUISITES"
    Verify-RequiredEnvironmentVariables
    
    Write-Section -Title "2. INSTALLING DEPENDENCIES"
    Install-DotnetSdk
    Install-NodeJs
    Install-AzureCli
    Install-GlobalNpmPackages
    Install-DotnetEfIfNeeded
    
    Write-Section -Title "3. SETTING UP DEVELOPMENT ENVIRONMENT"
    Restore-NugetPackages
    Setup-EnvFile
    Setup-TestConfig
    Apply-EfMigrations
    Show-NextSteps
} catch {
    Write-Host "`nERROR: $_" -ForegroundColor Red
    Write-Host "Stack Trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
} finally {
    try { Stop-Transcript } catch {}
}
