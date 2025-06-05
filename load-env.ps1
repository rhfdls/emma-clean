# load-env.ps1
# Loads environment variables from .env file for development
# Note: We exclusively use Azure PostgreSQL - no local PostgreSQL instance is required

Write-Host "Loading environment variables for Emma AI Platform..." -ForegroundColor Cyan

# Check if .env exists
if (Test-Path ".env") {
    $envContent = Get-Content ".env"
    
    foreach ($line in $envContent) {
        # Skip comments and empty lines
        if ($line.Trim().StartsWith("#") -or [string]::IsNullOrWhiteSpace($line)) {
            continue
        }
        
        # Parse key=value format
        $parts = $line.Split('=', 2)
        if ($parts.Length -eq 2) {
            $key = $parts[0].Trim()
            $value = $parts[1].Trim()
            
            # Set the environment variable
            [Environment]::SetEnvironmentVariable($key, $value, "Process")
            Write-Host "  Set $key" -ForegroundColor Green
        }
    }
    
    Write-Host "Environment variables loaded successfully." -ForegroundColor Green
} else {
    Write-Host "Warning: .env file not found." -ForegroundColor Yellow
    Write-Host "Please ensure the .env file exists at C:\Users\david\GitHub\WindsurfProjects\emma\.env" -ForegroundColor Yellow
    Write-Host "It should contain your PostgreSQL connection string and other environment variables." -ForegroundColor Yellow
}

# Display current PostgreSQL connection string (hide password for security)
if ([Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")) {
    $connStr = [Environment]::GetEnvironmentVariable("ConnectionStrings__PostgreSql")
    $maskedConnStr = $connStr
    
    # Mask password if present
    if ($connStr -match 'Password=([^;]+)') {
        $maskedConnStr = $connStr -replace 'Password=([^;]+)', 'Password=********'
    }
    
    Write-Host "`nCurrent PostgreSQL Connection:" -ForegroundColor Cyan
    Write-Host $maskedConnStr -ForegroundColor White
} else {
    Write-Host "`nNo PostgreSQL connection string found in environment variables." -ForegroundColor Yellow
}

Write-Host "`nYou can now run Entity Framework commands or connect to your Azure PostgreSQL database." -ForegroundColor Cyan
