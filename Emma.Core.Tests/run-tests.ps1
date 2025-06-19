<#
.SYNOPSIS
    Run test suites for the EMMA Core project

.DESCRIPTION
    This script provides commands to run different test suites for the EMMA Core project.
    You can run all tests or specific test suites as needed.

.EXAMPLE
    .\run-tests.ps1 -All
    Runs all test suites

.EXAMPLE
    .\run-tests.ps1 -ScheduledAction
    Runs only the ScheduledAction model tests

.EXAMPLE
    .\run-tests.ps1 -ActionRelevanceValidator
    Runs only the ActionRelevanceValidator service tests
#>

param(
    [switch]$All,
    [switch]$ScheduledAction,
    [switch]$ActionRelevanceValidator,
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Project root directory
$projectRoot = Split-Path -Parent $PSScriptRoot
$testProjectPath = Join-Path $projectRoot "Emma.Core.Tests"
$testDllPath = Join-Path $testProjectPath "bin\Debug\net8.0\Emma.Core.Tests.dll"

# Common dotnet test arguments
$commonArgs = @(
    "test",
    "$testProjectPath\Emma.Core.Tests.csproj",
    "--no-build",
    "--configuration Debug",
    "--logger ""console;verbosity=normal"""
)

# Function to run tests with filtering
function Run-TestFilter {
    param(
        [string]$filter,
        [string]$testSuiteName
    )
    
    Write-Host "`n=== RUNNING $testSuiteName TESTS ===" -ForegroundColor Cyan
    $filterArg = "--filter \"FullyQualifiedName~$filter\""
    $fullCmd = "dotnet $($commonArgs -join ' ') $filterArg"
    
    if ($Verbose) {
        Write-Host "Command: $fullCmd" -ForegroundColor DarkGray
    }
    
    try {
        Invoke-Expression $fullCmd
        Write-Host "`n✅ $testSuiteName tests completed successfully" -ForegroundColor Green
    } catch {
        Write-Host "`n❌ $testSuiteName tests failed" -ForegroundColor Red
        throw $_
    }
}

# Main script execution
try {
    # If no specific test suite is specified, show help
    if (-not ($All -or $ScheduledAction -or $ActionRelevanceValidator)) {
        Write-Host "Please specify which tests to run or use -All to run all tests." -ForegroundColor Yellow
        Write-Host "Available options:" -ForegroundColor Yellow
        Write-Host "  -ScheduledAction" -ForegroundColor Cyan
        Write-Host "  -ActionRelevanceValidator" -ForegroundColor Cyan
        Write-Host "  -All" -ForegroundColor Cyan
        exit 0
    }

    # Run all tests if -All is specified
    if ($All) {
        $ScheduledAction = $true
        $ActionRelevanceValidator = $true
    }

    # Run individual test suites
    if ($ScheduledAction) {
        Run-TestFilter -filter "ScheduledActionTests" -testSuiteName "SCHEDULED ACTION MODEL"
    }

    if ($ActionRelevanceValidator) {
        Run-TestFilter -filter "ActionRelevanceValidatorTests" -testSuiteName "ACTION RELEVANCE VALIDATOR"
    }

    Write-Host "`n🎉 All selected test suites completed successfully!" -ForegroundColor Green
} catch {
    Write-Host "`n❌ Test execution failed: $_" -ForegroundColor Red
    exit 1
}
