# Emma AI Platform - Database Schema Setup Script
# This script will set up the necessary database schema using EF Core migrations

# Ensure PostgreSQL client is in path
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "emma"
$pgUser = "emmaadmin"
$pgPassword = "GOGdb54321"

# Set connection string for EF Core
$connectionString = "Host=$pgServer;Port=$pgPort;Database=$pgDatabase;Username=$pgUser;Password=$pgPassword;SslMode=Require"
$env:ConnectionStrings__PostgreSql = $connectionString

# Check database connection and schema status
Write-Host "Emma AI Platform - Database Schema Setup" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Checking database connection..." -ForegroundColor Yellow

# Set password securely as environment variable for psql
$env:PGPASSWORD = $pgPassword

# Check if we can connect to the database
$connectionTest = & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -c "\conninfo" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Database connection successful!" -ForegroundColor Green
    
    # Check for existing tables
    Write-Host "`nChecking for existing tables..." -ForegroundColor Yellow
    $tableCount = & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'" 2>&1
    
    if ($tableCount -match "(\d+)") {
        $count = [int]$matches[1]
        if ($count -gt 0) {
            Write-Host "✅ Found $count existing tables in the database." -ForegroundColor Green
            Write-Host "`nListing existing tables:" -ForegroundColor Yellow
            & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"
        } else {
            Write-Host "⚠️ No tables found in the database. Schema needs to be created." -ForegroundColor Yellow
        }
    }
    
    # Check migration options
    Write-Host "`nDatabase schema setup options:" -ForegroundColor Cyan
    Write-Host "1. Run Entity Framework Core migrations" -ForegroundColor White
    Write-Host "2. Run SQL scripts directly" -ForegroundColor White
    Write-Host "3. Create a simple test table" -ForegroundColor White
    Write-Host "`nChoose an option to proceed:" -ForegroundColor Yellow
    
    switch (Read-Host) {
        "1" {
            # Run EF Core migrations
            Write-Host "`nRunning EF Core migrations..." -ForegroundColor Yellow
            
            # Check if we're in the right directory
            if (-not (Test-Path ".\Emma.Data\Emma.Data.csproj")) {
                Write-Host "⚠️ Cannot find Emma.Data project. Navigating to project directory..." -ForegroundColor Yellow
                if (Test-Path ".\Emma.Data") {
                    Set-Location ".\Emma.Data"
                    Write-Host "Changed directory to Emma.Data" -ForegroundColor Green
                } else {
                    Write-Host "❌ Emma.Data directory not found. Please run this script from the solution root directory." -ForegroundColor Red
                    exit 1
                }
            }
            
            # Run EF Core migrations
            Write-Host "Running database update command..." -ForegroundColor Yellow
            dotnet ef database update
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Entity Framework migrations completed successfully!" -ForegroundColor Green
            } else {
                Write-Host "❌ Entity Framework migrations failed with exit code $LASTEXITCODE" -ForegroundColor Red
            }
        }
        "2" {
            # Run SQL scripts directly
            Write-Host "`nRunning SQL scripts directly..." -ForegroundColor Yellow
            
            # Check for emma-db-script.sql
            if (Test-Path ".\emma-db-script.sql") {
                Write-Host "Found emma-db-script.sql, executing..." -ForegroundColor Green
                & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -f ".\emma-db-script.sql"
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ SQL script executed successfully!" -ForegroundColor Green
                } else {
                    Write-Host "❌ SQL script execution failed with exit code $LASTEXITCODE" -ForegroundColor Red
                }
            } else {
                Write-Host "❌ Could not find emma-db-script.sql in the current directory." -ForegroundColor Red
                Write-Host "Please specify the path to your SQL script:" -ForegroundColor Yellow
                $scriptPath = Read-Host
                
                if (Test-Path $scriptPath) {
                    & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -f $scriptPath
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "✅ SQL script executed successfully!" -ForegroundColor Green
                    } else {
                        Write-Host "❌ SQL script execution failed with exit code $LASTEXITCODE" -ForegroundColor Red
                    }
                } else {
                    Write-Host "❌ Script not found at path: $scriptPath" -ForegroundColor Red
                }
            }
        }
        "3" {
            # Create a simple test table
            Write-Host "`nCreating a test table..." -ForegroundColor Yellow
            
            $testTableSQL = @'
CREATE TABLE IF NOT EXISTS test_entities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insert some test data
INSERT INTO test_entities (name, description) VALUES 
    ('Test Entity 1', 'This is a test entity for the Emma AI Platform'),
    ('Test Entity 2', 'Another test entity with different values');
    
-- Display the created data
SELECT * FROM test_entities;
'@
            
            $testTableSQL | Out-File -FilePath "create_test_table.sql" -Encoding utf8
            & psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -f "create_test_table.sql"
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✅ Test table created successfully!" -ForegroundColor Green
            } else {
                Write-Host "❌ Test table creation failed with exit code $LASTEXITCODE" -ForegroundColor Red
            }
        }
        default {
            Write-Host "❌ Invalid option selected. Exiting." -ForegroundColor Red
        }
    }
} else {
    Write-Host "❌ Failed to connect to the database. Error:" -ForegroundColor Red
    Write-Host $connectionTest -ForegroundColor Red
}

# Update the .env file with the correct connection string format
Write-Host "`nRecommended connection string format for .env file:" -ForegroundColor Cyan
Write-Host "ConnectionStrings__PostgreSql=Host=$pgServer;Port=$pgPort;Database=$pgDatabase;Username=$pgUser;Password=$pgPassword;SslMode=Require" -ForegroundColor White
Write-Host "`nNote: This connection string uses 'Username=$pgUser' without the server suffix." -ForegroundColor Yellow

# Clean up environment variables
$env:PGPASSWORD = $null
$env:ConnectionStrings__PostgreSql = $null
