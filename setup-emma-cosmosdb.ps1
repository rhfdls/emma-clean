# Emma AI Platform - Cosmos DB Setup Script
# This script connects to Azure Cosmos DB and creates the necessary database and container
# with proper indexing policy for the Emma AI Platform's content storage needs.

# Load environment variables from .env file
function Load-EnvFile {
    param (
        [string]$envFilePath = ".\.env"
    )
    
    if (Test-Path $envFilePath) {
        Get-Content $envFilePath | ForEach-Object {
            if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim()
                # Remove quotes if they exist
                if ($value -match '^"(.*)"$' -or $value -match "^'(.*)'$") {
                    $value = $matches[1]
                }
                [Environment]::SetEnvironmentVariable($key, $value, "Process")
                Write-Host "Loaded environment variable: $key"
            }
        }
    } else {
        Write-Error ".env file not found at $envFilePath"
        exit 1
    }
}

# Install required modules if not already installed
function Ensure-Module {
    param (
        [string]$ModuleName,
        [string]$MinimumVersion = $null
    )

    $moduleParams = @{
        Name = $ModuleName
        ErrorAction = 'SilentlyContinue'
    }
    
    if ($MinimumVersion) {
        $moduleParams['MinimumVersion'] = $MinimumVersion
    }
    
    $module = Get-Module -ListAvailable @moduleParams
    
    if (-not $module) {
        Write-Host "Installing module $ModuleName..."
        Install-Module -Name $ModuleName -Scope CurrentUser -Force -AllowClobber
        Write-Host "$ModuleName module installed."
    } else {
        Write-Host "$ModuleName module already installed."
    }
}

# Function to create a Cosmos DB database and container
function Setup-CosmosDB {
    param (
        [string]$EndpointUrl,
        [string]$PrimaryKey,
        [string]$DatabaseName,
        [string]$ContainerName,
        [string]$PartitionKeyPath = "/interactionId"
    )
    
    try {
        # Connect to the Cosmos DB account
        $cosmosDbContext = New-CosmosDbContext -Account $EndpointUrl.Replace("https://", "").Replace(":443/", "") -Key $PrimaryKey
        
        # Create database if it doesn't exist
        $database = Get-CosmosDbDatabase -Context $cosmosDbContext -Id $DatabaseName -ErrorAction SilentlyContinue
        
        if (-not $database) {
            Write-Host "Creating database '$DatabaseName'..."
            $database = New-CosmosDbDatabase -Context $cosmosDbContext -Id $DatabaseName
            Write-Host "Database '$DatabaseName' created."
        } else {
            Write-Host "Database '$DatabaseName' already exists."
        }
        
        # Create container if it doesn't exist
        $container = Get-CosmosDbCollection -Context $cosmosDbContext -Database $DatabaseName -Id $ContainerName -ErrorAction SilentlyContinue
        
        if (-not $container) {
            Write-Host "Creating container '$ContainerName' with partition key '$PartitionKeyPath'..."
            
            # Define indexing policy
            $indexingPolicy = @{
                indexingMode = "consistent"
                includedPaths = @(
                    @{
                        path = "/*"
                    }
                )
                excludedPaths = @(
                    @{
                        path = "/content/fullText/?"
                    },
                    @{
                        path = "/aiProcessing/vectorEmbedding/?"
                    }
                )
                compositeIndexes = @(
                    @(
                        @{
                            path = "/interactionId"
                            order = "ascending"
                        },
                        @{
                            path = "/created"
                            order = "descending"
                        }
                    )
                )
            }
            
            # Convert to JSON
            $indexingPolicyJson = $indexingPolicy | ConvertTo-Json -Depth 10
            
            # Create container with custom indexing policy
            $container = New-CosmosDbCollection -Context $cosmosDbContext -Database $DatabaseName -Id $ContainerName -PartitionKey $PartitionKeyPath -IndexingPolicy $indexingPolicyJson -DefaultTimeToLive -1
            
            Write-Host "Container '$ContainerName' created."
        } else {
            Write-Host "Container '$ContainerName' already exists."
        }
        
        Write-Host "Cosmos DB setup completed successfully."
        return $true
    }
    catch {
        Write-Error "Error setting up Cosmos DB: $_"
        return $false
    }
}

