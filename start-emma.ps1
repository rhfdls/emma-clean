# PowerShell script to start PostgreSQL via Docker Compose and run the Emma.Api backend

# Start PostgreSQL container
docker-compose up -d

# Wait for the database to be ready
Start-Sleep -Seconds 10

# Build the Emma.Api project
dotnet build .\Emma.Api\Emma.Api.csproj

# Run the Emma.Api project
dotnet run --project .\Emma.Api\Emma.Api.csproj
