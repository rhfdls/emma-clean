# EMMA Test Debug Workflow

## Purpose
Stepwise approach for debugging test failures in EMMA, complementing the build debug workflow.

## Workflow Steps

### Step 1: Test Execution Assessment
- Run tests in focused batches: `dotnet test --filter Category=Unit`
- Analyze ONLY the first 3-5 test failures
- Categorize failure types before attempting fixes
- Create memory of failure patterns

### Step 2: Test Failure Categories
- **Unit Test Failures**: Logic errors, assertion failures
- **Integration Test Failures**: Database, API, service integration issues
- **Configuration Test Failures**: Settings, connection strings, environment
- **Architecture Test Failures**: Interface changes, dependency injection

### Step 3: Focused Test Fixing
- Fix tests by category, not all at once:
  1. **Critical unit tests** (core business logic)
  2. **Integration tests** (database, API endpoints)
  3. **Configuration tests** (settings validation)
  4. **Architecture tests** (DI, interfaces)

### Step 4: Incremental Test Validation
- After fixing 3-5 tests, run that specific test category
- Verify fixes don't break other tests
- Document test fix reasoning and approach
- Create targeted memory for complex fixes

### Step 5: Test-Specific Memory Management
- Create memories for:
  - Common test failure patterns
  - Successful test fix strategies
  - Mock and setup configurations
  - Database test data requirements

### Step 6: Test Iteration Control
- If more than 10 test failures remain, create checkpoint
- Run full test suite only after category fixes complete
- Never attempt to fix more than 5 test failures per iteration
- Maintain test isolation and independence

## Key Principles

### Test Quality
- Ensure tests validate actual business requirements
- Maintain test independence and repeatability
- Verify interaction terminology consistency in tests
- Keep test data and mocks realistic

### Debugging Efficiency
- Use test output and logs for targeted debugging
- Focus on root causes, not symptoms
- Maintain test coverage while fixing failures
- Preserve existing test patterns and conventions

## Usage
Invoke with: `/emma-test-debug`

## Example Invocation
```
EMMA tests are failing after recent changes. Let's debug systematically:
/emma-test-debug
```

## Success Criteria
- All critical tests pass
- Test coverage maintained or improved
- No test interdependencies created
- Test execution time reasonable
- Architecture consistency validated through tests
