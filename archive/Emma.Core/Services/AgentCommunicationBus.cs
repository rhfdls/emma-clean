using Emma.Models.Interfaces;
using Emma.Core.Models;
using Emma.Core.Agents;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Emma.Core.Services
{
    /// <summary>
    /// Agent-to-agent communication bus with hot-swappable orchestration
    /// Supports both custom orchestration and Azure AI Foundry workflows
    /// </summary>
    public class AgentCommunicationBus : IAgentCommunicationBus
    {
        private readonly IAgentRegistryService _agentRegistry;
        private readonly IIntentClassificationService _intentClassifier;
        private readonly ILogger<AgentCommunicationBus> _logger;
        private readonly Dictionary<string, WorkflowState> _workflowStates;
        private string _orchestrationMethod;

        public AgentCommunicationBus(
            IAgentRegistryService agentRegistry,
            IIntentClassificationService intentClassifier,
            ILogger<AgentCommunicationBus> logger)
        {
            _agentRegistry = agentRegistry;
            _intentClassifier = intentClassifier;
            _logger = logger;
            _workflowStates = new Dictionary<string, WorkflowState>();
            _orchestrationMethod = "custom"; // Default to custom orchestration
        }

        public async Task<AgentResponse> RouteRequestAsync(AgentRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Routing request {RequestId} with intent {Intent}, TraceId: {TraceId}",
                    request.Id, request.Intent, request.TraceId);

                // Update orchestration method in request
                request.OrchestrationMethod = _orchestrationMethod;

                // Find appropriate agent for the intent
                var agents = await _agentRegistry.FindAgentsForIntentAsync(request.Intent, request.Industry);
                
                if (!agents.Any())
                {
                    _logger.LogWarning("No agents found for intent {Intent}, falling back to general inquiry, TraceId: {TraceId}",
                        request.Intent, request.TraceId);
                    
                    // Fallback to general inquiry
                    agents = await _agentRegistry.FindAgentsForIntentAsync(AgentIntent.GeneralInquiry);
                }

                if (!agents.Any())
                {
                    return CreateErrorResponse(request, "No suitable agents available", stopwatch.ElapsedMilliseconds);
                }

                // Select best agent based on performance metrics
                var selectedAgent = SelectBestAgent(agents, request);
                var agentInstance = await _agentRegistry.GetAgentAsync(selectedAgent.AgentId);

                if (agentInstance == null)
                {
                    return CreateErrorResponse(request, $"Agent {selectedAgent.AgentId} not found", stopwatch.ElapsedMilliseconds);
                }

                // Execute the request
                var response = await ExecuteAgentRequestAsync(agentInstance, request, selectedAgent.AgentId);
                
                // Update agent metrics
                await _agentRegistry.UpdateAgentMetricsAsync(
                    selectedAgent.AgentId, 
                    stopwatch.ElapsedMilliseconds, 
                    response.Success, 
                    response.Confidence);

                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                response.OrchestrationMethod = _orchestrationMethod;

                _logger.LogInformation("Request {RequestId} routed successfully to agent {AgentId}, TraceId: {TraceId}",
                    request.Id, selectedAgent.AgentId, request.TraceId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error routing request {RequestId}, TraceId: {TraceId}", 
                    request.Id, request.TraceId);
                
                return CreateErrorResponse(request, ex.Message, stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task<bool> RegisterAgentAsync(string agentId, ISpecializedAgent agent, AgentCapability capability)
        {
            var result = await _agentRegistry.RegisterAgentAsync(agentId, agent, capability);
            
            _logger.LogInformation("Agent {AgentId} registered successfully with capabilities: {Capabilities}",
                agentId, string.Join(", ", capability.SupportedIntents));
                
            return result;
        }

        public async Task UnregisterAgentAsync(string agentId)
        {
            await _agentRegistry.UnregisterAgentAsync(agentId);
            
            _logger.LogInformation("Agent {AgentId} unregistered successfully", agentId);
        }

        public async Task<Dictionary<string, AgentCapability>> GetAgentCapabilitiesAsync()
        {
            return await _agentRegistry.GetAllAgentCapabilitiesAsync();
        }

        public async Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, AgentRequest initialRequest)
        {
            _logger.LogInformation("Starting workflow {WorkflowId} with initial request {RequestId}, TraceId: {TraceId}",
                workflowId, initialRequest.Id, initialRequest.TraceId);

            var workflowState = new WorkflowState
            {
                WorkflowId = workflowId,
                TraceId = initialRequest.TraceId,
                WorkflowVersion = initialRequest.WorkflowVersion ?? "1.0.0",
                CurrentState = "Processing",
                OrchestrationMethod = _orchestrationMethod,
                UserId = initialRequest.UserId,
                Industry = initialRequest.Industry
            };

            // Add initial request to pending
            workflowState.PendingRequests.Add(initialRequest);
            
            // Store workflow state
            _workflowStates[workflowId] = workflowState;

            try
            {
                // Process the initial request
                var response = await RouteRequestAsync(initialRequest);
                
                // Update workflow state
                workflowState.CompletedResponses.Add(response);
                workflowState.PendingRequests.Remove(initialRequest);
                
                // Add execution step
                var step = new WorkflowStep
                {
                    StepName = "InitialRequest",
                    AgentId = response.AgentId ?? "unknown",
                    IsCompleted = true,
                    Result = response.Content,
                    ErrorMessage = response.ErrorMessage
                };
                step.CompletedAt = DateTime.UtcNow;
                workflowState.ExecutionHistory.Add(step);

                // Check if workflow needs follow-up
                if (response.RequiresFollowUp && response.NextIntent.HasValue)
                {
                    var followUpRequest = CreateFollowUpRequest(initialRequest, response);
                    workflowState.PendingRequests.Add(followUpRequest);
                }
                else
                {
                    // Workflow completed
                    workflowState.IsCompleted = true;
                    workflowState.CompletedAt = DateTime.UtcNow;
                    workflowState.CurrentState = "Completed";
                }

                _logger.LogInformation("Workflow {WorkflowId} processed successfully, TraceId: {TraceId}",
                    workflowId, initialRequest.TraceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}, TraceId: {TraceId}",
                    workflowId, initialRequest.TraceId);
                
                workflowState.ErrorMessage = ex.Message;
                workflowState.CurrentState = "Error";
                workflowState.CompletedAt = DateTime.UtcNow;
            }

            return workflowState;
        }

        public async Task<WorkflowState?> GetWorkflowStateAsync(string workflowId)
        {
            _workflowStates.TryGetValue(workflowId, out var state);
            return await Task.FromResult(state);
        }

        public void SetOrchestrationMethod(string method)
        {
            _orchestrationMethod = method;
            _logger.LogInformation("Orchestration method updated to: {Method}", method);
        }

        private AgentCapability SelectBestAgent(List<AgentCapability> agents, AgentRequest request)
        {
            // Simple selection based on performance metrics
            // In production, this could be more sophisticated (load balancing, A/B testing, etc.)
            
            var bestAgent = agents
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.PerformanceMetrics?.SuccessRate ?? 0.5)
                .ThenBy(a => a.PerformanceMetrics?.AverageResponseTimeMs ?? 1000)
                .First();

            _logger.LogDebug("Selected agent {AgentId} for request {RequestId}, TraceId: {TraceId}",
                bestAgent.AgentId, request.Id, request.TraceId);

            return bestAgent;
        }

        private async Task<AgentResponse> ExecuteAgentRequestAsync(ISpecializedAgent agent, AgentRequest request, string agentId)
        {
            try
            {
                // Convert AgentRequest to AgentTask for the existing interface
                var task = new AgentTask
                {
                    Id = request.Id,
                    Type = request.Intent.ToString(),
                    Description = request.OriginalUserInput,
                    InteractionId = request.InteractionId, // Use InteractionId
                    Context = request.Context,
                    CreatedAt = request.Timestamp,
                    UserId = request.UserId,
                    Industry = request.Industry
                };

                var response = await agent.ExecuteTaskAsync(task);
                response.RequestId = request.Id;
                response.TraceId = request.TraceId;
                response.AgentId = agentId;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing agent request for agent {AgentId}, TraceId: {TraceId}",
                    agentId, request.TraceId);
                
                return new AgentResponse
                {
                    RequestId = request.Id,
                    TraceId = request.TraceId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    AgentId = agentId,
                    Confidence = 0.0
                };
            }
        }

        private AgentResponse CreateErrorResponse(AgentRequest request, string errorMessage, long processingTimeMs)
        {
            return new AgentResponse
            {
                RequestId = request.Id,
                TraceId = request.TraceId,
                Success = false,
                ErrorMessage = errorMessage,
                ProcessingTimeMs = processingTimeMs,
                OrchestrationMethod = _orchestrationMethod,
                Confidence = 0.0
            };
        }

        private AgentRequest CreateFollowUpRequest(AgentRequest originalRequest, AgentResponse response)
        {
            return new AgentRequest
            {
                Id = Guid.NewGuid().ToString(),
                TraceId = originalRequest.TraceId,
                EventVersion = originalRequest.EventVersion,
                WorkflowVersion = originalRequest.WorkflowVersion,
                Intent = response.NextIntent ?? AgentIntent.GeneralInquiry,
                OriginalUserInput = response.Content ?? "",
                InteractionId = originalRequest.InteractionId, // Use InteractionId
                Context = response.Data,
                SourceAgentId = response.AgentId,
                Urgency = originalRequest.Urgency,
                OrchestrationMethod = _orchestrationMethod,
                UserId = originalRequest.UserId,
                Industry = originalRequest.Industry
            };
        }
    }
}
