# Emma AI Platform - Cloud Development Setup Script
# This script sets up your development environment with all cloud resources
# No local PostgreSQL or Docker required

# Set error action preference
$ErrorActionPreference = "Stop"

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White"
    )
    Write-Host $Message -ForegroundColor $ForegroundColor
}

function Install-DotnetEfIfNeeded {
    Write-ColorMessage "Checking for dotnet-ef CLI tool..." -ForegroundColor Yellow
    try {
        $dotnetEfVersion = dotnet ef --version
        Write-ColorMessage "[SUCCESS] dotnet-ef is already installed (version: $dotnetEfVersion)" -ForegroundColor Green
    } catch {
        Write-ColorMessage "Installing dotnet-ef CLI tool..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorMessage "[SUCCESS] dotnet-ef installed successfully" -ForegroundColor Green
        } else {
            Write-ColorMessage "[ERROR] Failed to install dotnet-ef. Please install it manually." -ForegroundColor Red
            exit 1
        }
    }
}

function Test-AzureCredentials {
    Write-ColorMessage "Verifying Azure credentials..." -ForegroundColor Yellow
    
    # Load .env file
    if (Test-Path ".env") {
        Get-Content ".env" | ForEach-Object {
            if ($_ -match "^([^#][^=]*)=(.*)$") {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim()
                Set-Item -Path "env:$key" -Value $value
            }
        }
    } else {
        Write-ColorMessage "[ERROR] .env file not found!" -ForegroundColor Red
        exit 1
    }
    
    # Verify required Azure services
    $requiredCredentials = @(
        @{
            Name = "Azure Cosmos DB"
            Variables = @(
                "COSMOSDB__ACCOUNTENDPOINT",
                "COSMOSDB__ACCOUNTKEY",
                "COSMOSDB__DATABASENAME",
                "COSMOSDB__CONTAINERNAME"
            )
        },
        @{
            Name = "Azure OpenAI"
            Variables = @(
                "AzureOpenAI__ApiKey",
                "AzureOpenAI__Endpoint",
                "AzureOpenAI__DeploymentName"
            )
        },
        @{
            Name = "Azure AI Foundry"
            Variables = @(
                "AzureAIFoundry__ApiKey", 
                "AzureAIFoundry__Endpoint",
                "AzureAIFoundry__DeploymentName"
            )
        },
        @{
            Name = "Azure Storage"
            Variables = @(
                "AzureStorage__AccountName",
                "AzureStorage__AccountKey"
            )
        }
    )
    
    $missingServices = @()
    
    foreach ($service in $requiredCredentials) {
        $missingVars = @()
        foreach ($var in $service.Variables) {
            if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($var))) {
                $missingVars += $var
            }
        }
        
        if ($missingVars.Count -gt 0) {
            $missingServices += "$($service.Name) (missing: $($missingVars -join ', '))"
        }
    }
    
    # Check for Azure PostgreSQL
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")) -or 
        ![Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql").Contains("azure")) {
        $missingServices += "Azure PostgreSQL (connection string missing or not pointing to Azure)"
    }
    
    if ($missingServices.Count -gt 0) {
        Write-ColorMessage "[ERROR] Missing credentials for these services:" -ForegroundColor Red
        foreach ($service in $missingServices) {
            Write-ColorMessage "   - $service" -ForegroundColor Red
        }
        Write-ColorMessage "`nYou need to update your .env file with Azure PostgreSQL credentials" -ForegroundColor Yellow
        Write-ColorMessage "Would you like to update your .env file now? (Y/N)" -ForegroundColor Cyan
        $updateEnv = Read-Host
        
        if ($updateEnv -eq "Y" -or $updateEnv -eq "y") {
            Update-EnvFile
        } else {
            exit 1
        }
    } else {
        Write-ColorMessage "[SUCCESS] All required Azure credentials found" -ForegroundColor Green
    }
}

