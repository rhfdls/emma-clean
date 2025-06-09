using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Emma.Api.Controllers
{
    /// <summary>
    /// Controller for AI-first CRM agent orchestration and workflow management
    /// Demonstrates Azure AI Foundry integration with A2A protocol compliance
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AgentOrchestrationController : ControllerBase
    {
        private readonly IAgentCommunicationBus _communicationBus;
        private readonly IIntentClassificationService _intentClassifier;
        private readonly IContextIntelligenceService _contextIntelligence;
        private readonly IAgentRegistryService _agentRegistry;
        private readonly ILogger<AgentOrchestrationController> _logger;

        public AgentOrchestrationController(
            IAgentCommunicationBus communicationBus,
            IIntentClassificationService intentClassifier,
            IContextIntelligenceService contextIntelligence,
            IAgentRegistryService agentRegistry,
            ILogger<AgentOrchestrationController> logger)
        {
            _communicationBus = communicationBus;
            _intentClassifier = intentClassifier;
            _contextIntelligence = contextIntelligence;
            _agentRegistry = agentRegistry;
            _logger = logger;
        }

        /// <summary>
        /// Process a natural language request through the AI-first CRM agent orchestration system
        /// </summary>
        [HttpPost("process")]
        public async Task<IActionResult> ProcessRequest([FromBody] OrchestrationRequest request)
        {
            var traceId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Processing orchestration request, TraceId: {TraceId}", traceId);

                // Step 1: Classify the user's intent
                var intentResult = await _intentClassifier.ClassifyIntentAsync(
                    request.UserInput, 
                    request.Context, 
                    traceId);

                _logger.LogInformation("Intent classified as {Intent} with confidence {Confidence}, TraceId: {TraceId}",
                    intentResult.Intent, intentResult.Confidence, traceId);

                // Step 2: Analyze context intelligence if interaction content is provided
                ContactContext? contactContext = null;
                if (!string.IsNullOrEmpty(request.InteractionContent))
                {
                    contactContext = await _contextIntelligence.AnalyzeInteractionAsync(
                        request.InteractionContent, 
                        request.ContactContext, 
                        traceId);
                }

                // Step 3: Create agent request
                var agentRequest = new AgentRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    TraceId = traceId,
                    EventVersion = "1.0.0",
                    WorkflowVersion = "1.0.0",
                    Intent = intentResult.Intent,
                    OriginalUserInput = request.UserInput,
                    ConversationId = request.ConversationId ?? Guid.NewGuid(),
                    Context = request.Context ?? new Dictionary<string, object>(),
                    Urgency = intentResult.Urgency,
                    OrchestrationMethod = request.OrchestrationMethod ?? "custom",
                    UserId = request.UserId,
                    Industry = request.Industry
                };

                // Add context intelligence results to request context
                if (contactContext != null)
                {
                    agentRequest.Context["contactContext"] = contactContext;
                    agentRequest.Context["sentimentScore"] = contactContext.SentimentScore;
                    agentRequest.Context["buyingSignals"] = contactContext.BuyingSignals;
                    agentRequest.Context["closeProbability"] = contactContext.CloseProbability;
                }

                // Step 4: Route request through communication bus
                var response = await _communicationBus.RouteRequestAsync(agentRequest);

                // Step 5: Generate recommended actions if requested
                List<string> recommendedActions = new();
                if (request.IncludeRecommendations && contactContext != null)
                {
                    recommendedActions = await _contextIntelligence.GenerateRecommendedActionsAsync(
                        contactContext, traceId);
                }

                // Step 6: Create orchestration response
                var orchestrationResponse = new OrchestrationResponse
                {
                    RequestId = agentRequest.Id,
                    TraceId = traceId,
                    Success = response.Success,
                    Content = response.Content,
                    Confidence = response.Confidence,
                    ProcessingTimeMs = response.ProcessingTimeMs,
                    OrchestrationMethod = response.OrchestrationMethod,
                    IntentClassification = intentResult,
                    ContactContext = contactContext,
                    RecommendedActions = recommendedActions,
                    AgentResponse = response,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Orchestration request processed successfully, TraceId: {TraceId}", traceId);

                return Ok(orchestrationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing orchestration request, TraceId: {TraceId}", traceId);
                
                return StatusCode(500, new OrchestrationResponse
                {
                    TraceId = traceId,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Execute a multi-step workflow through the agent orchestration system
        /// </summary>
        [HttpPost("workflow")]
        public async Task<IActionResult> ExecuteWorkflow([FromBody] WorkflowExecutionRequest request)
        {
            var traceId = Guid.NewGuid().ToString();
            
            try
            {
                _logger.LogInformation("Starting workflow execution {WorkflowId}, TraceId: {TraceId}", 
                    request.WorkflowId, traceId);

                // Create initial agent request for workflow
                var initialRequest = new AgentRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    TraceId = traceId,
                    EventVersion = "1.0.0",
                    WorkflowVersion = request.WorkflowVersion ?? "1.0.0",
                    Intent = request.InitialIntent,
                    OriginalUserInput = request.InitialInput,
                    ConversationId = request.ConversationId ?? Guid.NewGuid(),
                    Context = request.Context ?? new Dictionary<string, object>(),
                    Urgency = request.Urgency,
                    OrchestrationMethod = request.OrchestrationMethod ?? "custom",
                    UserId = request.UserId,
                    Industry = request.Industry
                };

                // Execute workflow
                var workflowState = await _communicationBus.ExecuteWorkflowAsync(
                    request.WorkflowId, initialRequest);

                _logger.LogInformation("Workflow {WorkflowId} execution completed, TraceId: {TraceId}", 
                    request.WorkflowId, traceId);

                return Ok(workflowState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}, TraceId: {TraceId}", 
                    request.WorkflowId, traceId);
                
                return StatusCode(500, new { error = ex.Message, traceId });
            }
        }

        /// <summary>
        /// Get the status of a running workflow
        /// </summary>
        [HttpGet("workflow/{workflowId}/status")]
        public async Task<IActionResult> GetWorkflowStatus(string workflowId)
        {
            try
            {
                var workflowState = await _communicationBus.GetWorkflowStateAsync(workflowId);
                
                if (workflowState == null)
                {
                    return NotFound(new { message = "Workflow not found", workflowId });
                }

                return Ok(workflowState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow status for {WorkflowId}", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get all registered agent capabilities
        /// </summary>
        [HttpGet("agents/capabilities")]
        public async Task<IActionResult> GetAgentCapabilities()
        {
            try
            {
                var capabilities = await _agentRegistry.GetAllAgentCapabilitiesAsync();
                return Ok(capabilities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent capabilities");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get health status of all registered agents
        /// </summary>
        [HttpGet("agents/health")]
        public async Task<IActionResult> GetAgentHealth()
        {
            try
            {
                var healthStatuses = await _agentRegistry.GetAllAgentHealthAsync();
                return Ok(healthStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent health statuses");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Set the orchestration method (custom or azure_foundry)
        /// </summary>
        [HttpPost("orchestration/method")]
        public IActionResult SetOrchestrationMethod([FromBody] OrchestrationMethodRequest request)
        {
            try
            {
                _communicationBus.SetOrchestrationMethod(request.Method);
                
                _logger.LogInformation("Orchestration method updated to: {Method}", request.Method);
                
                return Ok(new { message = "Orchestration method updated", method = request.Method });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting orchestration method to {Method}", request.Method);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Load agent catalog from directory
        /// </summary>
        [HttpPost("agents/catalog/load")]
        public async Task<IActionResult> LoadAgentCatalog([FromBody] LoadCatalogRequest request)
        {
            try
            {
                var loadedCount = await _agentRegistry.LoadAgentCatalogAsync(request.CatalogPath);
                
                _logger.LogInformation("Loaded {Count} agents from catalog path: {Path}", 
                    loadedCount, request.CatalogPath);
                
                return Ok(new { message = "Agent catalog loaded", loadedCount, catalogPath = request.CatalogPath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading agent catalog from {Path}", request.CatalogPath);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Analyze interaction content for context intelligence
        /// </summary>
        [HttpPost("analysis/interaction")]
        public async Task<IActionResult> AnalyzeInteraction([FromBody] InteractionAnalysisRequest request)
        {
            var traceId = Guid.NewGuid().ToString();
            
            try
            {
                var contactContext = await _contextIntelligence.AnalyzeInteractionAsync(
                    request.InteractionContent, 
                    request.ContactContext, 
                    traceId);

                return Ok(contactContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing interaction, TraceId: {TraceId}", traceId);
                return StatusCode(500, new { error = ex.Message, traceId });
            }
        }
    }

    #region Request/Response Models

    /// <summary>
    /// Request model for agent orchestration
    /// </summary>
    public class OrchestrationRequest
    {
        [Required]
        public string UserInput { get; set; } = string.Empty;
        
        public Guid? ConversationId { get; set; }
        
        public Dictionary<string, object>? Context { get; set; }
        
        public string? InteractionContent { get; set; }
        
        public ContactContext? ContactContext { get; set; }
        
        public bool IncludeRecommendations { get; set; } = true;
        
        public string? OrchestrationMethod { get; set; }
        
        public string? UserId { get; set; }
        
        public string? Industry { get; set; }
    }

    /// <summary>
    /// Response model for agent orchestration
    /// </summary>
    public class OrchestrationResponse
    {
        public string? RequestId { get; set; }
        
        public string? TraceId { get; set; }
        
        public bool Success { get; set; }
        
        public string? Content { get; set; }
        
        public double Confidence { get; set; }
        
        public long ProcessingTimeMs { get; set; }
        
        public string? OrchestrationMethod { get; set; }
        
        public IntentClassificationResult? IntentClassification { get; set; }
        
        public ContactContext? ContactContext { get; set; }
        
        public List<string> RecommendedActions { get; set; } = new();
        
        public AgentResponse? AgentResponse { get; set; }
        
        public string? ErrorMessage { get; set; }
        
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Request model for workflow execution
    /// </summary>
    public class WorkflowExecutionRequest
    {
        [Required]
        public string WorkflowId { get; set; } = string.Empty;
        
        public string? WorkflowVersion { get; set; }
        
        [Required]
        public AgentIntent InitialIntent { get; set; }
        
        [Required]
        public string InitialInput { get; set; } = string.Empty;
        
        public Guid? ConversationId { get; set; }
        
        public Dictionary<string, object>? Context { get; set; }
        
        public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Medium;
        
        public string? OrchestrationMethod { get; set; }
        
        public string? UserId { get; set; }
        
        public string? Industry { get; set; }
    }

    /// <summary>
    /// Request model for setting orchestration method
    /// </summary>
    public class OrchestrationMethodRequest
    {
        [Required]
        public string Method { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for loading agent catalog
    /// </summary>
    public class LoadCatalogRequest
    {
        [Required]
        public string CatalogPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for interaction analysis
    /// </summary>
    public class InteractionAnalysisRequest
    {
        [Required]
        public string InteractionContent { get; set; } = string.Empty;
        
        public ContactContext? ContactContext { get; set; }
    }

    #endregion
}
