# EMMA API Integration Tests

This project contains integration tests for the EMMA API, focusing on testing the interaction with external services like Azure OpenAI.

## Azure OpenAI Integration Tests

To run the Azure OpenAI integration tests, you'll need to configure the following:

1. Copy `appsettings.Development.template.json` to `appsettings.Development.json`
2. Update the values in `appsettings.Development.json` with your Azure OpenAI credentials:
   ```json
   {
     "AzureOpenAI": {
       "ApiKey": "your-api-key",
       "Endpoint": "https://your-resource-name.openai.azure.com/",
       "DeploymentName": "your-deployment-name",
       "ApiVersion": "2023-05-15"
     }
   }
   ```
3. Set the `DOTNET_ENVIRONMENT` environment variable to `Development` to use the development settings

### Running Azure OpenAI Tests

By default, the Azure OpenAI integration tests are skipped unless the service is properly configured. To run them:

```bash
dotnet test --filter "FullyQualifiedName~Emma.Api.IntegrationTests.Services.EmmaAgentServiceIntegrationTests"
```

### Running All Tests

To run all tests (unit and integration):

```bash
dotnet test
```

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extensions
- Azure Functions Core Tools (if testing Azure Functions)
- Azure OpenAI service (for integration tests)

## Running Tests

### From Command Line

```bash
dotnet test
```

### From Visual Studio

1. Open the solution in Visual Studio
2. Open Test Explorer (Test > Test Explorer)
3. Click "Run All Tests" or run individual tests

## Test Categories

### Unit Tests
- Test individual components in isolation
- Fast execution
- No external dependencies

### Integration Tests
- Test interactions between components
- May use test doubles for external services
- Slower than unit tests

## Test Data

Test data is managed using the `TestData` class and embedded resources. This keeps test data separate from test logic.

## Best Practices

- **Naming Conventions**:
  - Test classes: `{ClassUnderTest}Tests`
  - Test methods: `{MethodUnderTest}_{Scenario}_{ExpectedResult}`

- **Arrange-Act-Assert**:
  - **Arrange**: Set up test data and dependencies
  - **Act**: Execute the code under test
  - **Assert**: Verify the results

## Debugging Tests

1. Set breakpoints in your test methods
2. Right-click the test in Test Explorer
3. Select "Debug"

## Code Coverage

To generate a code coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Continuous Integration

Tests are automatically run in the CI/CD pipeline. See the `.github/workflows` directory for configuration details.

## Troubleshooting

- **Tests failing with connection issues**: Ensure all required services are running
- **Missing dependencies**: Run `dotnet restore` to restore NuGet packages
- **Test data issues**: Verify the test data in the `TestData` class

## License

This project is licensed under the terms of the MIT license.
