# EMMA Testing Strategy

This document outlines the comprehensive testing strategy for the EMMA platform, with a focus on AI agent testing, industry-agnostic test patterns, and microservice integration testing.

## Table of Contents

- [Testing Philosophy](#testing-philosophy)
- [Testing Layers](#testing-layers)
- [AI Agent Testing](#ai-agent-testing)
- [Industry-Agnostic Test Patterns](#industry-agnostic-test-patterns)
- [Mocking Strategy](#mocking-strategy)
- [Integration Testing](#integration-testing)
- [Performance Testing](#performance-testing)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Testing Philosophy

1. **Isolation**: Tests should minimize external dependencies, especially for unit tests.
2. **Determinism**: Tests must be deterministic and not depend on external services or timing.
3. **Maintainability**: Tests should be well-structured, documented, and easy to update.
4. **Coverage**: Aim for high test coverage, especially for business logic and AI decision paths.
5. **Performance**: Tests should execute quickly to enable fast feedback loops.
6. **Industry-Agnostic**: Test patterns should work across different industry verticals.

## Testing Layers

EMMA employs a multi-layered testing strategy:

```mermaid
graph TD
    A[Unit Tests] -->|Fastest, most isolated|
    B[Integration Tests] -->|Test service interactions|
    C[Contract Tests] -->|Verify API contracts|
    D[End-to-End Tests] -->|Full system validation|
    E[Performance Tests] -->|Validate under load|
```

### 1. Unit Tests

- Test individual components in isolation
- Mock all external dependencies
- Focus on business logic and decision points
- Fast execution (milliseconds per test)

### 2. Integration Tests

- Test interactions between components
- Use test containers for databases and services
- Include AI model integration tests with mocked responses
- Medium execution speed (seconds per test)

### 3. Contract Tests

- Verify API contracts between services
- Use Pact or similar tools
- Ensure backward compatibility

### 4. End-to-End Tests

- Test complete user journeys
- Include UI automation where applicable
- Run against staging environments
- Slower execution (minutes per test)

### 5. Performance Tests

- Load testing for critical paths
- Stress testing for failure scenarios
- AI model performance benchmarking

## Testing Layers

EMMA employs a multi-layered testing strategy:

```mermaid
graph TD
    A[Unit Tests] -->|Fastest, most isolated|
    B[Integration Tests] -->|Test service interactions|
    C[Contract Tests] -->|Verify API contracts|
    D[End-to-End Tests] -->|Full system validation|
    E[Performance Tests] -->|Validate under load|
```

### 1. Unit Tests

- Test individual components in isolation
- Mock all external dependencies
- Focus on business logic and decision points
- Fast execution (milliseconds per test)

### 2. Integration Tests

- Test interactions between components
- Use test containers for databases and services
- Include AI model integration tests with mocked responses
- Medium execution speed (seconds per test)

### 3. Contract Tests

- Verify API contracts between services
- Use Pact or similar tools
- Ensure backward compatibility

### 4. End-to-End Tests

- Test complete user journeys
- Include UI automation where applicable
- Run against staging environments
- Slower execution (minutes per test)

### 5. Performance Tests

- Load testing for critical paths
- Stress testing for failure scenarios
- AI model performance benchmarking

## Testing Layers

EMMA employs a multi-layered testing strategy:

```mermaid
graph TD
    A[Unit Tests] -->|Fastest, most isolated|
    B[Integration Tests] -->|Test service interactions|
    C[Contract Tests] -->|Verify API contracts|
    D[End-to-End Tests] -->|Full system validation|
    E[Performance Tests] -->|Validate under load|
```

### 1. Unit Tests
- Test individual components in isolation
- Mock all external dependencies
- Focus on business logic and decision points
- Fast execution (milliseconds per test)

### 2. Integration Tests
- Test interactions between components
- Use test containers for databases and services
- Include AI model integration tests with mocked responses
- Medium execution speed (seconds per test)

### 3. Contract Tests
- Verify API contracts between services
- Use Pact or similar tools
- Ensure backward compatibility

### 4. End-to-End Tests
- Test complete user journeys
- Include UI automation where applicable
- Run against staging environments
- Slower execution (minutes per test)

### 5. Performance Tests
- Load testing for critical paths
- Stress testing for failure scenarios
- AI model performance benchmarking

## AI Agent Testing

AI agents require specialized testing approaches to validate their behavior and decision-making capabilities, especially in multi-tenant environments with user override support.

### Test Categories

1. **Unit Tests**
   - Test individual agent components in isolation
   - Mock external dependencies (LLM, databases, etc.)
   - Focus on business logic and decision boundaries
   - Test tenant isolation and data separation
   - Verify override mode behavior (AlwaysAsk, NeverAsk, LLMDecision, RiskBased)

2. **Integration Tests**
   - Test agent interactions with other services
   - Validate data flow between components with tenant context
   - Test error handling and recovery scenarios
   - Verify cross-tenant data isolation
   - Test approval workflows for overrides

3. **End-to-End Tests**
   - Test complete agent workflows across tenants
   - Validate input/output transformations with tenant context
   - Test with real or realistic data
   - Verify audit logging and compliance requirements
   - Test tenant-specific configurations and overrides

### Example: Testing NBA (Next Best Action) Logic

```csharp
[Theory]
[InlineData("real-estate", "new-lead", "schedule-showing")]
[InlineData("mortgage", "pre-approved", "send-application")]
[InlineData("insurance", "policy-renewal", "schedule-call")]
public async Task GetNextBestAction_ReturnsExpectedActionForIndustry(
    string industry,
    string contactState,
    string expectedAction)
{
    // Arrange
    var contact = new Contact { 
        Industry = industry,
        State = contactState,
        LastInteraction = DateTime.UtcNow.AddDays(-1)
    };
    
    // Act
    var result = await _nbaService.GetNextBestAction(contact);
    
    // Assert
    Assert.Equal(expectedAction, result.ActionType);
## Multi-tenant Testing

Multi-tenant environments require additional test coverage to ensure proper isolation and performance.

### Test Scenarios

1. **Tenant Isolation**
   - Verify data separation between tenants
   - Test cross-tenant access controls
   - Validate tenant-specific configurations

2. **User Override Workflows**
   - Test approval workflows for different override modes
   - Verify audit logging of all override actions
   - Test permission models for override approvals

## Performance Testing

Performance testing ensures the system can handle expected loads across multiple tenants.

### Test Scenarios

1. **Load Testing**
   - Test with expected concurrent users per tenant
   - Measure response times under load with tenant isolation
   - Identify bottlenecks in multi-tenant scenarios
   - Test tenant onboarding performance

2. **Stress Testing**
   - Test beyond expected load with tenant distribution
   - Identify breaking points with mixed tenant workloads
   - Test recovery mechanisms with tenant awareness
   - Verify no tenant can impact others' performance

## Best Practices

1. **Test Organization**
   - Group tests by feature/domain, not by project structure
   - Use consistent naming: `MethodName_StateUnderTest_ExpectedBehavior`
   - Keep tests focused on a single behavior

2. **Test Data Management**
   - Use test data builders for complex objects
   - Centralize test data generation
   - Make tests deterministic with fixed seeds for random data

3. **AI-Specific Testing**
   - Test prompt templates separately from model integration
   - Use snapshot testing for prompt outputs
   - Validate structured outputs with JSON Schema

4. **Performance Testing**
   - Run performance tests in a consistent environment
   - Establish baseline metrics
   - Monitor for regressions in CI/CD

5. **Test Maintenance**
   - Keep tests DRY (Don't Repeat Yourself)
   - Regularly review and update test data
   - Remove or update flaky tests

## Troubleshooting

### Common Issues

#### 1. Flaky Tests
- **Symptom**: Tests pass inconsistently
- **Cause**: Shared state between tests, timing issues
- **Solution**: 
  - Ensure tests are isolated
  - Use test fixtures for shared setup
  - Add retry logic for known flaky tests

#### 2. Slow Test Execution
- **Symptom**: Test suite takes too long to run
- **Cause**: Heavy setup, real network calls
- **Solution**:
  - Mock external dependencies
  - Use in-memory databases for testing
  - Run tests in parallel when possible

#### 3. AI Model Inconsistencies
- **Symptom**: Tests fail due to non-deterministic AI responses
- **Solution**:
  - Mock AI service responses
  - Test prompt templates separately
  - Use assertion libraries that support fuzzy matching

#### 4. Debugging Test Failures

```bash
# Run a single test with detailed output
dotnet test --filter "FullyQualifiedName=Your.Namespace.YourTestClass.YourTestMethod" --logger "console;verbosity=detailed"

# Debug test in VS Code
{
    "name": ".NET Test Launch (console) (performance)",
    "type": "coreclr",
    "request": "launch",
    "preLaunchTask": "build-debug",
    "program": "dotnet",
    "args": [
        "test",
        "--no-build",
        "--filter",
        "FullyQualifiedName~YourTestClass",
        "--logger:console;verbosity=detailed"
    ],
    "cwd": "${workspaceFolder}",
    "console": "integratedTerminal",
    "stopAtEntry": false
}
```

### Performance Optimization

1. **Test Data Optimization**
   - Use the minimum data required for each test
   - Share setup between tests when possible
   - Clean up test data after each test

2. **Parallel Test Execution**
   - Mark test classes with `[Collection]` to control parallelization
   - Use `[CollectionDefinition(DisableParallelization = true)]` for tests that can't run in parallel

3. **CI/CD Integration**
   - Run fast unit tests on every commit
   - Run integration and performance tests on schedule or before releases
   - Set up performance gates to catch regressions

### Monitoring and Reporting

1. **Test Coverage**
   - Track code coverage metrics
   - Set minimum coverage thresholds
   - Identify untested code paths

2. **Test Results**
   - Publish test results in CI/CD
   - Set up alerts for test failures
   - Track flaky tests

3. **Performance Metrics**
   - Track test execution time
   - Monitor memory usage
   - Set performance budgets

By following these guidelines and patterns, you can create a robust test suite that helps maintain the quality and reliability of the EMMA platform across all industry verticals.
{
    // Arrange
    var message = "Test message";
    var expectedAction = new EmmaAction
    {
        Action = EmmaActionType.SendEmail,
        Payload = "Test payload"
    };
    
    var responseContent = new 
    {
        action = "sendemail",
        payload = expectedAction.Payload
    };
    
    var responseJson = JsonSerializer.Serialize(responseContent);
    var response = CreateMockResponse(responseJson);
    
    _openAIClientMock
        .Setup(x => x.GetChatCompletionsAsync(
            It.Is<ChatCompletionsOptions>(o => 
                o.Messages != null &&
                o.Messages.Count == 2 && 
                o.Messages[0].Role == "system" &&
                o.Messages[1].Role == "user" &&
                o.Messages[1].Content == message),
            default))
        .ReturnsAsync(response);

    // Act
    var result = await _service.ProcessMessageAsync(message);

    // Assert
    Assert.NotNull(result);
    Assert.True(result.Success);
    Assert.Equal(expectedAction.Action, result.Action?.Action);
    Assert.Equal(expectedAction.Payload, result.Action?.Payload);
    
    // Verify the OpenAI client was called with the expected parameters
    _openAIClientMock.Verify(
        x => x.GetChatCompletionsAsync(
            It.Is<ChatCompletionsOptions>(o => 
                o.Messages != null &&
                o.Messages.Count == 2), 
            default),
        Times.Once);
}
```

## Best Practices

1. **Verify Method Calls**
   Always verify that the expected methods were called with the correct parameters

2. **Test Error Cases**
   Include tests for error scenarios (network errors, invalid responses, etc.)

3. **Keep Tests Focused**
   Each test should verify one specific behavior

4. **Use Meaningful Test Names**
   Test method names should clearly describe what's being tested

5. **Clean Up Mocks**
   Use a test cleanup method to reset mocks between tests if needed

## Common Test Scenarios

### Testing Successful API Calls

- Verify the response is correctly processed
- Verify the correct methods were called on the mocks
- Verify the returned data matches expectations

### Testing Error Cases

- Network errors
- Invalid responses
- Rate limiting
- Authentication failures

### Testing Edge Cases

- Empty responses
- Malformed JSON
- Unexpected response formats

## Troubleshooting

### Common Issues

1. **Null Reference Exceptions**
   Ensure all required properties are mocked

2. **Incorrect Mock Setup**
   Verify that the mock setup matches the actual method calls in the code

3. **Missing Interface Implementations**
   Ensure all interface members are properly mocked

### Debugging Tips

1. Use `MockBehavior.Strict` to catch unexpected method calls
2. Add logging to verify which methods are being called
3. Check the inner exceptions for more detailed error information

## See Also

- [Azure OpenAI SDK Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [xUnit Documentation](https://xunit.net/)
