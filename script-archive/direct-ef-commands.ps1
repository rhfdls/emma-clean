# Emma AI Platform - Direct EF Core Commands
# Simplest possible approach to apply migrations

# Set verbose output
$env:DOTNET_EF_VERBOSE = "1"

# Navigate to Emma.Data
Set-Location -Path "Emma.Data"

# List available migrations
Write-Output "LISTING MIGRATIONS:"
dotnet ef migrations list --startup-project ../Emma.Api

# Try to apply migrations
Write-Output "`nAPPLYING MIGRATIONS:"
dotnet ef database update --startup-project ../Emma.Api

# Return to original directory
Set-Location -Path ".."
