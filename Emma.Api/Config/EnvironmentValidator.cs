namespace Emma.Api.Config;

/// <summary>
/// Validates required environment variables at application startup
/// </summary>
public static class EnvironmentValidator
{
    /// <summary>
    /// Validates all required environment variables
    /// </summary>
    /// <param name="logger">Logger for validation messages</param>
    /// <returns>True if all required variables are valid</returns>
    public static bool ValidateEnvironment(ILogger logger)
    {
        var isValid = true;

        // Azure OpenAI validation
        isValid &= ValidateAzureOpenAI(logger);

        // Database validation
        isValid &= ValidateDatabase(logger);

        return isValid;
    }

    private static bool ValidateAzureOpenAI(ILogger logger)
    {
        var isValid = true;

        // Check Azure OpenAI endpoint
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            logger.LogError("AZURE_OPENAI_ENDPOINT environment variable is required");
            isValid = false;
        }
        else if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            logger.LogError("AZURE_OPENAI_ENDPOINT must be a valid URL");
            isValid = false;
        }

        // Check Azure OpenAI API key
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogError("AZURE_OPENAI_API_KEY environment variable is required");
            isValid = false;
        }

        // Check chat deployment name
        var chatDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_CHAT_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(chatDeployment))
        {
            logger.LogError("AZURE_OPENAI_CHAT_DEPLOYMENT environment variable is required");
            isValid = false;
        }

        // Check embedding deployment name
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(embeddingDeployment))
        {
            logger.LogError("AZURE_OPENAI_EMBEDDING_DEPLOYMENT environment variable is required");
            isValid = false;
        }

        // Validate optional numeric settings
        var maxTokensStr = Environment.GetEnvironmentVariable("AZURE_OPENAI_MAX_TOKENS");
        if (!string.IsNullOrWhiteSpace(maxTokensStr) && !int.TryParse(maxTokensStr, out var maxTokens))
        {
            logger.LogError("AZURE_OPENAI_MAX_TOKENS must be a valid integer");
            isValid = false;
        }

        var temperatureStr = Environment.GetEnvironmentVariable("AZURE_OPENAI_TEMPERATURE");
        if (!string.IsNullOrWhiteSpace(temperatureStr) && !float.TryParse(temperatureStr, out var temperature))
        {
            logger.LogError("AZURE_OPENAI_TEMPERATURE must be a valid float");
            isValid = false;
        }

        if (isValid)
        {
            logger.LogInformation("Azure OpenAI environment variables validated successfully");
        }

        return isValid;
    }

    private static bool ValidateDatabase(ILogger logger)
    {
        var connectionString = Environment.GetEnvironmentVariable("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError("DefaultConnection environment variable is required");
            return false;
        }

        logger.LogInformation("Database environment variables validated successfully");
        return true;
    }
}
