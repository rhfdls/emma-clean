using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure;
using Azure.AI.OpenAI;
using Emma.Core.Config;
using Emma.Core.Dtos;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;

namespace Emma.Api.Services
{

    public class EmmaAgentService : IEmmaAgentService
    {
        private const string SystemPrompt = """
    You are EMMA (Estate Management & Marketing Assistant), an AI assistant for real estate professionals.
    Your role is to analyze real estate-related conversations and determine the appropriate actions to take.

    Available Actions:
    - sendemail: Send an email to the client
    - schedulefollowup: Schedule a follow-up task
    - none: No action needed at this time

    For each message, respond with a JSON object in this format:
    {
        "action": "sendemail|schedulefollowup|none",
        "payload": "string"  // Details about the action to take
    }

    Examples:
    1. For scheduling a follow-up:
    {
        "action": "schedulefollowup",
        "payload": "Follow up with client about property viewing on Friday at 2 PM"
    }

    2. For sending an email:
    {
        "action": "sendemail",
        "payload": "Subject: Property Details for 123 Main St\n\nHi [Client],\n\nHere are the details you requested about 123 Main St..."
    }

    3. When no action is needed:
    {
        "action": "none",
        "payload": ""
    }
    """;

        private readonly ILogger<EmmaAgentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OpenAIClient _openAIClient;
        private readonly AzureOpenAIConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmmaAgentService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="httpContextAccessor">HTTP context accessor for request context</param>
        /// <param name="openAIClient">Azure OpenAI client</param>
        /// <param name="config">Azure OpenAI configuration</param>
        public EmmaAgentService(
            ILogger<EmmaAgentService> logger,
            IHttpContextAccessor httpContextAccessor,
            OpenAIClient openAIClient,
            IOptions<AzureOpenAIConfig> config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.DeploymentName))
            {
                throw new InvalidOperationException("Azure OpenAI deployment name is not configured");
            }
        }

        private string GetCorrelationId()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.Request.Headers.TryGetValue("X-Correlation-ID", out var headerValues) == true && 
                    headerValues.Count > 0 && 
                    !string.IsNullOrWhiteSpace(headerValues[0]))
                {
                    return headerValues[0]!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get correlation ID from headers");
            }
            
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Processes an incoming message and returns a response.
        /// </summary>
        /// <param name="message">The message to process</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        public async Task<EmmaResponseDto> ProcessMessageAsync(string message)
        {
            var correlationId = GetCorrelationId();
            _logger.LogInformation("Processing message. Correlation ID: {CorrelationId}", correlationId);
            
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Empty message received");
                    return EmmaResponseDto.ErrorResponse("Message cannot be empty", correlationId);
                }

                if (_openAIClient == null || _config == null)
                {
                    _logger.LogError("Azure OpenAI client or configuration is not available. Client: {Client}, Config: {Config}", 
                        _openAIClient != null ? "Available" : "Null", 
                        _config != null ? "Available" : "Null");
                    return EmmaResponseDto.ErrorResponse("AI service is not available", correlationId);
                }
                
                _logger.LogInformation("Using Azure OpenAI endpoint: {Endpoint}, Deployment: {Deployment}", 
                    _config.Endpoint, _config.DeploymentName);

                _logger.LogDebug("Sending message to Azure OpenAI: {Message}", message);
                
                // Create chat completion options using the latest SDK
                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    DeploymentName = _config.DeploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(SystemPrompt),
                        new ChatRequestUserMessage(message)
                    },
                    Temperature = 0.3f,  // Lower temperature for more focused and deterministic responses
                    MaxTokens = 500,
                    ResponseFormat = ChatCompletionsResponseFormat.JsonObject
                };

                // Get response from Azure OpenAI
                _logger.LogInformation("Sending request to Azure OpenAI...");
                try
                {
                    var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                    
                    _logger.LogInformation("Received response from Azure OpenAI. Status: {Status}", response.GetRawResponse().Status);
                    
                    if (response?.Value?.Choices == null || response.Value.Choices.Count == 0)
                    {
                        _logger.LogError("No response choices returned from Azure OpenAI");
                        return EmmaResponseDto.ErrorResponse("No response from AI service", correlationId);
                    }
                    
                    var choice = response.Value.Choices[0];
                    if (choice?.Message?.Content == null)
                    {
                        _logger.LogError("Invalid response format from Azure OpenAI");
                        return EmmaResponseDto.ErrorResponse("Invalid response format from AI service", correlationId);
                    }
                    
                    var responseContent = choice.Message.Content;
                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        _logger.LogError("Empty response content received from Azure OpenAI");
                        return EmmaResponseDto.ErrorResponse("Empty response from AI service", correlationId);
                    }
                    
                    _logger.LogDebug("Received response from Azure OpenAI: {Response}", responseContent);
                    
                    // Parse the output as JSON
                    var emmaAction = EmmaAction.FromJson(responseContent);
                    if (emmaAction == null)
                    {
                        _logger.LogError("Failed to parse AI response into EmmaAction. Output: {Output}", responseContent);
                        return EmmaResponseDto.ErrorResponse("Invalid response format from AI service", correlationId);
                    }
                    
                    _logger.LogInformation("Successfully processed message. Action: {Action}", emmaAction.Action);
                    return EmmaResponseDto.SuccessResponse(emmaAction, responseContent, correlationId);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, "Azure OpenAI request failed. Status: {Status}, Error: {Error}", ex.Status, ex.Message);
                    return EmmaResponseDto.ErrorResponse($"AI service request failed: {ex.Message}", correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error calling Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse($"Unexpected error: {ex.Message}", correlationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                return EmmaResponseDto.ErrorResponse("An unexpected error occurred: " + ex.Message, correlationId, null);
            }
        }
    }

    // Agent interfaces are defined in Emma.Core.Interfaces
}

