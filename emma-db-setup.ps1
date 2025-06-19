# Emma AI Platform - PostgreSQL Database Setup Script
# -------------------------------------------
# This script helps set up and manage the PostgreSQL database for the Emma AI Platform

# Add PostgreSQL bin directory to PATH
$env:Path = "C:\Program Files\PostgreSQL\16\bin;" + $env:Path

# Connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgSystemDB = "postgres"    # System database
$pgTargetDB = "emma"        # Emma AI Platform database
$pgUser = "emmaadmin"       # Username without server suffix
$pgPassword = "GOGdb54321"  # Password from .env file

# Set password as environment variable
$env:PGPASSWORD = $pgPassword

# Header
Write-Host "Emma AI Platform - PostgreSQL Database Management" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan

# Verify PostgreSQL client
try {
    $pgVersion = & psql --version
    Write-Host "✅ PostgreSQL client: $pgVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ PostgreSQL client not found or not working" -ForegroundColor Red
    Write-Host "   Error: $_" -ForegroundColor Red
    exit 1
}

# Main Menu Function
function Show-Menu {
    Write-Host "`nEmma AI Platform Database Operations:" -ForegroundColor Yellow
    Write-Host "1. Test Connection" -ForegroundColor White
    Write-Host "2. List Tables" -ForegroundColor White
    Write-Host "3. Create Test Table" -ForegroundColor White
    Write-Host "4. Check Database Existence" -ForegroundColor White
    Write-Host "5. Run SQL Command" -ForegroundColor White
    Write-Host "6. Update Connection String" -ForegroundColor White
    Write-Host "Q. Quit" -ForegroundColor White
    
    $choice = Read-Host "`nSelect an option"
    return $choice
}

# Test Connection Function
function Test-Connection {
    Write-Host "`nTesting connection to $pgServer..." -ForegroundColor Yellow
    
    try {
        $result = & psql -h $pgServer -p $pgPort -d $pgSystemDB -U $pgUser -c "SELECT 'Connection successful!' AS status;"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Connection successful!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "❌ Connection failed with exit code: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "❌ Connection error: $_" -ForegroundColor Red
        return $false
    }
}

# List Tables Function
function List-Tables {
    param(
        [string]$database = $pgTargetDB
    )
    
    Write-Host "`nListing tables in $database database..." -ForegroundColor Yellow
    
    try {
        $result = & psql -h $pgServer -p $pgPort -d $database -U $pgUser -c "SELECT table_schema, table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_schema, table_name;"
        
        if ($LASTEXITCODE -eq 0) {
            if ($result -match "0 rows") {
                Write-Host "No tables found in the 'public' schema." -ForegroundColor Yellow
            } else {
                Write-Host "✅ Tables retrieved successfully!" -ForegroundColor Green
            }
        } else {
            Write-Host "❌ Error listing tables with exit code: $LASTEXITCODE" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error listing tables: $_" -ForegroundColor Red
    }
}

# Create Test Table Function
function Create-TestTable {
    Write-Host "`nCreating test table in $pgTargetDB database..." -ForegroundColor Yellow
    
    $createTableSQL = @"
CREATE TABLE IF NOT EXISTS emma_test_entities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert some test data
INSERT INTO emma_test_entities (name, description)
VALUES 
    ('Test Entity 1', 'This is a test entity for the Emma AI Platform'),
    ('Test Entity 2', 'Another test entity with different values')
ON CONFLICT (id) DO NOTHING;

-- Show the created data
SELECT * FROM emma_test_entities;
"@
    
    try {
        $result = & psql -h $pgServer -p $pgPort -d $pgTargetDB -U $pgUser -c $createTableSQL
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Test table created successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Error creating test table with exit code: $LASTEXITCODE" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error creating test table: $_" -ForegroundColor Red
    }
}

# Check Database Function
function Check-Database {
    Write-Host "`nChecking if $pgTargetDB database exists..." -ForegroundColor Yellow
    
    try {
        $result = & psql -h $pgServer -p $pgPort -d $pgSystemDB -U $pgUser -t -c "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = '$pgTargetDB');"
        
        if ($result -match "t") {
            Write-Host "✅ $pgTargetDB database exists!" -ForegroundColor Green
            return $true
        } else {
            Write-Host "❌ $pgTargetDB database does NOT exist." -ForegroundColor Red
            
            $createChoice = Read-Host "Would you like to create it? (Y/N)"
            if ($createChoice -eq "Y" -or $createChoice -eq "y") {
                Write-Host "Creating $pgTargetDB database..." -ForegroundColor Yellow
                $createResult = & psql -h $pgServer -p $pgPort -d $pgSystemDB -U $pgUser -c "CREATE DATABASE $pgTargetDB;"
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ $pgTargetDB database created successfully!" -ForegroundColor Green
                    return $true
                } else {
                    Write-Host "❌ Error creating database with exit code: $LASTEXITCODE" -ForegroundColor Red
                    return $false
                }
            }
            return $false
        }
    } catch {
        Write-Host "❌ Error checking database: $_" -ForegroundColor Red
        return $false
    }
}

