# Verify Azure PostgreSQL settings for Emma AI Platform
Write-Host "Emma AI Platform - PostgreSQL Connection Verification Tool"
Write-Host "========================================================"

# Get external IP
$externalIP = (Invoke-RestMethod -Uri "https://api.ipify.org?format=json").ip
Write-Host "Your current public IP: $externalIP"

Write-Host "`nVerification Checklist for Azure Portal:"
Write-Host "----------------------------------------"
Write-Host "1. Firewall Rules:"
Write-Host "   - Confirm rule exists for IP: $externalIP"
Write-Host "   - Check if 'Allow access to Azure services' is enabled"
Write-Host "   - Verify no Network Security Groups (NSGs) are blocking port 5432"

Write-Host "`n2. Server Configuration:"
Write-Host "   - Check 'require_secure_transport' is set appropriately (ON for SSL requirement)"
Write-Host "   - Verify server status is 'Available'"

Write-Host "`n3. Authentication:"
Write-Host "   - Verify username is exactly: emmaadmin@emma-db-server"
Write-Host "   - Confirm password matches exactly what's in .env file: 'GOGdb54321'"
Write-Host "   - Check if password has special characters or unusual formatting"
Write-Host "   - Consider resetting password to something simple like 'EmmaDb2025Test'"

Write-Host "`n4. Connection Parameters:"
Write-Host "   - Ensure SSL mode is set to 'require'"
Write-Host "   - Verify database name is exactly: emma"
Write-Host "   - Check server name is exactly: emma-db-server.postgres.database.azure.com"
Write-Host "   - Confirm port is 5432"

Write-Host "`nAdditional Troubleshooting:"
Write-Host "----------------------------"
Write-Host "- Try creating a test connection from Azure Cloud Shell"
Write-Host "- Verify no recent maintenance or service alerts for Azure PostgreSQL"
Write-Host "- Check if server has connection limits or resource constraints"

Write-Host "`nPlease check these settings in the Azure Portal and make any needed adjustments."