function Update-EnvFile {
    Write-ColorMessage "`nUpdating .env file with Azure PostgreSQL configuration" -ForegroundColor Cyan
    Write-ColorMessage "------------------------------------------------" -ForegroundColor Cyan
    
    # Get PostgreSQL details from user
    Write-ColorMessage "Enter your Azure PostgreSQL server name (e.g., emma-db-server):" -ForegroundColor Yellow
    $serverName = Read-Host
    
    Write-ColorMessage "Enter your Azure PostgreSQL admin username:" -ForegroundColor Yellow
    $username = Read-Host
    
    Write-ColorMessage "Enter your Azure PostgreSQL admin password:" -ForegroundColor Yellow
    $passwordSecure = Read-Host -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwordSecure)
    $password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    
    # Build connection string
    $pgConnString = "Host=$serverName.postgres.database.azure.com;Database=emma;Username=$username@$serverName;Password=$password;SslMode=Require"
    
    # Update the .env file
    $envContent = Get-Content ".env" -Raw
    
    # Replace the PostgreSQL connection string
    if ($envContent -match "ConnectionStrings__PostgreSql=.*") {
        $envContent = $envContent -replace "ConnectionStrings__PostgreSql=.*", "ConnectionStrings__PostgreSql=$pgConnString"
    } else {
        $envContent += "`nConnectionStrings__PostgreSql=$pgConnString"
    }
    
    # Save the updated .env file
    Set-Content -Path ".env" -Value $envContent
    
    Write-ColorMessage "[SUCCESS] .env file updated with Azure PostgreSQL connection string" -ForegroundColor Green
}

function Restore-DotnetPackages {
    Write-ColorMessage "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorMessage "[SUCCESS] NuGet packages restored successfully" -ForegroundColor Green
    } else {
        Write-ColorMessage "[ERROR] Failed to restore NuGet packages" -ForegroundColor Red
        exit 1
    }
}

function Test-PostgreSqlDatabase {
    Write-ColorMessage "Checking if Azure PostgreSQL database exists..." -ForegroundColor Yellow
    
    # Parse connection string to get database details
    $connString = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    if ([string]::IsNullOrWhiteSpace($connString)) {
        Write-ColorMessage "[ERROR] PostgreSQL connection string not found in environment variables" -ForegroundColor Red
        return $false
    }
    
    # Extract host, username, password, database name, and port
    $connString -match "Host=([^;]+)" | Out-Null
    if (-not $Matches -or -not $Matches[1]) {
        Write-ColorMessage "[ERROR] Invalid PostgreSQL connection string: Host not found" -ForegroundColor Red
        return $false
    }
    
    $pgHost = $Matches[1]
    $connString -match "Username=([^;]+)" | Out-Null
    $pgUser = $Matches[1]
    $connString -match "Password=([^;]+)" | Out-Null
    $pgPass = $Matches[1]
    $connString -match "Database=([^;]+)" | Out-Null
    $pgDbName = $Matches[1]
    
    # Check for port in connection string
    $portMatch = $connString -match "Port=([^;]+)"
    if (-not $portMatch) {
        $pgPort = "5432" # Default PostgreSQL port
    } else {
        $pgPort = $Matches[1]
    }
    
    Write-ColorMessage "Connecting to Azure PostgreSQL server: $pgHost" -ForegroundColor Yellow
    
    # Check if psql is available
    $psqlExists = $null
    try {
        $psqlExists = Get-Command psql -ErrorAction SilentlyContinue
    } catch {
        # Command not found
    }
    
    if ($psqlExists) {
        # First check if database exists
        try {
            # Use single quotes for the SQL command to avoid PowerShell variable expansion
            $checkDbQuery = "SELECT 1 FROM pg_database WHERE datname = '$pgDbName';"
            $env:PGPASSWORD = $pgPass
            
            # Connect to postgres database to check if our database exists
            $result = psql -h $pgHost -U $pgUser -d "postgres" -p $pgPort -t -c $checkDbQuery -w 2>&1
            
            if ($result -match "1") {
                Write-ColorMessage "[SUCCESS] Database '$pgDbName' already exists" -ForegroundColor Green
                $env:PGPASSWORD = ""
                return $true
            } else {
                Write-ColorMessage "Database '$pgDbName' does not exist. Creating it now..." -ForegroundColor Yellow
                
                # Create the database
                $createDbQuery = "CREATE DATABASE $pgDbName;"
                $createResult = psql -h $pgHost -U $pgUser -d "postgres" -p $pgPort -c $createDbQuery -w 2>&1
                
                if ($LASTEXITCODE -eq 0) {
                    Write-ColorMessage "[SUCCESS] Database '$pgDbName' created successfully" -ForegroundColor Green
                    $env:PGPASSWORD = ""
                    return $true
                } else {
                    Write-ColorMessage "[ERROR] Failed to create database: $createResult" -ForegroundColor Red
                    $env:PGPASSWORD = ""
                    return $false
                }
            }
        } catch {
            Write-ColorMessage "[ERROR] Error connecting to PostgreSQL: $_" -ForegroundColor Red
            $env:PGPASSWORD = ""
            return $false
        }
    } else {
        Write-ColorMessage "[WARNING] psql command not found. Cannot verify database existence." -ForegroundColor Yellow
        Write-ColorMessage "Proceeding with migrations, which will fail if database doesn't exist." -ForegroundColor Yellow
        return $true # Continue anyway and let EF migrations attempt to connect
    }
}

