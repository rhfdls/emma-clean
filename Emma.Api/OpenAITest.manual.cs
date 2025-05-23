using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;

namespace Emma.Api.TestUtilities;

/// <summary>
/// A simple console application to test the OpenAI connection.
/// This is a separate utility and not part of the main application.
/// </summary>
public class OpenAITest
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<OpenAITest>()
                .Build();

            var endpoint = new Uri(Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? 
                                 config["OPENAI_ENDPOINT"] ?? 
                                 throw new InvalidOperationException("OPENAI_ENDPOINT is not configured"));
            
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                        config["OPENAI_API_KEY"] ??
                        throw new InvalidOperationException("OPENAI_API_KEY is not configured");
                        
            var key = new AzureKeyCredential(apiKey);
            
            var deploymentName = Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT") ?? 
                              config["OPENAI_DEPLOYMENT"] ??
                              throw new InvalidOperationException("OPENAI_DEPLOYMENT is not configured");

            var client = new OpenAIClient(endpoint, key);

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant."),
                    new ChatRequestUserMessage("Say 'Connection successful'")
                },
                MaxTokens = 50
            };

            Console.WriteLine("🔍 Testing OpenAI connection...");
            Console.WriteLine($"Endpoint: {endpoint}");
            Console.WriteLine($"Deployment: {deploymentName}");
            
            Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            
            Console.WriteLine("\n✅ Connection successful!");
            Console.WriteLine("Response: " + response.Value.Choices[0].Message.Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Connection failed: {ex.Message}");
            Console.WriteLine("Error: " + ex.Message);
            Console.WriteLine("\nStack Trace: " + ex.StackTrace);
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
