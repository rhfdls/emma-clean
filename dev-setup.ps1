# Local Development Setup Script

# Install required tools
Write-Host "Installing required tools..."

# Install Azure CLI
Write-Host "Installing Azure CLI..."
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Invoke-WebRequest -Uri "https://aka.ms/installazurecliwindows" -OutFile "azure-cli.msi"
    Start-Process msiexec.exe -Wait -ArgumentList "/I azure-cli.msi /quiet"
    Remove-Item "azure-cli.msi"
}

# Install Azure Storage Emulator
Write-Host "Installing Azure Storage Emulator..."
if (-not (Test-Path "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe")) {
    Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/?linkid=868033" -OutFile "azure-storage-emulator.msi"
    Start-Process msiexec.exe -Wait -ArgumentList "/I azure-storage-emulator.msi /quiet"
    Remove-Item "azure-storage-emulator.msi"
}

# Set Error Action Preference
$ErrorActionPreference = 'Stop'

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

try {
    # Check for admin privileges
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Error "This script requires administrator privileges. Please run as administrator."
        exit 1
    }

    # Start logging
    $logFile = "$PSScriptRoot\setup-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    Start-Transcript -Path $logFile -Force
    Write-Host "Logging to $logFile" -ForegroundColor Yellow

    # 1. Check Prerequisites
    Write-Section -Title "1. CHECKING PREREQUISITES"
    
    # Check .NET SDK
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

    # Check Node.js
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
            $env:Path = [System.Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path', 'User')
            Write-Host '✓ Node.js installed successfully' -ForegroundColor Green
        } catch {
            Write-Error "Failed to install Node.js: $_"
            throw
        }
    }

    # 2. Install Required Tools
    Write-Section -Title "2. INSTALLING REQUIRED TOOLS"
    
    # Install Azure CLI if not present
    if (-not (Test-CommandExists "az")) {
        Write-Host "Installing Azure CLI..." -ForegroundColor Yellow
        try {
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile AzureCLI.msi
            Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet /norestart'
            Remove-Item -Path AzureCLI.msi -Force -ErrorAction SilentlyContinue
            Write-Host "✓ Azure CLI installed successfully" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to install Azure CLI: $_"
        }
    } else {
        Write-Host "✓ Azure CLI is already installed" -ForegroundColor Green
    }

    # Start Azure Storage Emulator if available
    $storageEmulatorPath = "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
    if (Test-Path $storageEmulatorPath) {
        Write-Host "Starting Azure Storage Emulator..." -ForegroundColor Yellow
        try {
            Start-Process -FilePath $storageEmulatorPath -ArgumentList "start" -NoNewWindow -Wait
            Write-Host "✓ Azure Storage Emulator started" -ForegroundColor Green
        } catch {
            Write-Warning "Failed to start Azure Storage Emulator: $_"
        }
    } else {
        Write-Host "ℹ Azure Storage Emulator not found. Skipping..." -ForegroundColor Yellow
    }

    # 3. Install Global Packages
    Write-Section -Title "3. INSTALLING GLOBAL PACKAGES"
    
    # Install npm packages
    Write-Host "Installing npm packages..." -ForegroundColor Yellow
    try {
        npm install -g @azure/storage-blob @azure/identity --loglevel=error
        Write-Host "✓ npm packages installed successfully" -ForegroundColor Green
    } catch {
        Write-Warning "Failed to install npm packages: $_"
    }

    # Install .NET tools
    Write-Host "Installing .NET tools..." -ForegroundColor Yellow
    try {
        dotnet tool install --global dotnet-ef --version 8.0.0 --ignore-failed-sources
        Write-Host "✓ .NET tools installed successfully" -ForegroundColor Green
    } catch {
        Write-Warning "Failed to install .NET tools: $_"
    }

    # 4. Setup Environment
    Write-Section -Title "4. SETTING UP ENVIRONMENT"
    
    # Restore NuGet packages
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    try {
        dotnet restore
        Write-Host "✓ NuGet packages restored successfully" -ForegroundColor Green
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

# Database Connection
CONNECTION_STRING="Server=(localdb)\\mssqllocaldb;Database=Emmadev;Trusted_Connection=True;MultipleActiveResultSets=true"

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000;https://localhost:5001
"@ | Out-File -FilePath $envFile -Encoding utf8
        Write-Host "Created .env file at $envFile" -ForegroundColor Green
        Write-Host "  Please update it with your configuration" -ForegroundColor Yellow
    } else {
        Write-Host "✓ .env file already exists at $envFile" -ForegroundColor Green
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
            Write-Host "✓ Created test configuration at $testConfigPath" -ForegroundColor Green
            Write-Host "  Please update it with your test settings" -ForegroundColor Yellow
        } catch {
            Write-Warning "Failed to create test configuration: $_"
        }
    } elseif (Test-Path $testConfigPath) {
        Write-Host "✓ Test configuration already exists at $testConfigPath" -ForegroundColor Green
    }

    # 5. Final Steps
    Write-Section -Title "SETUP COMPLETED SUCCESSFULLY!"
    
    Write-Host "`nNEXT STEPS:" -ForegroundColor Cyan -BackgroundColor DarkBlue
    Write-Host "1. Update the .env file with your Azure OpenAI and database settings"
    Write-Host "2. Run 'dotnet build' to build the solution"
    Write-Host "3. Run 'dotnet test' to run all tests"
    Write-Host "4. To start the application:"
    Write-Host "   - Set your working directory to the API project"
    Write-Host "   - Run 'dotnet run'"
    Write-Host "   - Or press F5 in Visual Studio/VS Code"
    Write-Host "`nFor Azure OpenAI integration tests, ensure you have set up the required environment variables." -ForegroundColor Yellow
    Write-Host "`nLog file: $logFile" -ForegroundColor Gray
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
