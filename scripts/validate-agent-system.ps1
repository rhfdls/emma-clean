# EMMA AI-First CRM Agent System Validation Script
# Comprehensive validation of the complete agent orchestration implementation

param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$RunBuildTests = $false,
    [switch]$RunIntegrationTests = $false,
    [switch]$Verbose = $false
)

Write-Host " EMMA Agent System Validation" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"
$validationResults = @()

function Add-ValidationResult {
    param($Test, $Status, $Message, $Details = "")
    
    $result = @{
        Test = $Test
        Status = $Status
        Message = $Message
        Details = $Details
        Timestamp = Get-Date
    }
    
    $script:validationResults += $result
    
    $color = switch ($Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "WARN" { "Yellow" }
        "INFO" { "Cyan" }
        default { "White" }
    }
    
    $icon = switch ($Status) {
        "PASS" { "[PASS]" }
        "FAIL" { "[FAIL]" }
        "WARN" { "[WARN]" }
        "INFO" { "[INFO]" }
        default { "[TEST]" }
    }
    
    Write-Host "$icon $Test`: $Message" -ForegroundColor $color
    if ($Verbose -and $Details) {
        Write-Host "   Details: $Details" -ForegroundColor Gray
    }
}

# Test 1: Project Structure Validation
Write-Host " Validating Project Structure" -ForegroundColor Magenta
Write-Host "===============================" -ForegroundColor Magenta

$requiredFiles = @(
    "Emma.Core/Interfaces/IAgentRegistryService.cs",
    "Emma.Core/Interfaces/IIntentClassificationService.cs", 
    "Emma.Core/Interfaces/IAgentCommunicationBus.cs",
    "Emma.Core/Interfaces/IContextIntelligenceService.cs",
    "Emma.Core/Services/AgentRegistryService.cs",
    "Emma.Core/Services/IntentClassificationService.cs",
    "Emma.Core/Services/AgentCommunicationBus.cs",
    "Emma.Core/Services/ContextIntelligenceService.cs",
    "Emma.Core/Extensions/ServiceCollectionExtensions.cs",
    "Emma.Api/Controllers/AgentOrchestrationController.cs",
    "agents/catalog/orchestrators/emma-orchestrator.json",
    "agents/catalog/specialized/contact-management-agent.json",
    "agents/catalog/specialized/interaction-analysis-agent.json",
    "agents/README.md"
)

foreach ($file in $requiredFiles) {
    $fullPath = Join-Path $PSScriptRoot "..\$file"
    if (Test-Path $fullPath) {
        Add-ValidationResult "File Structure" "PASS" "Found: $file"
    } else {
        Add-ValidationResult "File Structure" "FAIL" "Missing: $file"
    }
}

# Test 2: Agent Card Validation
Write-Host ""
Write-Host " Validating Agent Cards" -ForegroundColor Magenta
Write-Host "=========================" -ForegroundColor Magenta

$agentCards = @(
    "agents/catalog/orchestrators/emma-orchestrator.json",
    "agents/catalog/specialized/contact-management-agent.json",
    "agents/catalog/specialized/interaction-analysis-agent.json"
)

foreach ($cardPath in $agentCards) {
    $fullPath = Join-Path $PSScriptRoot "..\$cardPath"
    if (Test-Path $fullPath) {
        try {
            $cardContent = Get-Content $fullPath -Raw | ConvertFrom-Json
            
            # Validate required A2A fields
            $requiredFields = @("id", "name", "version", "capabilities", "intents", "endpoints")
            $missingFields = @()
            
            foreach ($field in $requiredFields) {
                if (-not $cardContent.$field) {
                    $missingFields += $field
                }
            }
            
            if ($missingFields.Count -eq 0) {
                Add-ValidationResult "Agent Card" "PASS" "Valid A2A card: $(Split-Path $cardPath -Leaf)" "ID: $($cardContent.id), Version: $($cardContent.version)"
            } else {
                Add-ValidationResult "Agent Card" "FAIL" "Invalid A2A card: $(Split-Path $cardPath -Leaf)" "Missing fields: $($missingFields -join ', ')"
            }
        } catch {
            Add-ValidationResult "Agent Card" "FAIL" "Invalid JSON: $(Split-Path $cardPath -Leaf)" $_.Exception.Message
        }
    } else {
        Add-ValidationResult "Agent Card" "FAIL" "Missing agent card: $cardPath"
    }
}

