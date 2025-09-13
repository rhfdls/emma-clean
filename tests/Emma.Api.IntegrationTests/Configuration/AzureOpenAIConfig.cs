namespace Emma.Api.IntegrationTests.Configuration
{
    /// <summary>
    /// Minimal configuration DTO for Azure OpenAI tests. Matches appsettings.Test.json structure.
    /// </summary>
    public class AzureOpenAIConfig
    {
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
        public string? DeploymentName { get; set; }
    }
}
