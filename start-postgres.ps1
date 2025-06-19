# Emma AI Platform - PostgreSQL Starter Script
# This script starts PostgreSQL and creates the required database if needed

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Test-CommandExists {
    param([string]$Command)
    return ($null -ne (Get-Command $Command -ErrorAction SilentlyContinue))
}

function Start-PostgreSqlService {
    # Try to find PostgreSQL service
    $pgService = Get-Service | Where-Object { $_.Name -like "postgresql*" -or $_.DisplayName -like "*postgresql*" } | Select-Object -First 1
    
    if ($pgService) {
        Write-ColorMessage "Found PostgreSQL service: $($pgService.DisplayName)" -ForegroundColor Yellow
        
        if ($pgService.Status -eq "Running") {
            Write-ColorMessage "✅ PostgreSQL service is already running" -ForegroundColor Green
            return $true
        }
        
        try {
            Write-ColorMessage "Starting PostgreSQL service..." -ForegroundColor Yellow
            Start-Service -Name $pgService.Name
            Write-ColorMessage "✅ PostgreSQL service started successfully" -ForegroundColor Green
            return $true
        }
        catch {
            Write-ColorMessage "❌ Failed to start PostgreSQL service: $_" -ForegroundColor Red
            return $false
        }
    }
    else {
        Write-ColorMessage "No PostgreSQL service found, will try alternative methods" -ForegroundColor Yellow
        return $false
    }
}

function Start-PostgreSqlManually {
    # Common PostgreSQL installation paths
    $possiblePaths = @(
        "C:\Program Files\PostgreSQL\15\bin",
        "C:\Program Files\PostgreSQL\14\bin",
        "C:\Program Files\PostgreSQL\13\bin",
        "C:\Program Files\PostgreSQL\12\bin",
        "C:\Program Files\PostgreSQL\11\bin"
    )
    
    foreach ($path in $possiblePaths) {
        $pgCtlPath = Join-Path -Path $path -ChildPath "pg_ctl.exe"
        $dataDir = Join-Path -Path $path -ChildPath "..\data" | Resolve-Path -ErrorAction SilentlyContinue
        
        if (Test-Path $pgCtlPath) {
            Write-ColorMessage "Found PostgreSQL at: $path" -ForegroundColor Yellow
            
            try {
                Write-ColorMessage "Starting PostgreSQL server manually..." -ForegroundColor Yellow
                & $pgCtlPath -D "$dataDir" start
                Start-Sleep -Seconds 2  # Give it a moment to start
                Write-ColorMessage "✅ PostgreSQL server started manually" -ForegroundColor Green
                return $true
            }
            catch {
                Write-ColorMessage "❌ Failed to start PostgreSQL manually: $_" -ForegroundColor Red
            }
        }
    }
    
    Write-ColorMessage "❌ Could not find PostgreSQL installation to start manually" -ForegroundColor Red
    return $false
}

function Test-PostgreSqlConnection {
    if (Test-CommandExists "psql") {
        try {
            $output = & psql -U postgres -c "SELECT 1" 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "✅ Successfully connected to PostgreSQL" -ForegroundColor Green
                return $true
            }
        }
        catch {
            # Failed to connect
        }
        
        Write-ColorMessage "❌ Failed to connect to PostgreSQL" -ForegroundColor Red
        return $false
    }
    else {
        Write-ColorMessage "❌ psql command not found. Please ensure PostgreSQL is installed and in your PATH" -ForegroundColor Red
        return $false
    }
}

function New-EmmaDatabase {
    if (-not (Test-CommandExists "psql")) {
        Write-ColorMessage "❌ psql command not found. Cannot create database." -ForegroundColor Red
        return
    }
    
    Write-ColorMessage "Checking if Emma database exists..." -ForegroundColor Yellow
    
    # Check if emma database exists
    $databaseExists = $false
    try {
        $output = & psql -U postgres -lqt | Select-String "^ emma "
        $databaseExists = $output.Count -gt 0
    }
    catch {
        Write-ColorMessage "Error checking for database: $_" -ForegroundColor Red
    }
    
    if ($databaseExists) {
        Write-ColorMessage "✅ Emma database already exists" -ForegroundColor Green
    }
    else {
        Write-ColorMessage "Creating Emma database..." -ForegroundColor Yellow
        try {
            & psql -U postgres -c "CREATE DATABASE emma;"
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "✅ Created Emma database successfully" -ForegroundColor Green
                
                # Create schema just to be safe
                & psql -U postgres -d emma -c "CREATE SCHEMA IF NOT EXISTS public;"
                Write-ColorMessage "✅ Ensured public schema exists" -ForegroundColor Green
            }
            else {
                Write-ColorMessage "❌ Failed to create Emma database" -ForegroundColor Red
            }
        }
        catch {
            Write-ColorMessage "❌ Error creating database: $_" -ForegroundColor Red
        }
    }
}

# Main script execution
Write-ColorMessage "Emma AI Platform - PostgreSQL Starter" -ForegroundColor Cyan

$success = $false

# Try to start PostgreSQL service first
$success = Start-PostgreSqlService

# If service start failed, try manual methods
if (-not $success) {
    $success = Start-PostgreSqlManually
}

# Verify PostgreSQL is running
if (Test-PostgreSqlConnection) {
    # Create Emma database if needed
    New-EmmaDatabase
    
    Write-ColorMessage "`nPostgreSQL is now ready for the Emma AI Platform" -ForegroundColor Green
    Write-ColorMessage "You can now run the dev-setup.ps1 script to complete your setup." -ForegroundColor Cyan
}
else {
    Write-ColorMessage "`n❌ Could not start PostgreSQL. Please:" -ForegroundColor Red
    Write-ColorMessage "  1. Ensure PostgreSQL is installed on your system" -ForegroundColor Yellow
    Write-ColorMessage "  2. Check if PostgreSQL is running in Task Manager or Services" -ForegroundColor Yellow
    Write-ColorMessage "  3. Try starting PostgreSQL manually using pgAdmin or the command line" -ForegroundColor Yellow
}
