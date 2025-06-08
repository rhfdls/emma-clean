# Emma AI Platform - Environment Variable Migration Approach
# Sets environment variables directly before running migrations

# Load connection string
if (Test-Path ".env") {
    Get-Content ".env" | ForEach-Object {
        if ($_ -match "^([^#][^=]*)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            Set-Item -Path "env:$key" -Value $value
        }
    }
}

# Run migrations from correct location
Set-Location -Path "Emma.Data"
Write-Output "Running migrations from Emma.Data project..."
dotnet ef database update --startup-project ../Emma.Api

# Return to original directory
Set-Location -Path ".."
