$base = 'http://localhost:5000'
$orgId = 'da60e1ad-acc7-4b1d-91da-307d1b53c0ed'
$ownerId = '60e757d8-ae9a-4460-8b91-369a86f41a71'

$payload = @{
  firstName = 'Test'
  lastName  = 'Contact'
  organizationId = $orgId
  ownerId = $ownerId
} | ConvertTo-Json -Depth 5

try {
  Invoke-RestMethod -Uri "$base/api/contact" -Method Post -ContentType 'application/json' -Body $payload
} catch {
  if ($_.Exception.Response) {
    $resp = $_.Exception.Response
    $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
    $body = $reader.ReadToEnd()
    Write-Host "Status: $($resp.StatusCode) $($resp.StatusDescription)"
    Write-Host "Body:" -ForegroundColor Yellow
    Write-Host $body
  } else {
    throw
  }
}