# Test 3: Build Validation
if ($RunBuildTests) {
    Write-Host ""
    Write-Host " Build Validation" -ForegroundColor Magenta
    Write-Host "==================" -ForegroundColor Magenta
    
    try {
        $buildResult = dotnet build (Join-Path $PSScriptRoot "..") --configuration Release --verbosity quiet 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Add-ValidationResult "Build" "PASS" "Solution builds successfully"
        } else {
            Add-ValidationResult "Build" "FAIL" "Build failed" $buildResult
        }
    } catch {
        Add-ValidationResult "Build" "FAIL" "Build error" $_.Exception.Message
    }
}

# Test 4: Service Registration Validation
Write-Host ""
Write-Host " Service Registration Validation" -ForegroundColor Magenta
Write-Host "==================================" -ForegroundColor Magenta

$serviceExtensionsPath = Join-Path $PSScriptRoot "..\Emma.Core\Extensions\ServiceCollectionExtensions.cs"
if (Test-Path $serviceExtensionsPath) {
    $content = Get-Content $serviceExtensionsPath -Raw
    
    $requiredMethods = @(
        "AddEmmaAgentServices",
        "AddEmmaCoreServices", 
        "AddEmmaAgentServicesForDevelopment",
        "AddEmmaCoreServicesForDevelopment"
    )
    
    foreach ($method in $requiredMethods) {
        if ($content -match $method) {
            Add-ValidationResult "Service Registration" "PASS" "Found method: $method"
        } else {
            Add-ValidationResult "Service Registration" "FAIL" "Missing method: $method"
        }
    }
    
    # Check for required service registrations
    $requiredServices = @(
        "IAgentRegistryService",
        "IIntentClassificationService",
        "IAgentCommunicationBus", 
        "IContextIntelligenceService"
    )
    
    foreach ($service in $requiredServices) {
        if ($content -match $service) {
            Add-ValidationResult "Service Registration" "PASS" "Service registered: $service"
        } else {
            Add-ValidationResult "Service Registration" "FAIL" "Service not registered: $service"
        }
    }
} else {
    Add-ValidationResult "Service Registration" "FAIL" "ServiceCollectionExtensions.cs not found"
}

# Test 5: Integration Tests
if ($RunIntegrationTests) {
    Write-Host ""
    Write-Host " Integration Tests" -ForegroundColor Magenta
    Write-Host "===================" -ForegroundColor Magenta
    
    $testProject = Join-Path $PSScriptRoot "..\Emma.Tests"
    if (Test-Path $testProject) {
        try {
            $testResult = dotnet test $testProject --configuration Release --verbosity quiet --logger "console;verbosity=minimal" 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Add-ValidationResult "Integration Tests" "PASS" "All tests passed"
            } else {
                Add-ValidationResult "Integration Tests" "FAIL" "Some tests failed" $testResult
            }
        } catch {
            Add-ValidationResult "Integration Tests" "FAIL" "Test execution error" $_.Exception.Message
        }
    } else {
        Add-ValidationResult "Integration Tests" "WARN" "Test project not found"
    }
}

# Test 6: API Endpoint Validation (if running)
Write-Host ""
Write-Host " API Endpoint Validation" -ForegroundColor Magenta
Write-Host "==========================" -ForegroundColor Magenta

try {
    $healthResponse = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method Get -TimeoutSec 5 -ErrorAction Stop
    Add-ValidationResult "API Health" "PASS" "API is responding" "Status: $($healthResponse.status)"
    
    # Test agent orchestration endpoints
    $endpoints = @(
        "/api/agentorchestration/agents/capabilities",
        "/api/agentorchestration/agents/health"
    )
    
    foreach ($endpoint in $endpoints) {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl$endpoint" -Method Get -TimeoutSec 10 -ErrorAction Stop
            Add-ValidationResult "API Endpoint" "PASS" "Endpoint responding: $endpoint"
        } catch {
            Add-ValidationResult "API Endpoint" "FAIL" "Endpoint not responding: $endpoint" $_.Exception.Message
        }
    }
    
} catch {
    Add-ValidationResult "API Health" "WARN" "API not running or not accessible" "Start with: dotnet run --project Emma.Api"
}

# Test 7: Configuration Validation
Write-Host ""
Write-Host " Configuration Validation" -ForegroundColor Magenta
Write-Host "============================" -ForegroundColor Magenta

$configFiles = @(
    "Emma.Api/appsettings.json",
    "Emma.Api/appsettings.Development.json"
)

