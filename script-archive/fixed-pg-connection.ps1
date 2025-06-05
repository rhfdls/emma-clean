# Fixed PostgreSQL Connection Script for Emma AI Platform
# Based on successful Azure Data Studio connection insights
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Clear any existing PostgreSQL environment variables
$env:PGPASSWORD = $null
$env:PGHOST = $null
$env:PGUSER = $null
$env:PGSSLMODE = $null
$env:PGSSLROOTCERT = $null

# Set connection parameters based on successful Azure Data Studio connection
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgSystemDB = "postgres"    # Start with postgres system database
$pgTargetDB = "emma"        # Our target database
$pgUser = "emmaadmin"       # Just username without server suffix
$pgPassword = "GOGdb54321" # Password from .env file - updated

# Set password for authentication
$env:PGPASSWORD = $pgPassword

Write-Host "Emma AI Platform PostgreSQL Connection Test"
Write-Host "=============================================="
Write-Host "Server: $pgServer"
Write-Host "System Database: $pgSystemDB"
Write-Host "Target Database: $pgTargetDB" 
Write-Host "User: $pgUser"
Write-Host "SSL Mode: require (no certificate path needed)"

# Test 1: Connect to postgres system database
Write-Host "`n[Test 1] Connecting to postgres system database..."
& psql -h $pgServer -p $pgPort -d $pgSystemDB -U $pgUser -c "SELECT current_database(), current_user, version();"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Connection to postgres database successful!"
    
    # Test 2: Check if emma database exists
    Write-Host "`n[Test 2] Checking if emma database exists..."
    $emmaExists = & psql -h $pgServer -p $pgPort -d $pgSystemDB -U $pgUser -t -c "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = '$pgTargetDB');"
    
    if ($emmaExists -match "t") {
        Write-Host "`n✅ Emma database exists. Connecting to it..."
        
        # Test 3: Connect to emma database
        & psql -h $pgServer -p $pgPort -d $pgTargetDB -U $pgUser -c "SELECT current_database(), current_user;"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✅ Connection to emma database successful!"
            
            # Test 4: Create test table in emma database
            Write-Host "`n[Test 4] Creating test table in emma database..."
            $createTableSQL = @"
CREATE TABLE IF NOT EXISTS test_entities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO test_entities (name) VALUES ('Test Entity 1');
SELECT * FROM test_entities;
"@
            
            $createTableSQL | Out-File -FilePath "create_test_table.sql" -Encoding utf8
            & psql -h $pgServer -p $pgPort -d $pgTargetDB -U $pgUser -f "create_test_table.sql"
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "`n✅ Test table created/updated successfully in emma database!"
            } else {
                Write-Host "`n❌ Error creating test table. Exit code: $LASTEXITCODE"
            }
        } else {
            Write-Host "`n❌ Failed to connect to emma database. Exit code: $LASTEXITCODE"
        }
    } else {
        Write-Host "`n⚠️ Emma database does not exist. Creating it now..."
        
        # Create emma database
        & psql -h $pgServer -p $pgPort -d $pgSystemDB -U $pgUser -c "CREATE DATABASE $pgTargetDB;"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n✅ Emma database created successfully!"
            
            # Connect to new emma database
            & psql -h $pgServer -p $pgPort -d $pgTargetDB -U $pgUser -c "SELECT current_database(), current_user;"
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "`n✅ Connection to new emma database successful!"
                
                # Create test table in new emma database
                Write-Host "`n[Test 4] Creating test table in new emma database..."
                $createTableSQL = @"
CREATE TABLE IF NOT EXISTS test_entities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO test_entities (name) VALUES ('Test Entity 1');
SELECT * FROM test_entities;
"@
                
                $createTableSQL | Out-File -FilePath "create_test_table.sql" -Encoding utf8
                & psql -h $pgServer -p $pgPort -d $pgTargetDB -U $pgUser -f "create_test_table.sql"
                
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "`n✅ Test table created successfully in new emma database!"
                } else {
                    Write-Host "`n❌ Error creating test table. Exit code: $LASTEXITCODE"
                }
            } else {
                Write-Host "`n❌ Failed to connect to new emma database. Exit code: $LASTEXITCODE"
            }
        } else {
            Write-Host "`n❌ Failed to create emma database. Exit code: $LASTEXITCODE"
        }
    }
} else {
    Write-Host "`n❌ Failed to connect to postgres database. Exit code: $LASTEXITCODE"
    Write-Host "Double-check your connection parameters and Azure PostgreSQL firewall rules."
}

# Update .env file with working connection string
Write-Host "`n[Final Step] Updating .env connection string recommendation:"
Write-Host "ConnectionStrings__PostgreSql=Host=$pgServer;Port=$pgPort;Database=$pgTargetDB;Username=$pgUser;Password=$pgPassword;SslMode=Require"

# Clean up environment variables
$env:PGPASSWORD = $null
