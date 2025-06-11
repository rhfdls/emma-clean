namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Lifecycle interface for agents supporting hot-reload, health checks, and graceful shutdown.
    /// Implements the lifecycle hooks pattern identified in Sprint 1 requirements.
    /// </summary>
    public interface IAgentLifecycle
    {
        /// <summary>
        /// Called when the agent is started or registered.
        /// Use for initialization, resource allocation, and startup validation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for startup timeout</param>
        /// <returns>Task representing the startup operation</returns>
        Task OnStartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when the agent configuration or implementation is reloaded.
        /// Use for hot-reload scenarios without full system restart.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for reload timeout</param>
        /// <returns>Task representing the reload operation</returns>
        Task OnReloadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Called periodically to check agent health status.
        /// Should return quickly and indicate if the agent is operational.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for health check timeout</param>
        /// <returns>Health check result with status and optional details</returns>
        Task<AgentHealthCheckResult> OnHealthCheckAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Called when the agent is being stopped or unregistered.
        /// Use for cleanup, resource disposal, and graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for shutdown timeout</param>
        /// <returns>Task representing the shutdown operation</returns>
        Task OnStopAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of an agent health check operation.
    /// </summary>
    public class AgentHealthCheckResult
    {
        /// <summary>
        /// Health status of the agent.
        /// </summary>
        public AgentHealthStatus Status { get; set; } = AgentHealthStatus.Unknown;

        /// <summary>
        /// Human-readable description of the health status.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Additional health check details or metrics.
        /// </summary>
        public IDictionary<string, object> Details { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp when the health check was performed.
        /// </summary>
        public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Unique identifier for audit trail.
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Reason for the health status (explainability).
        /// </summary>
        public string Reason { get; set; } = "Health check completed";

        /// <summary>
        /// Create a healthy result.
        /// </summary>
        /// <param name="description">Optional description</param>
        /// <returns>Healthy health check result</returns>
        public static AgentHealthCheckResult Healthy(string description = "Agent is healthy")
        {
            return new AgentHealthCheckResult
            {
                Status = AgentHealthStatus.Healthy,
                Description = description,
                Reason = "Agent passed health check"
            };
        }

        /// <summary>
        /// Create a degraded result.
        /// </summary>
        /// <param name="description">Description of the degradation</param>
        /// <param name="details">Additional details about the issue</param>
        /// <returns>Degraded health check result</returns>
        public static AgentHealthCheckResult Degraded(string description, IDictionary<string, object>? details = null)
        {
            return new AgentHealthCheckResult
            {
                Status = AgentHealthStatus.Degraded,
                Description = description,
                Details = details ?? new Dictionary<string, object>(),
                Reason = "Agent is experiencing issues but still functional"
            };
        }

        /// <summary>
        /// Create an unhealthy result.
        /// </summary>
        /// <param name="description">Description of the health issue</param>
        /// <param name="details">Additional details about the failure</param>
        /// <returns>Unhealthy health check result</returns>
        public static AgentHealthCheckResult Unhealthy(string description, IDictionary<string, object>? details = null)
        {
            return new AgentHealthCheckResult
            {
                Status = AgentHealthStatus.Unhealthy,
                Description = description,
                Details = details ?? new Dictionary<string, object>(),
                Reason = "Agent failed health check"
            };
        }

        /// <summary>
        /// Create an unknown result.
        /// </summary>
        /// <param name="description">Description of why status is unknown</param>
        /// <returns>Unknown health check result</returns>
        public static AgentHealthCheckResult Unknown(string description = "Agent health status is unknown")
        {
            return new AgentHealthCheckResult
            {
                Status = AgentHealthStatus.Unknown,
                Description = description,
                Reason = "Unable to determine agent health status"
            };
        }
    }
}
