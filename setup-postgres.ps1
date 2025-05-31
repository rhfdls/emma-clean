
# PostgreSQL Docker Setup Script

# Stop and remove existing containers if they exist
Write-Host "Stopping and removing existing PostgreSQL container..."
docker stop emma-postgres 2>$null
docker rm emma-postgres 2>$null

# Pull PostgreSQL 15 image
Write-Host "Pulling PostgreSQL 15 image..."
docker pull postgres:15

# Run PostgreSQL container with vector extension
Write-Host "Starting PostgreSQL container with vector extension..."
docker run --name emma-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15

# Wait for PostgreSQL to start
Start-Sleep -Seconds 10

# Create database and install vector extension
Write-Host "Setting up database and vector extension..."
docker exec -it emma-postgres psql -U postgres -c "CREATE DATABASE emma;"

Write-Host "PostgreSQL setup complete!"
Write-Host "Database: emma"
Write-Host "Username: postgres"
Write-Host "Password: postgres"
Write-Host "Port: 5432"
Write-Host "Vector extension installed successfully"