function Update-EfDatabase {
    Write-ColorMessage "Applying Entity Framework migrations to Azure PostgreSQL..." -ForegroundColor Yellow
    
    try {
        # Navigate to the API project directory
        $currentDir = Get-Location
        $apiDir = Join-Path -Path $currentDir -ChildPath "Emma.Api"
        
        if (Test-Path $apiDir) {
            Set-Location -Path $apiDir
            dotnet ef database update
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "[SUCCESS] Database migrations applied successfully" -ForegroundColor Green
            } else {
                Write-ColorMessage "[ERROR] Failed to apply database migrations" -ForegroundColor Red
                Write-ColorMessage "This could be due to connectivity issues or incorrect connection string" -ForegroundColor Yellow
            }
        } else {
            Write-ColorMessage "[ERROR] Could not find Emma.Api directory" -ForegroundColor Red
        }
    }
    catch {
        Write-ColorMessage "[ERROR] Error applying migrations: $_" -ForegroundColor Red
    }
    finally {
        # Return to the original directory
        Set-Location -Path $currentDir
    }
}

function Show-NextSteps {
    Write-ColorMessage "`nEmma AI Platform - Cloud Development Setup Complete!" -ForegroundColor Cyan
    Write-ColorMessage "------------------------------------------------" -ForegroundColor Cyan
    Write-ColorMessage "Your development environment is configured to use:" -ForegroundColor White
    Write-ColorMessage "[ENABLED] Azure PostgreSQL" -ForegroundColor Green
    Write-ColorMessage "[ENABLED] Azure Cosmos DB" -ForegroundColor Green
    Write-ColorMessage "[ENABLED] Azure OpenAI" -ForegroundColor Green
    Write-ColorMessage "[ENABLED] Azure AI Foundry" -ForegroundColor Green
    Write-ColorMessage "[ENABLED] Azure Storage" -ForegroundColor Green
    
    Write-ColorMessage "`nNext steps:" -ForegroundColor Yellow
    Write-ColorMessage "1. Build the solution: dotnet build" -ForegroundColor White
    Write-ColorMessage "2. Run the API: 'cd Emma.Api; dotnet run'" -ForegroundColor White
    Write-ColorMessage "3. Run the Web UI: 'cd Emma.Web; npm start'" -ForegroundColor White
    
    Write-ColorMessage "`nImportant notes:" -ForegroundColor Yellow
    Write-ColorMessage "- All data will be stored in Azure cloud services" -ForegroundColor White
    Write-ColorMessage "- No local services are required for development" -ForegroundColor White
    Write-ColorMessage "- Your .env file contains sensitive credentials - do not commit it to source control" -ForegroundColor White
    Write-ColorMessage "- Consider using Azure Key Vault for more secure credential management" -ForegroundColor White
}

# Main script execution
try {
    Write-ColorMessage "Emma AI Platform - Cloud Development Setup" -ForegroundColor Cyan
    Write-ColorMessage "=========================================" -ForegroundColor Cyan
    
    # Install dotnet-ef if needed
    Install-DotnetEfIfNeeded
    
    # Verify Azure credentials
    Test-AzureCredentials
    
    # Restore NuGet packages
    Restore-DotnetPackages
    
    # Check and create database if needed
    $dbExists = Test-PostgreSqlDatabase
    if (-not $dbExists) {
        Write-ColorMessage "[WARNING] Database setup failed. EF migrations may fail." -ForegroundColor Yellow
    }
    
    # Apply EF Core migrations
    Update-EfDatabase
    
    # Show next steps
    Show-NextSteps
}
catch {
    Write-ColorMessage "[ERROR] An error occurred: $_" -ForegroundColor Red
    exit 1
}
