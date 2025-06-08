# Download and validate certificates for Emma AI Platform PostgreSQL connection
Write-Host "Downloading and validating SSL certificates for Azure PostgreSQL..."

# Create certs directory if it doesn't exist
$certsDir = "./certs"
if (-not (Test-Path $certsDir)) {
    New-Item -Path $certsDir -ItemType Directory | Out-Null
    Write-Host "Created directory: $certsDir"
}

# Download DigiCert Global Root CA certificate (primary Azure certificate)
$digiCertUrl = "https://dl.cacerts.digicert.com/DigiCertGlobalRootCA.crt.pem"
$digiCertPath = "$certsDir/DigiCertGlobalRootCA.crt.pem"
Invoke-WebRequest -Uri $digiCertUrl -OutFile $digiCertPath
Write-Host "Downloaded DigiCert Global Root CA to: $digiCertPath"

# Download Baltimore CyberTrust Root (backup certificate)
$baltimoreUrl = "https://www.digicert.com/CACerts/BaltimoreCyberTrustRoot.crt.pem"
$baltimorePath = "$certsDir/BaltimoreCyberTrustRoot.crt.pem"
Invoke-WebRequest -Uri $baltimoreUrl -OutFile $baltimorePath
Write-Host "Downloaded Baltimore CyberTrust Root to: $baltimorePath"

# Verify certificate files
Write-Host "`nVerifying certificate files:"

function Verify-Certificate {
    param([string]$path)
    
    if (Test-Path $path) {
        $content = Get-Content -Path $path -Raw
        $hasBegin = $content -match "-----BEGIN CERTIFICATE-----"
        $hasEnd = $content -match "-----END CERTIFICATE-----"
        
        if ($hasBegin -and $hasEnd) {
            Write-Host "✅ $path is valid (contains proper BEGIN/END markers)"
            return $true
        } else {
            Write-Host "❌ $path is invalid (missing proper BEGIN/END markers)"
            return $false
        }
    } else {
        Write-Host "❌ $path does not exist"
        return $false
    }
}

$digiCertValid = Verify-Certificate -path $digiCertPath
$baltimoreValid = Verify-Certificate -path $baltimorePath

if ($digiCertValid -or $baltimoreValid) {
    Write-Host "`nAt least one valid certificate is available for SSL connections."
    Write-Host "DigiCert Global Root CA: $digiCertPath"
    Write-Host "Baltimore CyberTrust Root: $baltimorePath"
    Write-Host "`nUse these certificates with sslrootcert parameter in your connection string."
} else {
    Write-Host "`nNo valid certificates found. Please check the download URLs and try again."
}
