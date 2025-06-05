# Emma AI Platform - Local PostgreSQL Starter
# This script starts the locally installed PostgreSQL service and creates the required database if needed

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Start-PostgreSqlService {
    Write-ColorMessage "Looking for PostgreSQL Windows service..." -ForegroundColor Yellow
    
    # Search for PostgreSQL service with wildcard pattern
    $pgServices = Get-Service | Where-Object { $_.Name -like "postgresql*" -or $_.DisplayName -like "*postgresql*" }
    
    if ($pgServices -and $pgServices.Count -gt 0) {
        foreach ($pgService in $pgServices) {
            Write-ColorMessage "Found PostgreSQL service: $($pgService.DisplayName)" -ForegroundColor Green
            
            if ($pgService.Status -eq "Running") {
                Write-ColorMessage "✅ PostgreSQL service is already running" -ForegroundColor Green
            }
            else {
                try {
                    Write-ColorMessage "Starting PostgreSQL service..." -ForegroundColor Yellow
                    Start-Service -Name $pgService.Name
                    Write-ColorMessage "✅ PostgreSQL service started successfully" -ForegroundColor Green
                }
                catch {
                    Write-ColorMessage "❌ Failed to start PostgreSQL service: $_" -ForegroundColor Red
                    return $false
                }
            }
            return $true
        }
    }
    
    Write-ColorMessage "❌ No PostgreSQL service found. Is PostgreSQL installed as a Windows service?" -ForegroundColor Red
    Write-ColorMessage "Try running PostgreSQL using pgAdmin or the pg_ctl command if it's installed differently." -ForegroundColor Yellow
    return $false
}

function Create-EmmaDatabase {
    Write-ColorMessage "Checking for existing 'emma' database..." -ForegroundColor Yellow
    
    # Try to connect to the 'postgres' database first (default admin database)
    try {
        $dbExists = (& psql -U postgres -t -c "SELECT 1 FROM pg_database WHERE datname='emma'") -match "1"
        
        if ($dbExists) {
            Write-ColorMessage "✅ Database 'emma' already exists" -ForegroundColor Green
        }
        else {
            Write-ColorMessage "Creating 'emma' database..." -ForegroundColor Yellow
            & psql -U postgres -c "CREATE DATABASE emma;"
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "✅ Database 'emma' created successfully" -ForegroundColor Green
            }
            else {
                Write-ColorMessage "❌ Failed to create 'emma' database" -ForegroundColor Red
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-ColorMessage "❌ Error connecting to PostgreSQL: $_" -ForegroundColor Red
        Write-ColorMessage "Make sure psql is in your PATH and PostgreSQL is running" -ForegroundColor Yellow
        return $false
    }
}

function Update-ConnectionString {
    # Update the connection string in .env to use localhost instead of Docker service name
    $envFile = ".\.env"
    $content = Get-Content -Path $envFile -Raw
    
    # Look for the ConnectionStrings__PostgreSql line with Docker settings
    if ($content -match "ConnectionStrings__PostgreSql=Host=postgres;") {
        Write-ColorMessage "Updating PostgreSQL connection string to use localhost..." -ForegroundColor Yellow
        
        $updatedContent = $content -replace "ConnectionStrings__PostgreSql=Host=postgres;", "ConnectionStrings__PostgreSql=Host=localhost;"
        Set-Content -Path $envFile -Value $updatedContent
        
        Write-ColorMessage "✅ Updated connection string in .env file" -ForegroundColor Green
    }
}

function Test-PsqlAvailable {
    try {
        $null = Get-Command psql -ErrorAction Stop
        return $true
    }
    catch {
        Write-ColorMessage "❌ psql command not found in PATH" -ForegroundColor Red
        Write-ColorMessage "Please add PostgreSQL bin directory to your PATH" -ForegroundColor Yellow
        Write-ColorMessage "Typically: C:\Program Files\PostgreSQL\15\bin" -ForegroundColor Yellow
        return $false
    }
}

# Main script
Write-ColorMessage "Emma AI Platform - Local PostgreSQL Starter" -ForegroundColor Cyan
Write-ColorMessage "================================================" -ForegroundColor Cyan

# Check if psql is available
if (-not (Test-PsqlAvailable)) {
    exit 1
}

# Start PostgreSQL service
$serviceStarted = Start-PostgreSqlService
if (-not $serviceStarted) {
    Write-ColorMessage "Could not start PostgreSQL service. Please start it manually." -ForegroundColor Red
    exit 1
}

# Create Emma database if needed
$dbCreated = Create-EmmaDatabase
if (-not $dbCreated) {
    exit 1
}

# Update connection string in .env
Update-ConnectionString

Write-ColorMessage "`nPostgreSQL is now ready for the Emma AI Platform!" -ForegroundColor Green
Write-ColorMessage "Database: emma" -ForegroundColor Cyan
Write-ColorMessage "Username: postgres" -ForegroundColor Cyan
Write-ColorMessage "Port: 5432" -ForegroundColor Cyan
Write-ColorMessage "Next: Run .\dev-setup.ps1 to apply migrations and complete setup" -ForegroundColor Yellow
