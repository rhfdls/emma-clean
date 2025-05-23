# Testing Strategy for Azure OpenAI SDK Integration

This document outlines the testing strategy for components that interact with the Azure OpenAI SDK. The goal is to ensure reliable tests that don't depend on the actual Azure OpenAI service while thoroughly validating our integration code.

## Table of Contents
- [Testing Philosophy](#testing-philosophy)
- [Mocking Strategy](#mocking-strategy)
- [Example Test Structure](#example-test-structure)
- [Best Practices](#best-practices)
- [Common Test Scenarios](#common-test-scenarios)
- [Troubleshooting](#troubleshooting)

## Testing Philosophy

1. **Isolation**: Tests should not make real API calls to Azure OpenAI.
2. **Determinism**: Tests should be deterministic and not depend on external services.
3. **Maintainability**: Tests should be easy to understand and maintain.
4. **Coverage**: Tests should cover both success and error scenarios.

## Mocking Strategy

### Key Principles

1. **Always Mock SDK Types**: Never directly instantiate Azure OpenAI SDK types in tests.
2. **Use Moq for All Mocks**: Create mocks for all SDK types that would normally interact with Azure.
3. **Mock Hierarchies**: Mock all levels of the response hierarchy (ChatCompletions → ChatChoice → ChatResponseMessage).
4. **Proper Interface Implementation**: Ensure mocks properly implement interfaces with all required members.

### Mocking Azure OpenAI SDK Types

#### 1. Mocking ChatResponseMessage

```csharp
var messageMock = new Mock<ChatResponseMessage>();
messageMock.SetupGet(m => m.Content).Returns("Test content");
messageMock.SetupGet(m => m.Role).Returns("assistant");
```

#### 2. Mocking ChatChoice

```csharp
var choiceMock = new Mock<ChatChoice>();
choiceMock.SetupGet(c => c.Message).Returns(messageMock.Object);
choiceMock.SetupGet(c => c.Index).Returns(0);
choiceMock.SetupGet(c => c.FinishReason).Returns("stop");
```

#### 3. Mocking IReadOnlyList<ChatChoice>

```csharp
var choicesList = new List<ChatChoice> { choiceMock.Object };
var choicesMock = new Mock<IReadOnlyList<ChatChoice>>();
choicesMock.Setup(c => c.Count).Returns(1);
choicesMock.Setup(c => c[0]).Returns(choiceMock.Object);
choicesMock.Setup(c => c.GetEnumerator()).Returns(choicesList.GetEnumerator());
choicesMock.As<IEnumerable<ChatChoice>>()
          .Setup(c => c.GetEnumerator())
          .Returns(choicesList.GetEnumerator());
```

#### 4. Mocking CompletionsUsage

```csharp
var usageMock = new Mock<CompletionsUsage>();
usageMock.SetupGet(u => u.CompletionTokens).Returns(1);
usageMock.SetupGet(u => u.PromptTokens).Returns(1);
usageMock.SetupGet(u => u.TotalTokens).Returns(2);
```

#### 5. Mocking ChatCompletions

```csharp
var chatCompletionsMock = new Mock<ChatCompletions>();
chatCompletionsMock.SetupGet(c => c.Id).Returns("test-completion-id");
chatCompletionsMock.SetupGet(c => c.Created).Returns(DateTimeOffset.UtcNow);
chatCompletionsMock.SetupGet(c => c.Choices).Returns(choicesMock.Object);
chatCompletionsMock.SetupGet(c => c.Usage).Returns(usageMock.Object);
```

#### 6. Creating the Response

```csharp
var responseMock = new Mock<Response>();
responseMock.SetupGet(r => r.Status).Returns(200);
return Response.FromValue(chatCompletionsMock.Object, responseMock.Object);
```

## Example Test Structure

```csharp
[Fact]
public async Task ProcessMessageAsync_WithValidMessage_ReturnsExpectedAction()
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

1. **Verify Method Calls**: Always verify that the expected methods were called with the correct parameters.
2. **Test Error Cases**: Include tests for error scenarios (network errors, invalid responses, etc.).
3. **Keep Tests Focused**: Each test should verify one specific behavior.
4. **Use Meaningful Test Names**: Test method names should clearly describe what's being tested.
5. **Clean Up Mocks**: Use a test cleanup method to reset mocks between tests if needed.

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

1. **Null Reference Exceptions**: Ensure all required properties are mocked.
2. **Incorrect Mock Setup**: Verify that the mock setup matches the actual method calls in the code.
3. **Missing Interface Implementations**: Ensure all interface members are properly mocked.

### Debugging Tips

1. Use `MockBehavior.Strict` to catch unexpected method calls.
2. Add logging to verify which methods are being called.
3. Check the inner exceptions for more detailed error information.

## See Also

- [Azure OpenAI SDK Documentation](https://docs.microsoft.com/en-us/azure/cognitive-services/openai/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [xUnit Documentation](https://xunit.net/)
