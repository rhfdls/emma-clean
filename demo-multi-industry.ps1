# Demo Script: Multi-Industry EMMA Platform
# This script demonstrates the industry-agnostic EMMA platform capabilities

Write-Host "🚀 Multi-Industry EMMA Platform Demo" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5000"

Write-Host "1. Testing available industries..." -ForegroundColor Yellow
try {
    $industries = Invoke-RestMethod -Uri "$baseUrl/demo/industries" -Method GET
    Write-Host "✅ Industries loaded successfully!" -ForegroundColor Green
    
    foreach ($industry in $industries.supportedIndustries) {
        Write-Host ""
        Write-Host "🏢 Industry: $($industry.name) ($($industry.code))" -ForegroundColor Cyan
        Write-Host "   Sample Queries:" -ForegroundColor White
        foreach ($query in $industry.sampleQueries) {
            Write-Host "   • $($query.query)" -ForegroundColor Gray
        }
        Write-Host "   Available Actions: $($industry.availableActions -join ', ')" -ForegroundColor White
        Write-Host "   Workflow States: $($industry.workflowStates -join ', ')" -ForegroundColor White
    }
}
catch {
    Write-Host "❌ Failed to get industries: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "2. Testing Ask EMMA with Real Estate context..." -ForegroundColor Yellow
try {
    $realEstateRequest = @{
        message = "What's the next best action for a new lead who just inquired about a property?"
        industryCode = "RealEstate"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/api/aea/ask" -Method POST -Body $realEstateRequest -ContentType "application/json"
    Write-Host "✅ Real Estate Response:" -ForegroundColor Green
    Write-Host "$($response.response)" -ForegroundColor White
    Write-Host "Processing time: $($response.processingTimeMs)ms" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Real Estate query failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Testing Ask EMMA with Mortgage context..." -ForegroundColor Yellow
try {
    $mortgageRequest = @{
        message = "What documents should I request from a first-time homebuyer?"
        industryCode = "Mortgage"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/api/aea/ask" -Method POST -Body $mortgageRequest -ContentType "application/json"
    Write-Host "✅ Mortgage Response:" -ForegroundColor Green
    Write-Host "$($response.response)" -ForegroundColor White
    Write-Host "Processing time: $($response.processingTimeMs)ms" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Mortgage query failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "4. Testing Ask EMMA with Financial Advisory context..." -ForegroundColor Yellow
try {
    $financialRequest = @{
        message = "How should I approach a client who wants to discuss retirement planning?"
        industryCode = "Financial"
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "$baseUrl/api/aea/ask" -Method POST -Body $financialRequest -ContentType "application/json"
    Write-Host "✅ Financial Advisory Response:" -ForegroundColor Green
    Write-Host "$($response.response)" -ForegroundColor White
    Write-Host "Processing time: $($response.processingTimeMs)ms" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Financial Advisory query failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Multi-Industry EMMA Demo Complete!" -ForegroundColor Green
Write-Host "The platform successfully adapts to different industries with:" -ForegroundColor White
Write-Host "• Industry-specific prompts and terminology" -ForegroundColor Gray
Write-Host "• Tailored workflow recommendations" -ForegroundColor Gray
Write-Host "• Context-aware AI responses" -ForegroundColor Gray
Write-Host "• Pluggable industry modules" -ForegroundColor Gray
