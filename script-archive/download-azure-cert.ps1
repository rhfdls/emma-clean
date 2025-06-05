# Download the proper Azure PostgreSQL certificate for Emma AI Platform
Write-Host "Downloading required certificate for Azure PostgreSQL SSL connection..."

# Create certs directory if it doesn't exist
$certsDir = ".\certs"
if (-not (Test-Path $certsDir)) {
    New-Item -Path $certsDir -ItemType Directory | Out-Null
    Write-Host "Created directory: $certsDir"
}

# Download the DigiCert Global Root CA certificate (used by Azure)
$certUrl = "https://dl.cacerts.digicert.com/DigiCertGlobalRootCA.crt.pem"
$certPath = "$certsDir\DigiCertGlobalRootCA.crt.pem"
Invoke-WebRequest -Uri $certUrl -OutFile $certPath
Write-Host "Downloaded DigiCert Global Root CA certificate to: $certPath"

# Also download the DigiCert Global Root G2 certificate as backup
$certG2Url = "https://cacerts.digicert.com/DigiCertGlobalRootG2.crt.pem"
$certG2Path = "$certsDir\DigiCertGlobalRootG2.crt.pem"
Invoke-WebRequest -Uri $certG2Url -OutFile $certG2Path
Write-Host "Downloaded DigiCert Global Root G2 certificate to: $certG2Path"

# Output the full path for reference
$fullCertPath = (Resolve-Path $certPath).Path
Write-Host "`nCertificate full path (use this in your connection):"
Write-Host $fullCertPath

Write-Host "`nCertificate download complete. Use this certificate when connecting to the Emma AI Platform PostgreSQL database."
