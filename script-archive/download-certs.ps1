# Download required certificates for Azure PostgreSQL flexible server
Write-Host "Downloading required root certificates for Emma AI Platform database connection..."

# Create certs directory if it doesn't exist
$certsDir = ".\certs"
if (-not (Test-Path $certsDir)) {
    New-Item -Path $certsDir -ItemType Directory | Out-Null
    Write-Host "Created directory: $certsDir"
}

# Download the Microsoft RSA Root Certificate Authority 2017
$msRSARootUrl = "https://www.microsoft.com/pkiops/certs/Microsoft%20RSA%20Root%20Certificate%20Authority%202017.crt"
$msRSARootPath = "$certsDir\MicrosoftRSARootCertificateAuthority2017.crt"
Invoke-WebRequest -Uri $msRSARootUrl -OutFile $msRSARootPath
Write-Host "Downloaded Microsoft RSA Root Certificate Authority 2017 to $msRSARootPath"

# Download the DigiCert Global Root G2
$digiCertG2Url = "https://cacerts.digicert.com/DigiCertGlobalRootG2.crt"
$digiCertG2Path = "$certsDir\DigiCertGlobalRootG2.crt"
Invoke-WebRequest -Uri $digiCertG2Url -OutFile $digiCertG2Path
Write-Host "Downloaded DigiCert Global Root G2 to $digiCertG2Path"

# Download the DigiCert Global Root CA
$digiCertRootUrl = "https://cacerts.digicert.com/DigiCertGlobalRootCA.crt"
$digiCertRootPath = "$certsDir\DigiCertGlobalRootCA.crt"
Invoke-WebRequest -Uri $digiCertRootUrl -OutFile $digiCertRootPath
Write-Host "Downloaded DigiCert Global Root CA to $digiCertRootPath"

Write-Host "`nAll required certificates downloaded to $certsDir directory."
Write-Host "These certificates will be used to establish secure connections to the Emma AI Platform PostgreSQL database."
