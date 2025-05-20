# Local Development Run Script

# Check if required services are running
Write-Host "Checking development services..."

# Check Azure Storage Emulator
$storageStatus = & "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" status
if ($storageStatus -notmatch "Status: Running") {
    Write-Host "Starting Azure Storage Emulator..."
    & "C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start
}

# Check Cosmos DB Emulator
$cosmosStatus = & "C:\Program Files\Azure Cosmos DB Emulator\Microsoft.Azure.Cosmos.Emulator.exe" status
if ($cosmosStatus -notmatch "Status: Running") {
    Write-Host "Starting Cosmos DB Emulator..."
    & "C:\Program Files\Azure Cosmos DB Emulator\Microsoft.Azure.Cosmos.Emulator.exe" start
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
$env:AZURE_COSMOSDB_ENDPOINT = "http://localhost:8081"
$env:AZURE_COSMOSDB_KEY = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
$env:DEBUG = "true"

dotnet run --project Emma.Api/Emma.Api.csproj
