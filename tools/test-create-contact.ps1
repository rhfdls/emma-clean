param(
  [Parameter(Mandatory=$true)][string]$BaseUrl,
  [Parameter(Mandatory=$true)][string]$OrgId,
  [Parameter(Mandatory=$true)][string]$OwnerId
)

$ErrorActionPreference = 'Stop'

$payload = @{ 
  firstName = 'Test'
  lastName  = 'Contact'
  organizationId = $OrgId
  ownerId = $OwnerId
  emailAddresses = @(@{ address = 'test.contact@example.com'; isPrimary = $true })
  phoneNumbers   = @(@{ number = '+15551234567'; isPrimary = $true })
} | ConvertTo-Json -Depth 5

Write-Host "POST $BaseUrl/api/contact" -ForegroundColor Cyan
$createResp = Invoke-RestMethod -Uri "$BaseUrl/api/contact" -Method Post -ContentType 'application/json' -Body $payload
$createResp | ConvertTo-Json -Depth 5

$contactId = $createResp.id
Write-Host ("Created contactId: " + $contactId) -ForegroundColor Green

Write-Host "GET $BaseUrl/api/contact/$contactId" -ForegroundColor Cyan
Invoke-RestMethod -Uri "$BaseUrl/api/contact/$contactId" -Method Get | ConvertTo-Json -Depth 5

Write-Host "GET $BaseUrl/api/contact?orgId=$OrgId" -ForegroundColor Cyan
Invoke-RestMethod -Uri "$BaseUrl/api/contact?orgId=$OrgId" -Method Get | ConvertTo-Json -Depth 5
