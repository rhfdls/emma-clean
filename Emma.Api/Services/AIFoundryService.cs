using System;
using Emma.Api.Models;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Emma.Core.Config;
using Emma.Core.Interfaces;
using Emma.Core.Models;  // Add this for ChatMessage
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Emma.Api.Services
{
    /// <summary>
    /// Service for interacting with Azure AI Foundry (Azure OpenAI)
    /// </summary>
    public class AIFoundryService : IAIFoundryService
    {
        private readonly ILogger<AIFoundryService> _logger;
        private readonly OpenAIClient _openAIClient;
        private readonly AzureAIFoundryConfig _config;
        private readonly CosmosAgentRepository _cosmosRepo;

        /// <summary>
        /// Inject CosmosAgentRepository for agent data access
        /// </summary>
        public AIFoundryService(
            IOptions<AzureAIFoundryConfig> config,
            ILogger<AIFoundryService> logger,
            CosmosAgentRepository cosmosRepo)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cosmosRepo = cosmosRepo ?? throw new ArgumentNullException(nameof(cosmosRepo));
            
            if (string.IsNullOrWhiteSpace(_config.Endpoint))
                throw new ArgumentException("Azure AI Foundry endpoint is not configured", nameof(_config.Endpoint));
                
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
                throw new ArgumentException("Azure AI Foundry API key is not configured", nameof(_config.ApiKey));
            
            if (string.IsNullOrWhiteSpace(_config.DeploymentName))
                throw new ArgumentException("Azure AI Foundry deployment name is not configured", nameof(_config.DeploymentName));

            _openAIClient = new OpenAIClient(
                new Uri(_config.Endpoint),
                new AzureKeyCredential(_config.ApiKey));



            _logger.LogInformation("Azure AI Foundry client initialized for deployment: {DeploymentName}", _config.DeploymentName);
        }

        public async Task<string> ProcessMessageAsync(string message, string? conversationId = null)
        {
            var requestId = Guid.NewGuid();
            _logger.LogDebug("[{RequestId}] Processing message. Interaction ID: {InteractionId}", 
                requestId, conversationId ?? "(new)");
            
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));



            try
            {
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _config.DeploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage("You are a helpful AI assistant."),
                        new ChatRequestUserMessage(message)
                    },
                    MaxTokens = _config.MaxTokens,
                    Temperature = (float?)_config.Temperature,
                    User = conversationId ?? "default-user"
                };
                
                _logger.LogDebug("[{RequestId}] Sending request to Azure OpenAI. Endpoint: {Endpoint}, Deployment: {Deployment}, Message length: {MessageLength} chars, MaxTokens: {MaxTokens}, Temperature: {Temperature}", 
                    requestId, 
                    _config.Endpoint,
                    _config.DeploymentName,
                    message.Length,
                    _config.MaxTokens,
                    _config.Temperature);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                stopwatch.Stop();
                
                _logger.LogDebug("[{RequestId}] Received response from Azure OpenAI in {ElapsedMs}ms. Status: {Status}, RequestId: {RequestId}, CompletionTokens: {CompletionTokens}", 
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    response.GetRawResponse().Status,
                    response.GetRawResponse().ClientRequestId,
                    response.Value.Usage?.CompletionTokens);
                
                if (response?.Value?.Choices?.Count > 0)
                {
                    var content = response.Value.Choices[0].Message.Content;
                    _logger.LogDebug("[{RequestId}] Response content length: {ResponseLength} chars", 
                        requestId,
                        content?.Length ?? 0);
                        
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("[{RequestId}] Full response: {Response}", 
                            requestId,
                            System.Text.Json.JsonSerializer.Serialize(response.GetRawResponse(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    }
                    
                    return content ?? string.Empty;
                }
                
                throw new InvalidOperationException("No response content received from Azure OpenAI");
            }
            catch (RequestFailedException ex) when (ex.Status == 401)
            {
                _logger.LogError("Authentication failed for Azure OpenAI. Please check your API key and endpoint.");
                throw new UnauthorizedAccessException("Authentication failed for Azure OpenAI. Please check your API key and endpoint.", ex);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError("The specified Azure OpenAI deployment was not found. Deployment: {DeploymentName}", _config.DeploymentName);
                throw new InvalidOperationException($"The specified Azure OpenAI deployment was not found: {_config.DeploymentName}", ex);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure OpenAI request failed with status code {StatusCode}", ex.Status);
                throw new HttpRequestException($"Azure OpenAI request failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing message");
                throw new Exception("An unexpected error occurred while processing your request.", ex);
            }
        }

        public async Task<string> ProcessMessageWithContextAsync(string message, string context, string? conversationId = null)
        {
            var requestId = Guid.NewGuid();
            _logger.LogDebug("[{RequestId}] Processing message with context. Interaction ID: {InteractionId}", 
                requestId, conversationId ?? "(new)");
                
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));
                
            if (string.IsNullOrWhiteSpace(context))
                throw new ArgumentException("Context cannot be empty", nameof(context));



            try
            {
                var chatCompletionsOptions = new ChatCompletionsOptions()
                {
                    DeploymentName = _config.DeploymentName,
                    MaxTokens = _config.MaxTokens,
                    Temperature = (float?)_config.Temperature,
                    User = conversationId ?? "default-user"
                };

                // Add system message with context
                chatCompletionsOptions.Messages.Add(
                    new ChatRequestSystemMessage($"Context: {context}"));

                // Add the current user message
                chatCompletionsOptions.Messages.Add(new ChatRequestUserMessage(message));
                
                _logger.LogDebug("[{RequestId}] Sending request to Azure OpenAI with context. Endpoint: {Endpoint}, Deployment: {Deployment}", 
                    requestId,
                    _config.Endpoint,
                    _config.DeploymentName);
                    
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("[{RequestId}] Request details: {RequestDetails}", 
                        requestId,
                        System.Text.Json.JsonSerializer.Serialize(new {
                            deployment = _config.DeploymentName,
                            messages = chatCompletionsOptions.Messages.Select(m => new { 
                                role = m.Role.ToString(),
                                content = m is ChatRequestUserMessage userMsg ? userMsg.Content : 
                                         m is ChatRequestSystemMessage sysMsg ? sysMsg.Content :
                                         m is ChatRequestAssistantMessage asstMsg ? asstMsg.Content :
                                         m.GetType().Name
                            }),
                            maxTokens = _config.MaxTokens,
                            temperature = _config.Temperature
                        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                stopwatch.Stop();
                
                _logger.LogDebug("[{RequestId}] Received response from Azure OpenAI in {ElapsedMs}ms. Status: {Status}, RequestId: {RequestId}, CompletionTokens: {CompletionTokens}", 
                    requestId,
                    stopwatch.ElapsedMilliseconds,
                    response.GetRawResponse().Status,
                    response.GetRawResponse().ClientRequestId,
                    response.Value.Usage?.CompletionTokens);
                
                if (response?.Value?.Choices?.Count > 0)
                {
                    var content = response.Value.Choices[0].Message.Content;
                    _logger.LogDebug("[{RequestId}] Response content length: {ResponseLength} chars", 
                        requestId,
                        content?.Length ?? 0);
                        
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("[{RequestId}] Full response: {Response}", 
                            requestId,
                            System.Text.Json.JsonSerializer.Serialize(response.GetRawResponse(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    }
                    
                    return content ?? string.Empty;
                }
                
                throw new InvalidOperationException("No response content received from Azure OpenAI");
            }
            catch (RequestFailedException ex) when (ex.Status == 401)
            {
                _logger.LogError("Authentication failed for Azure OpenAI. Please check your API key and endpoint.");
                throw new UnauthorizedAccessException("Authentication failed for Azure OpenAI. Please check your API key and endpoint.", ex);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError("The specified Azure OpenAI deployment was not found. Deployment: {DeploymentName}", _config.DeploymentName);
                throw new InvalidOperationException($"The specified Azure OpenAI deployment was not found: {_config.DeploymentName}", ex);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure OpenAI request failed with status code {StatusCode}", ex.Status);
                throw new HttpRequestException($"Azure OpenAI request failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing message with context");
                throw new Exception("An unexpected error occurred while processing your request with context.", ex);
            }
        }

        public Task<string> StartNewInteractionAsync()
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// CosmosDB-backed skill/tool: Retrieve agent interactions using typed parameters.
        /// </summary>
        /// <param name="query">Query DTO with optional leadId, agentId, start, end.</param>
        /// <returns>Enumerable of FulltextInteractionDocument matching the query.</returns>
        public async Task<IEnumerable<FulltextInteractionDocument>> RetrieveAgentInteractionsAsync(Models.InteractionQueryDto query)
        {
            // Build CosmosDB SQL query from parameters
            var sql = "SELECT * FROM c WHERE 1=1";
            if (query.LeadId.HasValue)
                sql += $" AND c.contactId = '{query.LeadId.Value}'";
            if (query.AgentId.HasValue)
                sql += $" AND c.agentId = '{query.AgentId.Value}'";
            if (query.Start.HasValue)
                sql += $" AND c.timestamp >= '{query.Start.Value:O}'";
            if (query.End.HasValue)
                sql += $" AND c.timestamp <= '{query.End.Value:O}'";

            return await _cosmosRepo.QueryItemsAsync<FulltextInteractionDocument>(sql);
        }
    }
}
