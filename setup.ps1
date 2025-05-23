# Ensure Docker is running
if (-not (docker info 2>&1 | findstr "Server Version")) {
    Write-Error "Docker is not running. Please start Docker and try again."
    exit 1
}

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__PostgreSql = "Server=postgres;Port=5432;Database=emma;User Id=postgres;Password=postgres;"

# Stop and remove any existing containers
Write-Host "Stopping and removing existing containers..." -ForegroundColor Cyan
docker-compose down --remove-orphans

# Build the solution
Write-Host "`nBuilding the solution..." -ForegroundColor Cyan
dotnet build

# Start PostgreSQL
Write-Host "`nStarting PostgreSQL..." -ForegroundColor Cyan
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
$maxRetries = 10
$retryCount = 0

Write-Host "`nWaiting for PostgreSQL to be ready..." -ForegroundColor Cyan
do {
    try {
        docker-compose exec -T postgres pg_isready -U postgres | Out-Null
        if ($LASTEXITCODE -eq 0) {
            break
        }
    } catch {
        # Ignore errors and retry
    }
    
    $retryCount++
    if ($retryCount -ge $maxRetries) {
        Write-Error "Timed out waiting for PostgreSQL to be ready"
        exit 1
    }
    
    Write-Host "." -NoNewline -ForegroundColor Yellow
    Start-Sleep -Seconds 2
} while ($true)

Write-Host "`nPostgreSQL is ready!" -ForegroundColor Green

# Apply migrations if needed
Write-Host "`nChecking for database migrations..." -ForegroundColor Cyan
try {
    dotnet ef database update --project "Emma.Data" --startup-project "Emma.Api" --connection $env:ConnectionStrings__PostgreSql
    Write-Host "Database migrations applied successfully" -ForegroundColor Green
} catch {
    Write-Warning "Failed to apply database migrations: $_"
    Write-Warning "Continuing setup, but some features may not work correctly"
}

# Build and start all services
Write-Host "`nBuilding and starting all services..." -ForegroundColor Cyan
docker-compose up -d --build

# Wait for services to be ready
Write-Host "`nWaiting for services to start..." -ForegroundColor Cyan
Start-Sleep -Seconds 5

# Display status
Write-Host "`n[SUCCESS] Setup complete!" -ForegroundColor Green
Write-Host "[FRONTEND] http://localhost:3000" -ForegroundColor Cyan
Write-Host "[API]      http://localhost:5262" -ForegroundColor Cyan
Write-Host "[SWAGGER]  http://localhost:5262/swagger" -ForegroundColor Cyan
Write-Host "[POSTGRES] localhost:5432 (user: postgres, db: emma)" -ForegroundColor Cyan

Write-Host "`nUseful commands:" -ForegroundColor Yellow
Write-Host "  View logs:        docker-compose logs -f" -ForegroundColor Yellow
Write-Host "  Stop services:    docker-compose down" -ForegroundColor Yellow
Write-Host "  Rebuild/restart:  docker-compose up -d --build" -ForegroundColor Yellow
