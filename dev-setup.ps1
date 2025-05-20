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

# Install Azure Cosmos DB Emulator
Write-Host "Installing Azure Cosmos DB Emulator..."
if (-not (Test-Path "C:\Program Files\Azure Cosmos DB Emulator\Microsoft.Azure.Cosmos.Emulator.exe")) {
    Invoke-WebRequest -Uri "https://aka.ms/cosmosdb-emulator" -OutFile "cosmosdb-emulator.msi"
    Start-Process msiexec.exe -Wait -ArgumentList "/I cosmosdb-emulator.msi /quiet"
    Remove-Item "cosmosdb-emulator.msi"
}

# Start services
Write-Host "Starting development services..."

# Start Azure Storage Emulator
Start-Process "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" -ArgumentList "start"

# Start Cosmos DB Emulator
Start-Process "C:\Program Files\Azure Cosmos DB Emulator\Microsoft.Azure.Cosmos.Emulator.exe" -ArgumentList "/NoFirewall /NoUI /NoExplorer"

# Install .NET SDK
Write-Host "Installing .NET SDK..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Invoke-WebRequest -Uri "https://dotnet.microsoft.com/download/dotnet/thank-you/sdk-8.0-windows-x64-installer" -OutFile "dotnet-sdk.exe"
    Start-Process dotnet-sdk.exe -Wait -ArgumentList "/quiet /norestart"
    Remove-Item "dotnet-sdk.exe"
}

# Install Node.js
Write-Host "Installing Node.js..."
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Invoke-WebRequest -Uri "https://nodejs.org/dist/v20.x/node-v20.x.x-x64.msi" -OutFile "node.msi"
    Start-Process msiexec.exe -Wait -ArgumentList "/I node.msi /quiet"
    Remove-Item "node.msi"
}

# Install required npm packages
Write-Host "Installing npm packages..."
Set-Location "c:\Users\david\GitHub\WindsurfProjects\emma"
npm install -g @azure/storage-blob @azure/cosmos @azure/identity

Write-Host "Development environment setup complete!" -ForegroundColor Green
Write-Host "To run the application locally:" -ForegroundColor Green
Write-Host "1. Copy the local.env file to .env" -ForegroundColor Green
Write-Host "2. Update the API keys in .env with your local development keys" -ForegroundColor Green
Write-Host "3. Run 'dotnet run' to start the application" -ForegroundColor Green
