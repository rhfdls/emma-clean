# Local Development Run Script

# Check if required services are running
Write-Host "Checking development services..."

# Check Azure Storage Emulator
$storageStatus = & "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" status
if ($storageStatus -notmatch "Status: Running") {
    Write-Host "Starting Azure Storage Emulator..."
    & "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start
}

# Copy environment file
if (-not (Test-Path ".env")) {
    Write-Host "Copying local environment file..."
    Copy-Item "local.env" ".env"
}

# Build and run the application
Write-Host "Building and running Emma..."
dotnet build Emma.Api/Emma.Api.csproj -c Debug

# Start the application with environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:AZURE_STORAGE_CONNECTION_STRING = "UseDevelopmentStorage=true"
$env:DEBUG = "true"

dotnet run --project Emma.Api/Emma.Api.csproj
