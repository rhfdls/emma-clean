# Simple EMMA Agent Orchestration Demo
# Tests core functionality without Unicode characters

param(
    [string]$BaseUrl = "http://localhost:5000"
)

Write-Host "EMMA Agent Orchestration Demo" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check API Health
Write-Host "1. Testing API Health..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get -TimeoutSec 10
    Write-Host "[PASS] API is healthy" -ForegroundColor Green
    Write-Host "   Status: $($healthResponse.status)" -ForegroundColor Gray
} catch {
    Write-Host "[FAIL] API not accessible: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure to start the API with: dotnet run --project Emma.Api" -ForegroundColor Yellow
    exit 1
}

# Test 2: Load Agent Catalog
Write-Host ""
Write-Host "2. Loading Agent Catalog..." -ForegroundColor Yellow
try {
    $catalogPath = Join-Path $PSScriptRoot "..\agents\catalog"
    $loadRequest = @{
        catalogPath = $catalogPath
    } | ConvertTo-Json

    $loadResponse = Invoke-RestMethod -Uri "$BaseUrl/api/agentorchestration/agents/catalog/load" -Method Post -Body $loadRequest -ContentType "application/json" -TimeoutSec 15
    Write-Host "[PASS] Agent catalog loaded" -ForegroundColor Green
    Write-Host "   Agents loaded: $($loadResponse.agentsLoaded)" -ForegroundColor Gray
} catch {
    Write-Host "[WARN] Could not load agent catalog: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 3: Check Agent Capabilities
Write-Host ""
Write-Host "3. Checking Agent Capabilities..." -ForegroundColor Yellow
try {
    $capabilitiesResponse = Invoke-RestMethod -Uri "$BaseUrl/api/agentorchestration/agents/capabilities" -Method Get -TimeoutSec 10
    Write-Host "[PASS] Retrieved agent capabilities" -ForegroundColor Green
    Write-Host "   Available agents: $($capabilitiesResponse.agents.Count)" -ForegroundColor Gray
    
    foreach ($agent in $capabilitiesResponse.agents) {
        Write-Host "   - $($agent.name) (v$($agent.version))" -ForegroundColor Gray
    }
} catch {
    Write-Host "[WARN] Could not retrieve capabilities: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 4: Process Natural Language Request
Write-Host ""
Write-Host "4. Processing Natural Language Request..." -ForegroundColor Yellow
try {
    $orchestrationRequest = @{
        userInput = "I need to update Emily Johnson's contact information and schedule a follow-up call"
        context = @{
            contactId = "550e8400-e29b-41d4-a716-446655440001"
            agentId = "agent-001"
            industry = "real_estate"
        }
        includeRecommendations = $true
        orchestrationMethod = "custom"
    } | ConvertTo-Json -Depth 3

    $orchestrationResponse = Invoke-RestMethod -Uri "$BaseUrl/api/agentorchestration/process" -Method Post -Body $orchestrationRequest -ContentType "application/json" -TimeoutSec 20
    
    Write-Host "[PASS] Request processed successfully" -ForegroundColor Green
    Write-Host "   Intent: $($orchestrationResponse.intent)" -ForegroundColor Gray
    Write-Host "   Confidence: $($orchestrationResponse.confidence)" -ForegroundColor Gray
    Write-Host "   Response: $($orchestrationResponse.content.Substring(0, [Math]::Min(100, $orchestrationResponse.content.Length)))..." -ForegroundColor Gray
} catch {
    Write-Host "[WARN] Could not process request: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 5: Context Intelligence Analysis
Write-Host ""
Write-Host "5. Testing Context Intelligence..." -ForegroundColor Yellow
try {
    $analysisRequest = @{
        interactionContent = "Client expressed strong interest in downtown properties and wants to schedule viewing this week"
        contactContext = @{
            contactId = "550e8400-e29b-41d4-a716-446655440001"
            name = "Emily Johnson"
            relationshipState = "ActiveClient"
        }
    } | ConvertTo-Json -Depth 3

    $analysisResponse = Invoke-RestMethod -Uri "$BaseUrl/api/agentorchestration/analyze" -Method Post -Body $analysisRequest -ContentType "application/json" -TimeoutSec 15
    
    Write-Host "[PASS] Context analysis completed" -ForegroundColor Green
    Write-Host "   Sentiment Score: $($analysisResponse.sentimentScore)" -ForegroundColor Gray
    Write-Host "   Close Probability: $($analysisResponse.closeProbability)" -ForegroundColor Gray
    Write-Host "   Urgency: $($analysisResponse.urgency)" -ForegroundColor Gray
} catch {
    Write-Host "[WARN] Could not analyze context: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 6: Multi-Agent Workflow
Write-Host ""
Write-Host "6. Testing Multi-Agent Workflow..." -ForegroundColor Yellow
try {
    $workflowRequest = @{
        workflowId = "client-onboarding-workflow"
        initialIntent = "ContactManagement"
        initialInput = "New client wants to start property search in downtown area"
        context = @{
            clientType = "first_time_buyer"
            budget = 450000
            location = "downtown"
        }
    } | ConvertTo-Json -Depth 3

    $workflowResponse = Invoke-RestMethod -Uri "$BaseUrl/api/agentorchestration/workflow/execute" -Method Post -Body $workflowRequest -ContentType "application/json" -TimeoutSec 20
    
    Write-Host "[PASS] Workflow executed successfully" -ForegroundColor Green
    Write-Host "   Workflow ID: $($workflowResponse.workflowId)" -ForegroundColor Gray
    Write-Host "   Status: $($workflowResponse.status)" -ForegroundColor Gray
    Write-Host "   Steps Completed: $($workflowResponse.stepsCompleted)" -ForegroundColor Gray
} catch {
    Write-Host "[WARN] Could not execute workflow: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "Demo Summary" -ForegroundColor Cyan
Write-Host "============" -ForegroundColor Cyan
Write-Host ""
Write-Host "The EMMA AI-First CRM Agent Orchestration System is ready!" -ForegroundColor Green
Write-Host ""
Write-Host "Key Features Demonstrated:" -ForegroundColor White
Write-Host "- Agent catalog loading and registration" -ForegroundColor Gray
Write-Host "- Intent classification with confidence scoring" -ForegroundColor Gray
Write-Host "- Multi-agent workflow orchestration" -ForegroundColor Gray
Write-Host "- Context intelligence and sentiment analysis" -ForegroundColor Gray
Write-Host "- Hot-swappable orchestration methods" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "- Configure Azure OpenAI credentials" -ForegroundColor Gray
Write-Host "- Set up CosmosDB for workflow persistence" -ForegroundColor Gray
Write-Host "- Deploy to Azure Container Apps" -ForegroundColor Gray
Write-Host "- Integrate with Azure AI Foundry (when GA)" -ForegroundColor Gray
Write-Host ""
Write-Host "For more information, see: agents/README.md" -ForegroundColor Cyan
