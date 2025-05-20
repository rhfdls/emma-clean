using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services
{
    public interface IEmmaAgentService
    {
        Task<EmmaAgentResult> HandleNewMessageAsync(string messageContent, string conversationContext);
    }

    public class EmmaAgentResult
    {
        public string? RawModelOutput { get; set; }
        public string? ActionType { get; set; } // e.g., "SendEmail", "ScheduleFollowup"
        public string? ActionPayload { get; set; } // e.g., email body, follow-up details
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class EmmaAgentService : IEmmaAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IEmailAgent _emailAgent;
        private readonly ISchedulerAgent _schedulerAgent;
        private readonly ILogger<EmmaAgentService> _logger;
        private readonly string _openAIApiKey;
        private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";

        public EmmaAgentService(HttpClient httpClient, IEmailAgent emailAgent, ISchedulerAgent schedulerAgent, ILogger<EmmaAgentService> logger, string openAIApiKey)
        {
            _httpClient = httpClient;
            _emailAgent = emailAgent;
            _schedulerAgent = schedulerAgent;
            _logger = logger;
            _openAIApiKey = openAIApiKey;
        }

        public async Task<EmmaAgentResult> HandleNewMessageAsync(string messageContent, string conversationContext)
        {
            _logger.LogInformation("Received new message for orchestration: {MessageContent}", messageContent);
            // 1. Build prompt for OpenAI
            var prompt = $@"You are EMMA, an AI assistant for real estate agents. Here is the latest message: '{messageContent}'. Here is the conversation context: '{conversationContext}'. What is the best next action for the agent? Respond with a JSON object like: {{ ""action"": ""SendEmail|ScheduleFollowup|None"", ""payload"": ""..."" }}.";
            _logger.LogInformation("Prompt sent to OpenAI: {Prompt}", prompt);

            var requestBody = new
            {
                model = "gpt-4-1106-preview", // or "gpt-4.1" if available
                messages = new[]
                {
                    new { role = "system", content = "You are EMMA, an AI orchestrator for real estate agents." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.3
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAIEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAIApiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw OpenAI response: {ResponseContent}", responseContent);

                // Parse OpenAI response
                var modelOutput = JsonDocument.Parse(responseContent)
                    .RootElement.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                _logger.LogInformation("Model output: {ModelOutput}", modelOutput);

                var actionObj = JsonDocument.Parse(modelOutput);
                var actionType = actionObj.RootElement.GetProperty("action").GetString();
                var actionPayload = actionObj.RootElement.GetProperty("payload").GetString();
                _logger.LogInformation("Parsed action: {ActionType}, payload: {ActionPayload}", actionType, actionPayload);

                // Delegate action
                bool success = false;
                string error = null;
                switch (actionType)
                {
                    case "SendEmail":
                        success = await _emailAgent.SendEmailAsync(actionPayload);
                        break;
                    case "ScheduleFollowup":
                        success = await _schedulerAgent.ScheduleAsync(actionPayload);
                        break;
                    case "None":
                        success = true;
                        break;
                    default:
                        error = $"Unknown action: {actionType}";
                        break;
                }
                _logger.LogInformation("Delegated action result: Success={Success}, Error={Error}", success, error);

                return new EmmaAgentResult
                {
                    RawModelOutput = modelOutput,
                    ActionType = actionType,
                    ActionPayload = actionPayload,
                    Success = success,
                    Error = error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmmaAgentService failed to process message.");
                return new EmmaAgentResult
                {
                    RawModelOutput = null,
                    ActionType = null,
                    ActionPayload = null,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }

    // Stub agent interfaces and implementations
    public interface IEmailAgent
    {
        Task<bool> SendEmailAsync(string emailBody);
    }
    public class EmailAgentStub : IEmailAgent
    {
        public Task<bool> SendEmailAsync(string emailBody)
        {
            // TODO: Integrate with real email service
            return Task.FromResult(true);
        }
    }
    public interface ISchedulerAgent
    {
        Task<bool> ScheduleAsync(string details);
    }
    public class SchedulerAgentStub : ISchedulerAgent
    {
        public Task<bool> ScheduleAsync(string details)
        {
            // TODO: Integrate with real scheduling service
            return Task.FromResult(true);
        }
    }
}
