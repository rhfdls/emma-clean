using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Emma.Core.Config;
using Emma.Core.Dtos;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

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
        
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        
        private static readonly AsyncRetryPolicy _retryPolicy = Policy
            .Handle<RequestFailedException>(ex => ex.Status == (int)HttpStatusCode.TooManyRequests || ex.Status >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, retryCount, context) => 
                {
                    var logger = (ILogger)context["logger"];
                    logger?.LogWarning(ex, "Retry {RetryCount} after {Delay}ms due to: {Message}", 
                        retryCount, delay.TotalMilliseconds, ex.Message);
                });

        private readonly ILogger<EmmaAgentService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OpenAIClient _openAIClient;
        private readonly IOptions<AzureOpenAIConfig> _configOptions;
        private AzureOpenAIConfig Config => _configOptions.Value;

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
            _configOptions = config ?? throw new ArgumentNullException(nameof(config));
            
            // Validate configuration on service creation
            if (!Config.IsValid())
            {
                var validationErrors = new List<string>();
                if (string.IsNullOrEmpty(Config.ApiKey)) validationErrors.Add("ApiKey is required");
                if (string.IsNullOrEmpty(Config.Endpoint)) validationErrors.Add("Endpoint is required");
                if (string.IsNullOrEmpty(Config.DeploymentName)) validationErrors.Add("DeploymentName is required");
                if (Config.Temperature < 0 || Config.Temperature > 2) validationErrors.Add("Temperature must be between 0.0 and 2.0");
                
                throw new InvalidOperationException($"Invalid Azure OpenAI configuration: {string.Join(", ", validationErrors)}");
            }
            
            _logger.LogInformation("EmmaAgentService initialized with deployment: {DeploymentName}", Config.DeploymentName);
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
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
        public async Task<EmmaResponseDto> ProcessMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            var correlationId = GetCorrelationId();
            _logger.LogInformation("Processing message. Correlation ID: {CorrelationId}", correlationId);
            
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    _logger.LogWarning("Empty message received");
                    return EmmaResponseDto.ErrorResponse("Message cannot be empty", correlationId, null);
                }

                // Log request details for debugging
                _logger.LogInformation("Processing message with {Length} characters. Correlation ID: {CorrelationId}", 
                    message?.Length ?? 0, correlationId);

                // Create chat completion options
                var chatCompletionsOptions = new ChatCompletionsOptions
                {
                    DeploymentName = Config.DeploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(SystemPrompt),
                        new ChatRequestUserMessage(message)
                    },
                    Temperature = Config.Temperature,
                    MaxTokens = Config.MaxTokens,
                    NucleusSamplingFactor = Config.TopP,
                    ResponseFormat = ChatCompletionsResponseFormat.JsonObject
                };

                // Execute with retry policy
                var context = new Context
                {
                    ["logger"] = _logger,
                    ["deployment"] = Config.DeploymentName,
                    ["correlationId"] = correlationId
                };

                try
                {
                    try
                    {
                        var response = await _retryPolicy.ExecuteAsync(
                            action: async (ctx, ct) =>
                            {
                                _logger.LogDebug("Sending request to Azure OpenAI deployment: {Deployment}", Config.DeploymentName);
                                var result = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions, ct);
                                if (result == null)
                                {
                                    throw new InvalidOperationException("Received null response from Azure OpenAI");
                                }
                                return result;
                            },
                            context: context,
                            continueOnCapturedContext: false,
                            cancellationToken: cancellationToken);

                        return await ProcessSuccessfulResponse(response, correlationId);
                    }
                    catch (Exception ex) when (ex is not RequestFailedException)
                    {
                        _logger.LogError(ex, "Error processing message. Correlation ID: {CorrelationId}", correlationId);
                        return EmmaResponseDto.ErrorResponse("An error occurred while processing your request", correlationId, null);
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Unauthorized)
                {
                    _logger.LogError(ex, "Authentication failed for Azure OpenAI. Check your API key and endpoint");
                    return EmmaResponseDto.ErrorResponse("Authentication failed for AI service", correlationId, null);
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
                {
                    _logger.LogError(ex, "Azure OpenAI deployment not found: {Deployment}", Config.DeploymentName);
                    return EmmaResponseDto.ErrorResponse($"AI model deployment '{Config.DeploymentName}' not found", correlationId, null);
                }
                catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.TooManyRequests)
                {
                    _logger.LogError(ex, "Rate limit exceeded for Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse("AI service is currently overloaded. Please try again later.", correlationId, null);
                }
                catch (RequestFailedException ex)
                {
                    _logger.LogError(ex, "Azure OpenAI request failed. Status: {Status}, Error: {Error}", ex.Status, ex.Message);
                    return EmmaResponseDto.ErrorResponse($"AI service error: {ex.Message}", correlationId, null);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Request was cancelled by the client. Correlation ID: {CorrelationId}", correlationId);
                    return EmmaResponseDto.ErrorResponse("Request was cancelled", correlationId, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing message. Correlation ID: {CorrelationId}", correlationId);
                    return EmmaResponseDto.ErrorResponse("An unexpected error occurred while processing your request", correlationId, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                return EmmaResponseDto.ErrorResponse("An unexpected error occurred: " + ex.Message, correlationId, null);
            }
        }

        /// <summary>
        /// Processes a successful response from Azure OpenAI.
        /// </summary>
        /// <param name="response">The response from Azure OpenAI.</param>
        /// <param name="correlationId">The correlation ID for the request.</param>
        /// <returns>An <see cref="EmmaResponseDto"/> containing the processed response.</returns>
        private async Task<EmmaResponseDto> ProcessSuccessfulResponse(
            Azure.Response<Azure.AI.OpenAI.ChatCompletions> response, 
            string correlationId)
        {
            try
            {
                if (response == null)
                {
                    _logger.LogError("Null response received from Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse("No response received from AI service", correlationId, null);
                }
                
                if (response.Value == null)
                {
                    _logger.LogError("Null value in response from Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse("Invalid response from AI service", correlationId, null);
                }

                _logger.LogInformation(
                    "Received response from Azure OpenAI. Status: {Status}, Request ID: {RequestId}", 
                    response.GetRawResponse().Status,
                    response.GetRawResponse().ClientRequestId);
                
                if (response.Value?.Choices == null || response.Value.Choices.Count == 0)
                {
                    _logger.LogError("No response choices returned from Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse("No response from AI service", correlationId, null);
                }
                
                var choice = response.Value.Choices[0];
                if (choice?.Message?.Content == null)
                {
                    _logger.LogError("Invalid response format from Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse("Invalid response format from AI service", correlationId, null);
                }
                
                var responseContent = choice.Message.Content;
                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    _logger.LogError("Empty response content received from Azure OpenAI");
                    return EmmaResponseDto.ErrorResponse("Empty response from AI service", correlationId, null);
                }
                
                _logger.LogDebug("Received response from Azure OpenAI: {Response}", responseContent);
                
                // Parse the output as JSON
                var emmaAction = EmmaAction.FromJson(responseContent);
                if (emmaAction == null)
                {
                    _logger.LogError("Failed to parse AI response into EmmaAction. Output: {Output}", responseContent);
                    return EmmaResponseDto.ErrorResponse("Invalid response format from AI service", correlationId, responseContent);
                }
                
                _logger.LogInformation("Successfully processed message. Action: {Action}", emmaAction.Action);
                return EmmaResponseDto.SuccessResponse(emmaAction, responseContent, correlationId);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize AI response. Correlation ID: {CorrelationId}", correlationId);
                return EmmaResponseDto.ErrorResponse("Failed to process AI response", correlationId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing AI response. Correlation ID: {CorrelationId}", correlationId);
                return EmmaResponseDto.ErrorResponse("An error occurred while processing the AI response", correlationId, null);
            }
        }
    }

    // Agent interfaces are defined in Emma.Core.Interfaces
}

