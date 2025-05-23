using Azure;
using Azure.AI.OpenAI;
using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using DotNetEnv;

namespace Emma.OpenAI.Tests;

public class OpenAIConnectionTests
{
    [Fact]
    public async Task TestOpenAIConnection()
    {
        try
        {
            // Load environment variables from the local.env file in the solution root
            var envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, "local.env");
            
            if (!File.Exists(envPath))
            {
                throw new FileNotFoundException($"Environment file not found at: {envPath}");
            }
            
            Console.WriteLine($"Loading environment from: {envPath}");
            Env.Load(envPath);

            // Get environment variables
            var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var deploymentName = Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT");

            // Validate configuration
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException("OPENAI_ENDPOINT is not set in environment variables");
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("OPENAI_API_KEY is not set in environment variables");
            if (string.IsNullOrEmpty(deploymentName))
                throw new ArgumentNullException("OPENAI_DEPLOYMENT is not set in environment variables");

            Console.WriteLine("🔍 Testing OpenAI connection...");
            Console.WriteLine($"Endpoint: {endpoint}");
            Console.WriteLine($"Deployment: {deploymentName}");
            
            // Log the configuration being used (redacted for security)
            Console.WriteLine($"Using endpoint: {endpoint}");
            Console.WriteLine($"Using deployment: {deploymentName}");
            Console.WriteLine($"API Key: {(string.IsNullOrEmpty(apiKey) ? "NOT SET" : "SET (redacted)")}");

            // Create client with Azure OpenAI configuration
            var clientOptions = new OpenAIClientOptions()
            {
                Diagnostics = {
                    IsLoggingContentEnabled = true,
                    ApplicationId = "EmmaAITest"
                }
            };
            
            // Ensure the endpoint ends with a slash
            if (!endpoint.EndsWith("/"))
            {
                endpoint += "/";
            }

            // For Azure OpenAI, the endpoint should be the base URL (e.g., https://your-resource-name.openai.azure.com/)
            var client = new OpenAIClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey),
                clientOptions);

            // Create chat completion options
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
            
            // Make the API call
            Console.WriteLine("Sending request to Azure OpenAI...");
            var response = await client.GetChatCompletionsAsync(chatCompletionsOptions);
            
            if (response?.Value?.Choices?.Count == 0)
            {
                throw new InvalidOperationException("No choices returned from the API");
            }
            
            var responseMessage = response.Value.Choices[0].Message.Content;
            
            Console.WriteLine("\n✅ Connection successful!");
            Console.WriteLine($"Response: {responseMessage}");
            
            // Assert that we got a response
            Assert.NotNull(responseMessage);
            Assert.NotEmpty(responseMessage);
        }
        catch (RequestFailedException rfex) when (rfex.Status == 401)
        {
            Console.WriteLine("\n❌ Authentication failed. Please check your API key and endpoint.");
            Console.WriteLine($"Status: {rfex.Status}");
            Console.WriteLine($"Error Code: {rfex.ErrorCode}");
            Console.WriteLine($"Message: {rfex.Message}");
            throw;
        }
        catch (RequestFailedException rfex)
        {
            Console.WriteLine($"\n❌ Request failed with status {rfex.Status}");
            Console.WriteLine($"Error Code: {rfex.ErrorCode}");
            Console.WriteLine($"Message: {rfex.Message}");
            Console.WriteLine($"Details: {rfex.ToString()}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n❌ An unexpected error occurred");
            Console.WriteLine($"Error: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"\nStack Trace: {ex.StackTrace}");
            throw;
        }
    }
}
