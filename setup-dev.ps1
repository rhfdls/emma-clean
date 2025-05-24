# Setup-Dev.ps1 - Development Environment Setup Script
# This script sets up the development environment for the EMMA project
#
# Prerequisites:
# - Windows OS
# - Internet connection
# - Administrator privileges

# Set Error Action Preference
$ErrorActionPreference = 'Stop'

# Configuration
$NODE_LTS_VERSION = "20.x"  # Node.js LTS version
$DOTNET_SDK_VERSION = "8.0"  # .NET SDK version
$LOG_DIR = "$PSScriptRoot\logs"

# Function to write a section header
function Write-Section {
    param([string]$Title)
    Write-Host "`n" + ("=" * 80) -ForegroundColor Cyan
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

# Function to check if a command exists
function Test-CommandExists {
    param([string]$Command)
    $exists = $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
    Write-Verbose "Command $Command exists: $exists"
    return $exists
}

# Function to test internet connection
function Test-InternetConnection {
    try {
        $null = Invoke-WebRequest -Uri "http://www.google.com" -UseBasicParsing -TimeoutSec 10
        return $true
    } catch {
        return $false
    }
}

# Function to update PATH environment variable
function Update-Path {
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + 
               [System.Environment]::GetEnvironmentVariable("Path", "User")
}

# Initialize script
$scriptStartTime = Get-Date
$newInstallation = $false

try {
    # Check for admin privileges
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Error "This script requires administrator privileges. Please run as administrator."
        exit 1
    }
    
    # Create logs directory if it doesn't exist
    if (-not (Test-Path $LOG_DIR)) {
        New-Item -ItemType Directory -Path $LOG_DIR -Force | Out-Null
    }
    
    # Start logging
    $logFile = "$LOG_DIR\setup-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    Start-Transcript -Path $logFile -Force
    Write-Host "Starting EMMA Development Environment Setup" -ForegroundColor Cyan
    Write-Host "Logging to $logFile" -ForegroundColor Yellow
    Write-Host "Start Time: $($scriptStartTime.ToString('yyyy-MM-dd HH:mm:ss'))"
    
    # Check internet connection
    Write-Host "Checking internet connection..." -ForegroundColor Yellow -NoNewline
    if (Test-InternetConnection) {
        Write-Host " [CONNECTED]" -ForegroundColor Green
    } else {
        Write-Host " [OFFLINE]" -ForegroundColor Red
        Write-Error "No internet connection detected. Please check your network and try again."
        exit 1
    }

    # 1. Check Prerequisites
    Write-Section -Title "1. CHECKING PREREQUISITES"
    
    # Check .NET SDK
    if (Test-CommandExists "dotnet") {
        $dotnetVersion = (dotnet --version)
        if ($dotnetVersion.StartsWith($DOTNET_SDK_VERSION)) {
            Write-Host ".NET SDK $DOTNET_SDK_VERSION is installed (Version: $dotnetVersion)" -ForegroundColor Green
        } else {
            Write-Warning "Different .NET SDK version detected. Expected $DOTNET_SDK_VERSION.x but found $dotnetVersion"
            Write-Host "Please install .NET SDK $DOTNET_SDK_VERSION from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host ".NET SDK $DOTNET_SDK_VERSION not found. Please install it from https://dotnet.microsoft.com/download" -ForegroundColor Red
        exit 1
    }

    # Check Node.js
    if (Test-CommandExists "node") {
        $nodeVersion = (node --version).TrimStart('v')
        if ($nodeVersion.StartsWith($NODE_LTS_VERSION)) {
            Write-Host "Node.js LTS ($NODE_LTS_VERSION) is installed (Version: $nodeVersion)" -ForegroundColor Green
        } else {
            Write-Warning "Different Node.js version detected. Expected $NODE_LTS_VERSION.x but found $nodeVersion"
            Write-Host "Please install Node.js LTS ($NODE_LTS_VERSION) from https://nodejs.org/" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "Node.js LTS ($NODE_LTS_VERSION) not found. Please install it from https://nodejs.org/" -ForegroundColor Red
        exit 1
    }

    # 2. Install Global Packages
    Write-Section -Title "2. INSTALLING GLOBAL PACKAGES"
    
    # Install npm packages locally
    Write-Host "Installing npm packages locally..." -ForegroundColor Yellow
    try {
        Set-Location $PSScriptRoot
        if (Test-Path "package.json") {
            npm install --loglevel=error
            Write-Host "npm packages installed successfully" -ForegroundColor Green
        } else {
            npm init -y
            npm install @azure/storage-blob @azure/identity --save --loglevel=error
            Write-Host "Created new package.json and installed npm packages" -ForegroundColor Green
        }
    } catch {
        Write-Error "Failed to install npm packages: $_" -ErrorAction Continue
    }

    # Install .NET tools
    Write-Host "Installing .NET tools..." -ForegroundColor Yellow
    try {
        dotnet tool install --global dotnet-ef --version 8.0.0 --ignore-failed-sources
        Write-Host ".NET tools installed successfully" -ForegroundColor Green
    } catch {
        Write-Warning "Failed to install .NET tools: $_"
    }

    # 3. Setup Environment
    Write-Section -Title "3. SETTING UP ENVIRONMENT"
    
    # Restore NuGet packages
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    try {
        dotnet restore
        Write-Host "NuGet packages restored successfully" -ForegroundColor Green
    } catch {
        Write-Warning "Failed to restore NuGet packages: $_"
    }

    # Create .env file if it doesn't exist
    $envFile = "$PSScriptRoot\.env"
    if (-not (Test-Path $envFile)) {
        Write-Host "Creating .env file..." -ForegroundColor Yellow
        @"
# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource-name.openai.azure.com/
AZURE_OPENAI_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=your-deployment-name

# Database Connection (PostgreSQL)
CONNECTION_STRING="Host=localhost;Port=5432;Database=emma;Username=postgres;Password=yourpassword"

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
"@ | Out-File -FilePath $envFile -Encoding utf8
        Write-Host "Created .env file at $envFile" -ForegroundColor Green
        Write-Host "Please update it with your configuration" -ForegroundColor Yellow
        $newInstallation = $true
    } else {
        Write-Host ".env file already exists at $envFile" -ForegroundColor Green
    }

    # Create test configuration if it doesn't exist
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
            Write-Host "Created test configuration at $testConfigPath" -ForegroundColor Green
            Write-Host "Please update it with your test settings" -ForegroundColor Yellow
        } catch {
            Write-Warning "Failed to create test configuration: $_"
        }
    } elseif (Test-Path $testConfigPath) {
        Write-Host "Test configuration already exists at $testConfigPath" -ForegroundColor Green
    }

    # 4. Final Steps
    Write-Section -Title "SETUP COMPLETED SUCCESSFULLY!"
    
    # Refresh PATH for current session
    Update-Path
    
    Write-Host "`nNEXT STEPS:" -ForegroundColor Cyan -BackgroundColor DarkBlue
    Write-Host "1. Update the .env file with your configuration:"
    Write-Host "   - Azure OpenAI settings"
    Write-Host "   - PostgreSQL database credentials"
    Write-Host "   - Other environment-specific settings"
    Write-Host "`n2. Build and run the application:"
    Write-Host "   cd src/Emma.Api"
    Write-Host "   dotnet build"
    Write-Host "   dotnet run"
    Write-Host "`n3. Or open in VS Code:"
    Write-Host "   code ."
    Write-Host "   (Press F5 to debug)"
    
    if ($newInstallation) {
        Write-Host "`nNOTE: A new terminal window may be required for PATH changes to take effect." -ForegroundColor Yellow
    }
    
    Write-Host "`nFor Azure OpenAI integration tests, ensure you have set up the required environment variables." -ForegroundColor Yellow
    Write-Host "`nLog file: $logFile" -ForegroundColor Gray
    
    # Offer to launch VS Code
    $launchVSCode = Read-Host "`nWould you like to open the project in VS Code now? (y/n)"
    if ($launchVSCode -eq 'y') {
        if (Test-CommandExists "code") {
            code .
        } else {
            Write-Host "VS Code command line tools not found. Please open VS Code manually." -ForegroundColor Yellow
        }
    }
}
catch {
    Write-Host "`nERROR: $_" -ForegroundColor Red
    Write-Host "Stack Trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
finally {
    try { Stop-Transcript } catch {}
}
