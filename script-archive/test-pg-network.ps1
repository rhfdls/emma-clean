# Test network connectivity to Azure PostgreSQL for Emma AI Platform
$pgServer = "emma-db-server.postgres.database.azure.com"
$pgPort = 5432

Write-Host "Testing network connectivity to the Emma AI Platform PostgreSQL server..."
Write-Host "Server: $pgServer"
Write-Host "Port: $pgPort"

# Get current IP address
$ipInfo = Invoke-RestMethod -Uri "https://api.ipify.org?format=json"
Write-Host "Your current public IP address: $($ipInfo.ip)"
Write-Host "This IP should be allowed in the Azure PostgreSQL firewall rules."

# Test basic connectivity
Write-Host "`nTesting TCP connection to PostgreSQL server..."
$tcpTest = Test-NetConnection -ComputerName $pgServer -Port $pgPort -InformationLevel Detailed

if ($tcpTest.TcpTestSucceeded) {
    Write-Host "TCP connection successful! Port is open and accessible."
    
    # Test SSL handshake
    Write-Host "`nTesting SSL handshake with PostgreSQL server..."
    
    # Powershell-native SSL check
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient($pgServer, $pgPort)
        $sslStream = New-Object System.Net.Security.SslStream($tcpClient.GetStream())
        $sslStream.AuthenticateAsClient($pgServer)
        Write-Host "SSL handshake successful!"
        $sslStream.Close()
        $tcpClient.Close()
    } catch {
        Write-Host "SSL handshake failed: $_"
    }
} else {
    Write-Host "TCP connection failed. Check if firewall rules are correctly configured."
}

Write-Host "`nFirewall Configuration Check:"
Write-Host "1. Confirm in Azure Portal that your IP ($($ipInfo.ip)) is added to the firewall rules."
Write-Host "2. Ensure 'Allow public access from any Azure service within Azure to this server' is enabled if needed."
Write-Host "3. Check if 'Require SSL connection' is set to 'ENABLED' in Azure PostgreSQL."