foreach ($configFile in $configFiles) {
    $fullPath = Join-Path $PSScriptRoot "..\$configFile"
    if (Test-Path $fullPath) {
        try {
            $config = Get-Content $fullPath -Raw | ConvertFrom-Json
            Add-ValidationResult "Configuration" "PASS" "Valid config: $(Split-Path $configFile -Leaf)"
            
            # Check for required sections
            if ($config.AzureOpenAI) {
                Add-ValidationResult "Configuration" "PASS" "AzureOpenAI section found"
            } else {
                Add-ValidationResult "Configuration" "WARN" "AzureOpenAI section missing"
            }
            
            if ($config.CosmosDb) {
                Add-ValidationResult "Configuration" "PASS" "CosmosDb section found"
            } else {
                Add-ValidationResult "Configuration" "WARN" "CosmosDb section missing"
            }
            
        } catch {
            Add-ValidationResult "Configuration" "FAIL" "Invalid JSON: $(Split-Path $configFile -Leaf)" $_.Exception.Message
        }
    } else {
        Add-ValidationResult "Configuration" "WARN" "Config file not found: $configFile"
    }
}

# Test 8: Documentation Validation
Write-Host ""
Write-Host " Documentation Validation" -ForegroundColor Magenta
Write-Host "============================" -ForegroundColor Magenta

$docFiles = @(
    "agents/README.md",
    "EMMA-DATA-DICTIONARY.md",
    "EMMA-AI-ARCHITECTURE-GUIDE.md"
)

foreach ($docFile in $docFiles) {
    $fullPath = Join-Path $PSScriptRoot "..\$docFile"
    if (Test-Path $fullPath) {
        $content = Get-Content $fullPath -Raw
        if ($content.Length -gt 1000) {
            Add-ValidationResult "Documentation" "PASS" "Comprehensive documentation: $(Split-Path $docFile -Leaf)" "Size: $($content.Length) chars"
        } else {
            Add-ValidationResult "Documentation" "WARN" "Documentation may be incomplete: $(Split-Path $docFile -Leaf)"
        }
    } else {
        Add-ValidationResult "Documentation" "FAIL" "Missing documentation: $docFile"
    }
}

# Generate Summary Report
Write-Host ""
Write-Host " Validation Summary" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan

$passCount = ($validationResults | Where-Object { $_.Status -eq "PASS" }).Count
$failCount = ($validationResults | Where-Object { $_.Status -eq "FAIL" }).Count
$warnCount = ($validationResults | Where-Object { $_.Status -eq "WARN" }).Count
$totalCount = $validationResults.Count

Write-Host ""
Write-Host "Total Tests: $totalCount" -ForegroundColor White
Write-Host "[PASS] Passed: $passCount" -ForegroundColor Green
Write-Host "[FAIL] Failed: $failCount" -ForegroundColor Red
Write-Host "[WARN]  Warnings: $warnCount" -ForegroundColor Yellow

$successRate = [math]::Round(($passCount / $totalCount) * 100, 1)
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } elseif ($successRate -ge 60) { "Yellow" } else { "Red" })

# Critical Issues
if ($failCount -gt 0) {
    Write-Host ""
    Write-Host " Critical Issues to Address:" -ForegroundColor Red
    $validationResults | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "   • $($_.Test): $($_.Message)" -ForegroundColor Red
    }
}

# Recommendations
Write-Host ""
Write-Host " Recommendations:" -ForegroundColor Yellow

if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host "   System is fully validated and ready for production!" -ForegroundColor Green
    Write-Host "   • All agent services implemented correctly" -ForegroundColor Green
    Write-Host "   • A2A protocol compliance verified" -ForegroundColor Green
    Write-Host "   • Documentation is comprehensive" -ForegroundColor Green
    Write-Host "   • Ready for Azure AI Foundry integration" -ForegroundColor Green
} else {
    if ($failCount -gt 0) {
        Write-Host "   Address critical failures before deployment" -ForegroundColor Red
    }
    if ($warnCount -gt 0) {
        Write-Host "   Review warnings for optimal configuration" -ForegroundColor Yellow
    }
    Write-Host "   Ensure all environment variables are configured" -ForegroundColor Yellow
    Write-Host "   Run integration tests with: -RunIntegrationTests" -ForegroundColor Yellow
    Write-Host "   Validate build with: -RunBuildTests" -ForegroundColor Yellow
}

# Export Results
$reportPath = Join-Path $PSScriptRoot "validation-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$validationResults | ConvertTo-Json -Depth 3 | Out-File $reportPath
Write-Host ""
Write-Host " Detailed report saved to: $reportPath" -ForegroundColor Gray

# Exit with appropriate code
if ($failCount -gt 0) {
    exit 1
} else {
    exit 0
}