# Run SQL Command Function
function Run-SqlCommand {
    Write-Host "`nEnter SQL command to execute on $pgTargetDB database:" -ForegroundColor Yellow
    Write-Host "(Type 'exit' to cancel)" -ForegroundColor Yellow
    $sqlCommand = Read-Host
    
    if ($sqlCommand -eq "exit") {
        return
    }
    
    try {
        Write-Host "Executing SQL command..." -ForegroundColor Yellow
        $result = & psql -h $pgServer -p $pgPort -d $pgTargetDB -U $pgUser -c $sqlCommand
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ SQL command executed successfully!" -ForegroundColor Green
        } else {
            Write-Host "❌ Error executing SQL command with exit code: $LASTEXITCODE" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error executing SQL command: $_" -ForegroundColor Red
    }
}

# Update Connection String
function Update-ConnectionString {
    Write-Host "`nCurrent Connection String in .env file:" -ForegroundColor Yellow
    
    $envPath = Join-Path $PSScriptRoot ".env"
    if (Test-Path $envPath) {
        $envContent = Get-Content $envPath
        $connectionStringLine = $envContent | Where-Object { $_ -like "ConnectionStrings__PostgreSql=*" }
        
        if ($connectionStringLine) {
            Write-Host $connectionStringLine -ForegroundColor White
            
            Write-Host "`nRecommended Connection String:" -ForegroundColor Green
            $recommendedString = "ConnectionStrings__PostgreSql=Host=$pgServer;Port=$pgPort;Database=$pgTargetDB;Username=$pgUser;Password=$pgPassword;SslMode=Require"
            Write-Host $recommendedString -ForegroundColor White
            
            $updateChoice = Read-Host "`nUpdate connection string in .env file? (Y/N)"
            if ($updateChoice -eq "Y" -or $updateChoice -eq "y") {
                $newEnvContent = $envContent -replace [regex]::Escape($connectionStringLine), $recommendedString
                $newEnvContent | Out-File -FilePath $envPath -Encoding utf8
                Write-Host "✅ Connection string updated successfully!" -ForegroundColor Green
            }
        } else {
            Write-Host "❌ Could not find PostgreSQL connection string in .env file." -ForegroundColor Red
        }
    } else {
        Write-Host "❌ .env file not found at $envPath" -ForegroundColor Red
    }
}

# Main loop
$exitRequested = $false

do {
    $choice = Show-Menu
    
    switch ($choice) {
        "1" { Test-Connection }
        "2" { List-Tables }
        "3" { Create-TestTable }
        "4" { Check-Database }
        "5" { Run-SqlCommand }
        "6" { Update-ConnectionString }
        "Q" { $exitRequested = $true }
        "q" { $exitRequested = $true }
        default { Write-Host "Invalid option. Please try again." -ForegroundColor Red }
    }
    
    if (-not $exitRequested) {
        Write-Host "`nPress Enter to continue..." -ForegroundColor Cyan
        Read-Host
    }
} while (-not $exitRequested)

# Clean up
Write-Host "`nCleaning up environment variables..." -ForegroundColor Yellow
$env:PGPASSWORD = $null

Write-Host "Emma AI Platform database management completed." -ForegroundColor Cyan
