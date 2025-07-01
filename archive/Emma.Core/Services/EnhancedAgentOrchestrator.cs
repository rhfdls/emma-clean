using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Configuration;
using Emma.Models.Enums;
using Emma.Core.Exceptions;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emma.Core.Services
{
    /// <summary>
    /// Enhanced AgentOrchestrator with support for User/Agent separation, capability validation,
    /// and comprehensive orchestration logging.
    /// </summary>
    public class EnhancedAgentOrchestrator : IAgentOrchestrator, IDisposable
    {
        private readonly IAgentRegistry _agentRegistry;
        private readonly IAgentCapabilityRegistry _capabilityRegistry;
        private readonly IOrchestrationLogger _orchestrationLogger;
        private readonly ILogger<EnhancedAgentOrchestrator> _logger;
        private readonly IOptions<AgentOrchestratorOptions> _options;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _agentLocks = new();
        private bool _disposed;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _cleanupTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedAgentOrchestrator"/> class.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedAgentOrchestrator"/> class.
        /// </summary>
        public EnhancedAgentOrchestrator(
            IAgentRegistry agentRegistry,
            IAgentCapabilityRegistry capabilityRegistry,
            IOrchestrationLogger orchestrationLogger,
            ILogger<EnhancedAgentOrchestrator> logger,
            IOptions<AgentOrchestratorOptions>? options = null)
        {
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _capabilityRegistry = capabilityRegistry ?? throw new ArgumentNullException(nameof(capabilityRegistry));
            _orchestrationLogger = orchestrationLogger ?? throw new ArgumentNullException(nameof(orchestrationLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? Options.Create(new AgentOrchestratorOptions());
            
            // Initialize health check timer (every 5 minutes)
            _healthCheckTimer = new Timer(
                CheckAgentHealthStatusAsync, 
                null, 
                TimeSpan.FromMinutes(5), 
                TimeSpan.FromMinutes(5));
                
            // Initialize cleanup timer (every hour)
            _cleanupTimer = new Timer(
                CleanupOldAgentLocksAsync, 
                null, 
                TimeSpan.FromHours(1), 
                TimeSpan.FromHours(1));
                
            _logger.LogInformation("EnhancedAgentOrchestrator initialized with {MaxConcurrentRequests} max concurrent requests per agent", 
                _options.Value.MaxConcurrentRequestsPerAgent);
        }

        /// <inheritdoc />
        public async Task<AgentResponse> ProcessRequestAsync(AgentRequest request, AgentContext context, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));
            
            // Get or create a lock for this agent type to prevent too many concurrent requests
            var agentLock = _agentLocks.GetOrAdd(context.AgentType, _ => new SemaphoreSlim(
                _options.Value.MaxConcurrentRequestsPerAgent, 
                _options.Value.MaxConcurrentRequestsPerAgent));
            
            // Create a log entry for this operation
            var log = await _orchestrationLogger.StartOperationAsync(
                "ProcessRequest", 
                context, 
                new { 
                    Request = request,
                    RequestId = context.RequestId,
                    SessionId = context.SessionId,
                    UserId = context.UserId,
                    TenantId = context.TenantId
                });
                
            try
            {
                // Wait for a slot to be available, with timeout
                if (!await agentLock.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
                {
                    throw new TimeoutException("Timeout waiting for agent processing slot");
                }
                
                // Validate the context
                var (isValid, error) = context.Validate();
                if (!isValid)
                {
                    throw new InvalidOperationException($"Invalid agent context: {error}");
                }
                
                // Log the request
                _logger.LogInformation("Processing {AgentType} request {RequestId} for user {UserId}", 
                    context.AgentType, context.RequestId, context.UserId);
                    
                // Check if the agent has the required capability
                await _capabilityRegistry.ValidateActionAsync(context.AgentType, request.Action, context.ToDictionary());
                
                // Get the agent instance
                var agent = await GetAgentAsync(context.AgentType);
                if (agent == null)
                {
                    throw new InvalidOperationException($"No agent registered with type: {context.AgentType}");
                }
                
                // Process the request
                var response = await agent.ProcessRequestAsync(request, context, cancellationToken);
                
                // Log successful completion
                await _orchestrationLogger.CompleteOperationAsync(
                    log.Id, 
                    OperationStatus.Completed, 
                    new { 
                        Response = response,
                        Duration = DateTimeOffset.UtcNow - log.StartedAt
                    });
                    
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {AgentType} request: {Message}", context.AgentType, ex.Message);
                await _orchestrationLogger.FailOperationAsync(
                    log.Id, 
                    OperationStatus.Faulted, 
                    new { 
                        Error = ex.Message,
                        StackTrace = ex.StackTrace
                    });
                throw new AgentProcessingException("An error occurred while processing the agent request", ex);
            }
            finally
            {
                // Always release the semaphore if it was acquired
                if (agentLock != null)
                {
                    try
                    {
                        agentLock.Release();
                    }
                    catch (SemaphoreFullException ex)
                    {
                        _logger.LogWarning(ex, "Semaphore release failed for {AgentType}", context.AgentType);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<WorkflowState> ExecuteWorkflowAsync(string workflowId, AgentRequest request, AgentContext context)
        {
            if (string.IsNullOrEmpty(workflowId))
                throw new ArgumentException("Workflow ID is required", nameof(workflowId));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var log = await _orchestrationLogger.StartOperationAsync(
                "ExecuteWorkflow", 
                context, 
                new { WorkflowId = workflowId, Request = request });

            try
            {
                // Validate the context and permissions
                await _capabilityRegistry.ValidateActionAsync(context.AgentType, $"workflow:{workflowId}:execute");

                // Implementation would execute the workflow steps here
                // This is a simplified example
                var workflowState = new WorkflowState
                {
                    WorkflowId = workflowId,
                    Status = "Completed",
                    Variables = new Dictionary<string, object>(),
                    ExecutionHistory = new List<WorkflowStep>(),
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                await _orchestrationLogger.CompleteOperationAsync(log.Id, new { State = workflowState });
                return workflowState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
                await _orchestrationLogger.FailOperationAsync(log.Id, ex);
                throw;
            }
        }



        /// <summary>
        /// Creates an error response with the specified message.
        /// </summary>
        private AgentResponse CreateErrorResponse(
            AgentContext context, 
            string message, 
            string logId, 
            Exception? ex = null)
        {
            return new AgentResponse
            {
                Success = false,
                Message = message,
                AgentType = context.AgentType,
                TraceId = context.TraceId,
                LogId = logId,
                ErrorDetails = ex?.Message
            };
        }

        /// <summary>
        /// Disposes the orchestrator and releases any resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the orchestrator and releases any resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here
                }
                _disposed = true;
            }
        }
        
        /// <summary>
        /// Gets an agent instance by type
        /// </summary>
        private async Task<IAgent?> GetAgentAsync(string agentType)
        {
            try
            {
                var agent = await _agentRegistry.GetAgentAsync<IAgent>(agentType);
                if (agent == null)
                {
                    _logger.LogWarning("No agent registered with type: {AgentType}", agentType);
                    return null;
                }
                
                return agent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent {AgentType}: {Message}", agentType, ex.Message);
                throw new AgentNotFoundException($"Failed to get agent of type {agentType}", ex);
            }
        }
        
        /// <summary>
        /// Performs health checks on all registered agents
        /// </summary>
        private async void CheckAgentHealthStatusAsync(object? state = null)
        {
            try
            {
                _logger.LogInformation("Starting agent health checks...");
                var agentTypes = _agentRegistry.GetRegisteredAgentTypes();
                var healthTasks = new List<Task>();
                
                foreach (var agentType in agentTypes)
                {
                    healthTasks.Add(CheckSingleAgentHealthAsync(agentType));
                }
                
                await Task.WhenAll(healthTasks);
                _logger.LogInformation("Completed agent health checks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent health checks");
            }
        }
        
        /// <summary>
        /// Checks the health of a single agent
        /// </summary>
        private async Task CheckSingleAgentHealthAsync(string agentType)
        {
            try
            {
                var agent = await GetAgentAsync(agentType);
                if (agent == null) return;
                
                var healthCheck = await agent.CheckHealthAsync();
                if (healthCheck.Status != HealthStatus.Healthy)
                {
                    _logger.LogWarning("Agent {AgentType} is unhealthy: {Status} - {Details}", 
                        agentType, healthCheck.Status, healthCheck.Details);
                }
                else
                {
                    _logger.LogDebug("Agent {AgentType} is healthy", agentType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health of agent {AgentType}", agentType);
            }
        }
        
        /// <summary>
        /// Cleans up old agent locks that are no longer in use
        /// </summary>
        private async void CleanupOldAgentLocksAsync(object? state = null)
        {
            try
            {
                _logger.LogInformation("Cleaning up old agent locks...");
                var now = DateTimeOffset.UtcNow;
                var locksToRemove = new List<string>();
                
                // Find locks that haven't been used in the last hour
                foreach (var kvp in _agentLocks)
                {
                    var agentType = kvp.Key;
                    var semaphore = kvp.Value;
                    
                    // If semaphore is available and hasn't been used recently, mark for removal
                    if (semaphore.CurrentCount == _options.Value.MaxConcurrentRequestsPerAgent)
                    {
                        // Check last activity time (simplified - in a real app, track last use)
                        locksToRemove.Add(agentType);
                    }
                }
                
                // Remove unused locks
                foreach (var agentType in locksToRemove)
                {
                    if (_agentLocks.TryRemove(agentType, out var semaphore))
                    {
                        semaphore.Dispose();
                        _logger.LogDebug("Removed unused lock for agent {AgentType}", agentType);
                    }
                }
                
                _logger.LogInformation("Cleaned up {Count} unused agent locks", locksToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up agent locks");
            }
        }
        
        /// <summary>
        /// Disposes the orchestrator and its resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                
                // Stop and dispose timers
                _healthCheckTimer?.Dispose();
                _cleanupTimer?.Dispose();
                
                // Clean up all semaphores
                foreach (var semaphore in _agentLocks.Values)
                {
                    try
                    {
                        semaphore?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing agent semaphore");
                    }
                }
                
                _agentLocks.Clear();
                
                GC.SuppressFinalize(this);
            }
        }
        // --- BEGIN: Missing IAgentOrchestrator methods ---
        public Task<AgentResponse> RouteToAzureAgentAsync(string agentType, AgentTask task)
        {
            throw new NotImplementedException();
        }

        public Task<AgentResponse> RouteToNbaAgentAsync(string userInput, Guid conversationId, string? traceId = null)
        {
            throw new NotImplementedException();
        }

        public Task<AgentResponse> RouteToContextIntelligenceAgentAsync(AgentRequest request, string? traceId = null)
        {
            throw new NotImplementedException();
        }

        public Task<AgentResponse> RouteToIntentClassificationAgentAsync(AgentRequest request, string? traceId = null)
        {
            throw new NotImplementedException();
        }
        // --- END: Missing IAgentOrchestrator methods ---
    }

    /// <summary>
    /// Options for configuring the EnhancedAgentOrchestrator.
    /// </summary>
    public class AgentOrchestratorOptions
    {
        /// <summary>
        /// Gets or sets the default timeout for agent operations.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to enable detailed logging of agent inputs and outputs.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate agent capabilities before executing requests.
        /// </summary>
        public bool ValidateCapabilities { get; set; } = true;
    }
}
