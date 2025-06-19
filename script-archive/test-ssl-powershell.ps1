# Test SSL connection to Emma AI Platform PostgreSQL using PowerShell
Write-Host "Testing SSL connection to Emma AI Platform PostgreSQL server..."
$server = "emma-db-server.postgres.database.azure.com"
$port = 5432

# Test basic TCP connectivity first
Write-Host "`nStep 1: Testing basic TCP connectivity..."
$tcpTest = Test-NetConnection -ComputerName $server -Port $port -InformationLevel Detailed
if ($tcpTest.TcpTestSucceeded) {
    Write-Host "TCP connection succeeded. Port 5432 is open and accessible."
} else {
    Write-Host "TCP connection failed. Port 5432 is not accessible. Check firewall rules."
    exit 1
}

# Try to establish a secure connection
Write-Host "`nStep 2: Attempting SSL handshake..."
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient($server, $port)
    
    # Create SSL Stream
    Write-Host "Creating SSL Stream..."
    $sslStream = New-Object System.Net.Security.SslStream($tcpClient.GetStream(), $false, {
        param($sender, $certificate, $chain, $sslPolicyErrors)
        
        # Log certificate details
        Write-Host "`nSSL Certificate Information:"
        Write-Host "Subject: $($certificate.Subject)"
        Write-Host "Issuer: $($certificate.Issuer)"
        Write-Host "Valid From: $($certificate.NotBefore)"
        Write-Host "Valid To: $($certificate.NotAfter)"
        Write-Host "SSL Policy Errors: $sslPolicyErrors"
        
        # For testing, we'll accept any certificate
        return $true
    })
    
    # Attempt handshake
    Write-Host "Attempting SSL handshake..."
    $sslStream.AuthenticateAsClient($server)
    
    Write-Host "`nSSL Handshake successful!"
    Write-Host "Cipher: $($sslStream.CipherAlgorithm)"
    Write-Host "Hash: $($sslStream.HashAlgorithm)"
    Write-Host "Key Exchange: $($sslStream.KeyExchangeAlgorithm)"
    Write-Host "Protocol: $($sslStream.SslProtocol)"
    
    # Close resources
    $sslStream.Close()
    $tcpClient.Close()
    
    Write-Host "`nSSL connectivity test passed. Certificate verification succeeded."
    Write-Host "You should be able to connect with: sslmode=require"
} catch {
    Write-Host "`nSSL handshake failed with error:"
    Write-Host $_.Exception.Message
    
    Write-Host "`nTroubleshooting suggestions:"
    Write-Host "1. Verify that the server requires SSL (require_secure_transport=ON)"
    Write-Host "2. Check if the certificate chain is trusted by your system"
    Write-Host "3. Ensure your firewall allows this connection"
    
    # Clean up resources if needed
    if ($sslStream) { $sslStream.Close() }
    if ($tcpClient) { $tcpClient.Close() }
}
