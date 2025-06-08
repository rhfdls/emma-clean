# Test Azure OpenAI Connection
$envFile = ".\.env"

# Load environment variables
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.*)') {
        $varName = $matches[1].Trim()
        $varValue = $matches[2].Trim(' "\')
        [System.Environment]::SetEnvironmentVariable($varName, $varValue, 'Process')
    }
}

$url = "$($env:AZURE_OPENAI_ENDPOINT)openai/deployments/$($env:AZURE_OPENAI_DEPLOYMENT)/chat/completions?api-version=2023-05-15"
$headers = @{
    "Content-Type"  = "application/json"
    "api-key"      = $env:AZURE_OPENAI_KEY
}

$body = @{
    messages = @(
        @{
            role = "user"
            content = "Hello, can you hear me? Just respond with 'Connection successful' if you can hear me."
        }
    )
    max_tokens = 100
    temperature = 0.7
} | ConvertTo-Json

try {
    Write-Host "Testing connection to Azure OpenAI endpoint..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri $url -Method Post -Headers $headers -Body $body -ErrorAction Stop
    
    if ($response.choices[0].message.content) {
        Write-Host "✅ Azure OpenAI connection successful!" -ForegroundColor Green
        Write-Host "Response: " -NoNewline
        Write-Host $response.choices[0].message.content -ForegroundColor Cyan
    } else {
        Write-Host "⚠️  Received response, but no content in choices:" -ForegroundColor Yellow
        $response | ConvertTo-Json -Depth 5 | Out-Host
    }
} catch {
    Write-Host "❌ Error connecting to Azure OpenAI:" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
