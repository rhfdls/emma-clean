using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using DotNetEnv;
using System.IO;

namespace Emma.OpenAI.Tests;

public class AzureOpenAIConfigTest
{
    [Fact]
    public async Task TestAzureOpenAIConfiguration()
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
            var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")?.TrimEnd('/');
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var deploymentName = Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT");

            // Validate configuration
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentNullException("OPENAI_ENDPOINT is not set in environment variables");
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException("OPENAI_API_KEY is not set in environment variables");
            if (string.IsNullOrEmpty(deploymentName))
                throw new ArgumentNullException("OPENAI_DEPLOYMENT is not set in environment variables");

            Console.WriteLine("🔍 Testing Azure OpenAI configuration...");
            Console.WriteLine($"Endpoint: {endpoint}");
            Console.WriteLine($"Deployment: {deploymentName}");
            Console.WriteLine($"API Key: {(string.IsNullOrEmpty(apiKey) ? "NOT SET" : "SET (redacted)")}");

            // Test the endpoint with a simple HTTP request
            using (var client = new HttpClient())
            {
                var apiVersion = "2023-05-15";
                var requestUri = $"{endpoint}/openai/deployments/{deploymentName}?api-version={apiVersion}";
                
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                Console.WriteLine($"\nSending request to: {requestUri}");
                
                try
                {
                    var response = await client.GetAsync(requestUri);
                    var content = await response.Content.ReadAsStringAsync();
                    
                    Console.WriteLine($"\nResponse Status: {(int)response.StatusCode} {response.StatusCode}");
                    Console.WriteLine("Response Headers:");
                    foreach (var header in response.Headers)
                    {
                        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                    }
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"\n❌ Request failed with status {response.StatusCode}");
                        Console.WriteLine($"Response: {content}");
                        throw new Exception($"Request failed with status {response.StatusCode}: {content}");
                    }
                    
                    Console.WriteLine($"\n✅ Success! Deployment '{deploymentName}' exists and is accessible.");
                    Console.WriteLine($"Response: {content}");
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"\n❌ HTTP Request failed: {httpEx.Message}");
                    if (httpEx.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {httpEx.InnerException.Message}");
                    }
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n❌ An error occurred while testing Azure OpenAI configuration");
            Console.WriteLine($"Error: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"\nStack Trace: {ex.StackTrace}");
            throw;
        }
    }
}
