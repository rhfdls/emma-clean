# Emma AI Platform - Database Connection Debugging Script
# This script tests each step of the database connection process with verbose output

# Connection parameters
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = "5432"
$pgDatabase = "emma"
$pgUser = "emmaadmin"
$pgPassword = "GOGdb54321"

# Function to log each step with timestamps
function Write-Step {
    param([string]$message)
    $timestamp = Get-Date -Format "HH:mm:ss.fff"
    Write-Host "[$timestamp] $message" -ForegroundColor Cyan
}

# Script execution starts
Log-Step "Emma AI Platform - Database Connection Debugging Started"
Log-Step "Using connection parameters:"
Write-Host "  Server: $pgServer"
Write-Host "  Database: $pgDatabase"
Write-Host "  User: $pgUser"
Write-Host "  SSL Mode: require"

# Step 1: Check if psql is available
Log-Step "STEP 1: Checking if PostgreSQL client (psql) is available..."
try {
    $pgVersion = & psql --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PostgreSQL client is available: $pgVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ PostgreSQL client check failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host $pgVersion -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Error checking PostgreSQL client: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Set environment variables and test simple command
Log-Step "STEP 2: Setting environment variables..."
$env:PGPASSWORD = $pgPassword
Write-Host "✅ Environment variables set" -ForegroundColor Green

# Step 3: Test simple database connection
Log-Step "STEP 3: Testing basic database connection with timeout..."
$connectionTestCmd = "psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -c `"SELECT 1 AS connection_test;`""
Write-Host "Running command: $connectionTestCmd"

# Use Start-Process with timeout to prevent hanging
$job = Start-Job -ScriptBlock { 
    param($cmd)
    Invoke-Expression $cmd
} -ArgumentList $connectionTestCmd

# Wait for the job with timeout
$timeout = 10 # seconds
$completed = Wait-Job -Job $job -Timeout $timeout
if ($completed -eq $null) {
    Write-Host "❌ Connection test TIMED OUT after $timeout seconds - command hung" -ForegroundColor Red
    Stop-Job -Job $job
    Remove-Job -Job $job -Force
} else {
    $result = Receive-Job -Job $job
    Remove-Job -Job $job -Force
    
    Write-Host "Connection test output:" -ForegroundColor Yellow
    Write-Host $result
    
    if ($result -match "connection_test") {
        Write-Host "✅ Connection test successful!" -ForegroundColor Green
    } else {
        Write-Host "❌ Connection test failed - output doesn't contain expected result" -ForegroundColor Red
    }
}

# Step 4: Test listing tables with timeout
Log-Step "STEP 4: Testing table listing with timeout..."
$listTablesCmd = "psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -c `"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' LIMIT 5;`""
Write-Host "Running command: $listTablesCmd"

$job = Start-Job -ScriptBlock { 
    param($cmd)
    Invoke-Expression $cmd
} -ArgumentList $listTablesCmd

$timeout = 10 # seconds
$completed = Wait-Job -Job $job -Timeout $timeout
if ($completed -eq $null) {
    Write-Host "❌ Table listing TIMED OUT after $timeout seconds - command hung" -ForegroundColor Red
    Stop-Job -Job $job
    Remove-Job -Job $job -Force
} else {
    $result = Receive-Job -Job $job
    Remove-Job -Job $job -Force
    
    Write-Host "Table listing output:" -ForegroundColor Yellow
    Write-Host $result
}

# Step 5: Try creating a simple table with timeout
Log-Step "STEP 5: Testing table creation with timeout..."
$createTableSQL = @"
CREATE TABLE IF NOT EXISTS debug_test (
    id SERIAL PRIMARY KEY,
    test_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO debug_test (test_name) VALUES ('Connection test $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")');
SELECT * FROM debug_test LIMIT 5;
"@

$createTableSQL | Out-File -FilePath "debug_test_table.sql" -Encoding utf8
Write-Host "Created SQL file: debug_test_table.sql" -ForegroundColor Green

$createTableCmd = "psql -h $pgServer -p $pgPort -d $pgDatabase -U $pgUser -f debug_test_table.sql"
Write-Host "Running command: $createTableCmd"

$job = Start-Job -ScriptBlock { 
    param($cmd)
    Invoke-Expression $cmd
} -ArgumentList $createTableCmd

$timeout = 15 # seconds
$completed = Wait-Job -Job $job -Timeout $timeout
if ($completed -eq $null) {
    Write-Host "❌ Table creation TIMED OUT after $timeout seconds - command hung" -ForegroundColor Red
    Stop-Job -Job $job
    Remove-Job -Job $job -Force
} else {
    $result = Receive-Job -Job $job
    Remove-Job -Job $job -Force
    
    Write-Host "Table creation output:" -ForegroundColor Yellow
    Write-Host $result
}

# Clean up
Log-Step "Cleaning up environment variables..."
$env:PGPASSWORD = $null

Log-Step "Database connection debugging completed"
Write-Host "`nTroubleshooting Summary:" -ForegroundColor Yellow
Write-Host "- If steps timed out: Check network connectivity, firewall rules, and server status" -ForegroundColor White
Write-Host "- If authentication failed: Verify username/password and server permissions" -ForegroundColor White
Write-Host "- If table operations failed: Check database permissions and schema setup" -ForegroundColor White
Write-Host "`nRecommended connection string for the Emma AI Platform:" -ForegroundColor Green
Write-Host "ConnectionStrings__PostgreSql=Host=$pgServer;Port=$pgPort;Database=$pgDatabase;Username=$pgUser;Password=$pgPassword;SslMode=Require" -ForegroundColor White
