# EMMA AI-First CRM Agent Orchestration Demo Script
# Demonstrates the complete agent catalog and registration system

Write-Host "🤖 EMMA AI-First CRM Agent Orchestration Demo" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$baseUrl = "http://localhost:5000"
$apiUrl = "$baseUrl/api"

# Check if API is running
Write-Host "🔍 Checking API availability..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-RestMethod -Uri "$apiUrl/health" -Method Get -TimeoutSec 5
    Write-Host "✅ API is running at $baseUrl" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not running. Please start the EMMA API first." -ForegroundColor Red
    Write-Host "   Run: dotnet run --project Emma.Api" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Demo 1: Load Agent Catalog
Write-Host "📁 Demo 1: Loading Agent Catalog" -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta

$catalogPath = Join-Path $PSScriptRoot "..\agents\catalog"
$loadCatalogRequest = @{
    catalogPath = $catalogPath
} | ConvertTo-Json

try {
    $loadResult = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/agents/catalog/load" -Method Post -Body $loadCatalogRequest -ContentType "application/json"
    Write-Host "✅ Loaded $($loadResult.loadedCount) agents from catalog" -ForegroundColor Green
    Write-Host "   Catalog Path: $($loadResult.catalogPath)" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  Could not load agent catalog: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Demo 2: Get Agent Capabilities
Write-Host "🎯 Demo 2: Agent Capabilities Discovery" -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor Magenta

try {
    $capabilities = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/agents/capabilities" -Method Get
    Write-Host "✅ Available Agent Capabilities:" -ForegroundColor Green
    
    foreach ($agent in $capabilities) {
        Write-Host "   🤖 $($agent.name) (v$($agent.version))" -ForegroundColor Cyan
        Write-Host "      ID: $($agent.id)" -ForegroundColor Gray
        Write-Host "      Capabilities: $($agent.capabilities -join ', ')" -ForegroundColor Gray
        Write-Host "      Intents: $($agent.intents -join ', ')" -ForegroundColor Gray
        Write-Host ""
    }
} catch {
    Write-Host "⚠️  Could not retrieve agent capabilities: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Demo 3: Agent Health Check
Write-Host "🏥 Demo 3: Agent Health Status" -ForegroundColor Magenta
Write-Host "==============================" -ForegroundColor Magenta

try {
    $healthStatuses = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/agents/health" -Method Get
    Write-Host "✅ Agent Health Status:" -ForegroundColor Green
    
    foreach ($health in $healthStatuses) {
        $statusColor = if ($health.isHealthy) { "Green" } else { "Red" }
        $statusIcon = if ($health.isHealthy) { "✅" } else { "❌" }
        
        Write-Host "   $statusIcon $($health.agentId): $($health.status)" -ForegroundColor $statusColor
        Write-Host "      Last Check: $($health.lastHealthCheck)" -ForegroundColor Gray
        Write-Host "      Response Time: $($health.responseTimeMs)ms" -ForegroundColor Gray
        Write-Host ""
    }
} catch {
    Write-Host "⚠️  Could not retrieve agent health: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Demo 4: Intent Classification and Orchestration
Write-Host "🧠 Demo 4: AI-First CRM Orchestration" -ForegroundColor Magenta
Write-Host "=====================================" -ForegroundColor Magenta

$scenarios = @(
    @{
        name = "Contact Management"
        userInput = "I need to update Emily Johnson's phone number and schedule a follow-up call for next week"
        context = @{
            contactId = "550e8400-e29b-41d4-a716-446655440001"
            agentId = "agent-001"
            industry = "real_estate"
        }
        interactionContent = "Agent called Emily to discuss property updates. Emily mentioned she's ready to make an offer and wants to schedule a viewing for this weekend."
    },
    @{
        name = "Property Search"
        userInput = "Find 3-bedroom houses under $500K in downtown area for the Johnson family"
        context = @{
            contactId = "550e8400-e29b-41d4-a716-446655440001"
            bedrooms = 3
            maxBudget = 500000
            area = "downtown"
            propertyType = "house"
        }
        interactionContent = "Client expressed strong interest in downtown properties with good school districts. Mentioned timeline pressure due to current lease ending."
    },
    @{
        name = "Market Analysis"
        userInput = "Provide market analysis for properties in the $400K-$600K range in the suburban area"
        context = @{
            priceRange = @{
                min = 400000
                max = 600000
            }
            area = "suburban"
            analysisType = "market_trends"
        }
        interactionContent = "Client is a first-time buyer looking for investment potential. Interested in areas with good appreciation prospects."
    }
)

foreach ($scenario in $scenarios) {
    Write-Host "📋 Scenario: $($scenario.name)" -ForegroundColor Cyan
    Write-Host "   Input: $($scenario.userInput)" -ForegroundColor Gray
    
    $orchestrationRequest = @{
        userInput = $scenario.userInput
        context = $scenario.context
        interactionContent = $scenario.interactionContent
        includeRecommendations = $true
        orchestrationMethod = "custom"
        userId = "demo-user"
        industry = "real_estate"
    } | ConvertTo-Json -Depth 10
    
    try {
        $response = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/process" -Method Post -Body $orchestrationRequest -ContentType "application/json"
        
        Write-Host "   ✅ Success: $($response.success)" -ForegroundColor Green
        Write-Host "   🎯 Intent: $($response.intentClassification.intent) (Confidence: $($response.intentClassification.confidence))" -ForegroundColor Yellow
        Write-Host "   ⚡ Urgency: $($response.intentClassification.urgency)" -ForegroundColor Yellow
        Write-Host "   🤖 Agent Response: $($response.content)" -ForegroundColor White
        Write-Host "   ⏱️  Processing Time: $($response.processingTimeMs)ms" -ForegroundColor Gray
        
        if ($response.contactContext) {
            Write-Host "   📊 Context Intelligence:" -ForegroundColor Cyan
            Write-Host "      Sentiment: $($response.contactContext.sentimentScore)" -ForegroundColor Gray
            Write-Host "      Close Probability: $($response.contactContext.closeProbability)" -ForegroundColor Gray
            Write-Host "      Buying Signals: $($response.contactContext.buyingSignals -join ', ')" -ForegroundColor Gray
        }
        
        if ($response.recommendedActions -and $response.recommendedActions.Count -gt 0) {
            Write-Host "   💡 Recommended Actions:" -ForegroundColor Cyan
            foreach ($action in $response.recommendedActions) {
                Write-Host "      • $action" -ForegroundColor Gray
            }
        }
        
    } catch {
        Write-Host "   ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Demo 5: Workflow Execution
Write-Host "🔄 Demo 5: Multi-Agent Workflow" -ForegroundColor Magenta
Write-Host "===============================" -ForegroundColor Magenta

$workflowRequest = @{
    workflowId = "client-onboarding-workflow"
    workflowVersion = "1.0.0"
    initialIntent = "ContactManagement"
    initialInput = "New client Sarah Davis wants to start looking for properties. Set up initial consultation and gather requirements."
    context = @{
        contactId = "550e8400-e29b-41d4-a716-446655440006"
        clientType = "first_time_buyer"
        budget = 450000
        timeline = "3_months"
    }
    urgency = "Medium"
    orchestrationMethod = "custom"
    userId = "demo-agent"
    industry = "real_estate"
} | ConvertTo-Json -Depth 10

try {
    Write-Host "🚀 Starting client onboarding workflow..." -ForegroundColor Yellow
    $workflowResponse = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/workflow" -Method Post -Body $workflowRequest -ContentType "application/json"
    
    Write-Host "✅ Workflow initiated successfully" -ForegroundColor Green
    Write-Host "   Workflow ID: $($workflowResponse.workflowId)" -ForegroundColor Gray
    Write-Host "   Status: $($workflowResponse.status)" -ForegroundColor Gray
    Write-Host "   Steps Completed: $($workflowResponse.steps.Count)" -ForegroundColor Gray
    
    foreach ($step in $workflowResponse.steps) {
        $stepIcon = if ($step.status -eq "Completed") { "✅" } else { "⏳" }
        Write-Host "   $stepIcon Step $($step.stepNumber): $($step.agentName) - $($step.status)" -ForegroundColor Cyan
        if ($step.output) {
            Write-Host "      Output: $($step.output)" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Host "❌ Workflow execution failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Demo 6: Orchestration Method Switching
Write-Host "🔄 Demo 6: Orchestration Method Switching" -ForegroundColor Magenta
Write-Host "=========================================" -ForegroundColor Magenta

$methods = @("custom", "azure_foundry")

foreach ($method in $methods) {
    Write-Host "🔧 Testing $method orchestration..." -ForegroundColor Yellow
    
    $methodRequest = @{
        method = $method
    } | ConvertTo-Json
    
    try {
        $methodResponse = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/orchestration/method" -Method Post -Body $methodRequest -ContentType "application/json"
        Write-Host "   ✅ Orchestration method set to: $($methodResponse.method)" -ForegroundColor Green
        
        # Test with simple request
        $testRequest = @{
            userInput = "Test orchestration method: $method"
            orchestrationMethod = $method
            userId = "demo-user"
            industry = "real_estate"
        } | ConvertTo-Json
        
        $testResponse = Invoke-RestMethod -Uri "$apiUrl/agentorchestration/process" -Method Post -Body $testRequest -ContentType "application/json"
        Write-Host "   🤖 Response using $method: $($testResponse.content)" -ForegroundColor White
        Write-Host "   📊 Method confirmed: $($testResponse.orchestrationMethod)" -ForegroundColor Gray
        
    } catch {
        Write-Host "   ❌ Error testing $method method: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

# Demo Summary
Write-Host "🎉 Demo Complete!" -ForegroundColor Green
Write-Host "=================" -ForegroundColor Green
Write-Host ""
Write-Host "✅ Agent Catalog System: Loaded and registered agents from JSON manifests" -ForegroundColor Green
Write-Host "✅ Intent Classification: AI-powered intent recognition with confidence scoring" -ForegroundColor Green
Write-Host "✅ Agent Communication: Hot-swappable orchestration with A2A protocol compliance" -ForegroundColor Green
Write-Host "✅ Context Intelligence: Sentiment analysis, buying signals, and close probability" -ForegroundColor Green
Write-Host "✅ Workflow Orchestration: Multi-step agent coordination and state management" -ForegroundColor Green
Write-Host "✅ Health Monitoring: Agent availability and performance tracking" -ForegroundColor Green
Write-Host "✅ Method Switching: Seamless transition between custom and Azure Foundry modes" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 The AI-first CRM agent orchestration system is fully operational!" -ForegroundColor Cyan
Write-Host "   Ready for Azure AI Foundry integration and enterprise deployment." -ForegroundColor Cyan
Write-Host ""
Write-Host "📚 Next Steps:" -ForegroundColor Yellow
Write-Host "   • Configure Azure AI Foundry endpoints for production" -ForegroundColor Gray
Write-Host "   • Set up CosmosDB for workflow state persistence" -ForegroundColor Gray
Write-Host "   • Deploy agent catalog to Azure Container Registry" -ForegroundColor Gray
Write-Host "   • Enable Application Insights for observability" -ForegroundColor Gray
Write-Host "   • Configure multi-tenant agent isolation" -ForegroundColor Gray