# Function to insert sample documents
function Insert-SampleDocuments {
    param (
        [string]$EndpointUrl,
        [string]$PrimaryKey,
        [string]$DatabaseName,
        [string]$ContainerName,
        [string]$SchemaFilePath = ".\emma-cosmosdb-schema.json"
    )
    
    try {
        # Load schema file with sample documents
        $schemaContent = Get-Content $SchemaFilePath -Raw | ConvertFrom-Json
        $sampleDocs = $schemaContent.documentExamples
        
        if (-not $sampleDocs -or $sampleDocs.Count -eq 0) {
            Write-Host "No sample documents found in schema file."
            return $false
        }
        
        # Connect to Cosmos DB
        $cosmosDbContext = New-CosmosDbContext -Account $EndpointUrl.Replace("https://", "").Replace(":443/", "") -Key $PrimaryKey
        
        # Insert each sample document
        foreach ($doc in $sampleDocs) {
            $docId = $doc.id
            $docJson = $doc | ConvertTo-Json -Depth 10
            
            Write-Host "Inserting sample document with ID: $docId"
            
            try {
                # Check if document exists
                $existingDoc = Get-CosmosDbDocument -Context $cosmosDbContext -Database $DatabaseName -CollectionId $ContainerName -Id $docId -PartitionKey $doc.interactionId -ErrorAction SilentlyContinue
                
                if ($existingDoc) {
                    Write-Host "Document '$docId' already exists. Updating..."
                    Set-CosmosDbDocument -Context $cosmosDbContext -Database $DatabaseName -CollectionId $ContainerName -Id $docId -DocumentBody $docJson -PartitionKey $doc.interactionId
                } else {
                    New-CosmosDbDocument -Context $cosmosDbContext -Database $DatabaseName -CollectionId $ContainerName -DocumentBody $docJson -PartitionKey $doc.interactionId
                }
                
                Write-Host "Document '$docId' inserted/updated successfully."
            }
            catch {
                Write-Warning "Error inserting document '$docId': $_"
            }
        }
        
        Write-Host "Sample documents inserted successfully."
        return $true
    }
    catch {
        Write-Error "Error inserting sample documents: $_"
        return $false
    }
}

# Main execution flow
function Show-Menu {
    Write-Host "==== Emma AI Platform - Cosmos DB Setup ===="
    Write-Host "1. Install required PowerShell modules"
    Write-Host "2. Create Cosmos DB database and container"
    Write-Host "3. Insert sample documents"
    Write-Host "4. Run complete setup"
    Write-Host "5. Exit"
    Write-Host ""
    
    $choice = Read-Host "Enter your choice (1-5)"
    return $choice
}

# Main execution loop
try {
    # Load environment variables
    Load-EnvFile
    
    # Extract Cosmos DB configuration from environment variables
    $cosmosDbEndpoint = $env:COSMOSDB__ACCOUNTENDPOINT
    $cosmosDbKey = $env:COSMOSDB__ACCOUNTKEY
    $cosmosDbName = $env:COSMOSDB__DATABASENAME
    $cosmosDbContainer = $env:COSMOSDB__CONTAINERNAME
    
    if (-not $cosmosDbEndpoint -or -not $cosmosDbKey -or -not $cosmosDbName -or -not $cosmosDbContainer) {
        Write-Error "Cosmos DB configuration not found in environment variables."
        Write-Host "Make sure your .env file contains the following variables:"
        Write-Host "COSMOSDB__ACCOUNTENDPOINT=https://your-account.documents.azure.com:443/"
        Write-Host "COSMOSDB__ACCOUNTKEY=your-primary-key"
        Write-Host "COSMOSDB__DATABASENAME=emma-agent"
        Write-Host "COSMOSDB__CONTAINERNAME=messages"
        exit 1
    }
    
    # Show menu and process choices
    $exitRequested = $false
    
    while (-not $exitRequested) {
        $choice = Show-Menu
        
        switch ($choice) {
            "1" {
                Write-Host "Installing required PowerShell modules..."
                Ensure-Module -ModuleName "CosmosDB" -MinimumVersion "4.0.0"
                Write-Host "Modules installed successfully."
                Write-Host ""
            }
            "2" {
                Write-Host "Creating Cosmos DB database and container..."
                Setup-CosmosDB -EndpointUrl $cosmosDbEndpoint -PrimaryKey $cosmosDbKey -DatabaseName $cosmosDbName -ContainerName $cosmosDbContainer
                Write-Host ""
            }
            "3" {
                Write-Host "Inserting sample documents..."
                Insert-SampleDocuments -EndpointUrl $cosmosDbEndpoint -PrimaryKey $cosmosDbKey -DatabaseName $cosmosDbName -ContainerName $cosmosDbContainer
                Write-Host ""
            }
            "4" {
                Write-Host "Running complete setup..."
                Ensure-Module -ModuleName "CosmosDB" -MinimumVersion "4.0.0"
                $dbSetup = Setup-CosmosDB -EndpointUrl $cosmosDbEndpoint -PrimaryKey $cosmosDbKey -DatabaseName $cosmosDbName -ContainerName $cosmosDbContainer
                
                if ($dbSetup) {
                    $docsInserted = Insert-SampleDocuments -EndpointUrl $cosmosDbEndpoint -PrimaryKey $cosmosDbKey -DatabaseName $cosmosDbName -ContainerName $cosmosDbContainer
                    
                    if ($docsInserted) {
                        Write-Host "Complete setup finished successfully!" -ForegroundColor Green
                    }
                }
                
                Write-Host ""
            }
            "5" {
                $exitRequested = $true
                Write-Host "Exiting..."
            }
            default {
                Write-Host "Invalid choice. Please enter a number between 1 and 5." -ForegroundColor Red
                Write-Host ""
            }
        }
    }
}
catch {
    Write-Error "An error occurred: $_"
    exit 1
}
