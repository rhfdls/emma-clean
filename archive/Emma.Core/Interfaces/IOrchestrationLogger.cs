using System;
using System.Threading.Tasks;
using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Defines a service for logging agent orchestration activities.
    /// </summary>
    public interface IOrchestrationLogger
    {
        /// <summary>
        /// Logs the start of an operation.
        /// </summary>
        /// <param name="operationType">The type of operation being performed.</param>
        /// <param name="context">The agent context for the operation.</param>
        /// <param name="input">Optional input data for the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entry.</returns>
        Task<OrchestrationLog> StartOperationAsync(string operationType, AgentContext context, object? input = null);

        /// <summary>
        /// Logs the successful completion of an operation.
        /// </summary>
        /// <param name="logId">The ID of the log entry to update.</param>
        /// <param name="output">Optional output data from the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CompleteOperationAsync(string logId, object? output = null);

        /// <summary>
        /// Logs the failure of an operation.
        /// </summary>
        /// <param name="logId">The ID of the log entry to update.</param>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task FailOperationAsync(string logId, Exception exception);

        /// <summary>
        /// Gets a log entry by its ID.
        /// </summary>
        /// <param name="logId">The ID of the log entry to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entry, or null if not found.</returns>
        Task<OrchestrationLog?> GetLogByIdAsync(string logId);

        /// <summary>
        /// Gets all log entries for a trace ID.
        /// </summary>
        /// <param name="traceId">The trace ID to get logs for.</param>
        /// <param name="includeRelated">Whether to include related logs (e.g., from child operations).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entries.</returns>
        Task<IEnumerable<OrchestrationLog>> GetLogsByTraceIdAsync(string traceId, bool includeRelated = true);

        /// <summary>
        /// Gets all log entries for a session.
        /// </summary>
        /// <param name="sessionId">The session ID to get logs for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entries.</returns>
        Task<IEnumerable<OrchestrationLog>> GetLogsBySessionIdAsync(string sessionId);

        /// <summary>
        /// Gets all log entries for an agent.
        /// </summary>
        /// <param name="agentId">The agent ID to get logs for.</param>
        /// <param name="startDate">Optional start date to filter logs.</param>
        /// <param name="endDate">Optional end date to filter logs.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entries.</returns>
        Task<IEnumerable<OrchestrationLog>> GetLogsByAgentIdAsync(
            string agentId, 
            DateTimeOffset? startDate = null, 
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Gets all log entries for a user.
        /// </summary>
        /// <param name="userId">The user ID to get logs for.</param>
        /// <param name="startDate">Optional start date to filter logs.</param>
        /// <param name="endDate">Optional end date to filter logs.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entries.</returns>
        Task<IEnumerable<OrchestrationLog>> GetLogsByUserIdAsync(
            Guid userId, 
            DateTimeOffset? startDate = null, 
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Gets all log entries for a contact.
        /// </summary>
        /// <param name="contactId">The contact ID to get logs for.</param>
        /// <param name="startDate">Optional start date to filter logs.</param>
        /// <param name="endDate">Optional end date to filter logs.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entries.</returns>
        Task<IEnumerable<OrchestrationLog>> GetLogsByContactIdAsync(
            string contactId, 
            DateTimeOffset? startDate = null, 
            DateTimeOffset? endDate = null);

        /// <summary>
        /// Searches log entries based on the specified criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the log entries.</returns>
        Task<IEnumerable<OrchestrationLog>> SearchLogsAsync(OrchestrationLogSearchCriteria criteria);

        /// <summary>
        /// Archives old log entries.
        /// </summary>
        /// <param name="olderThan">The cutoff date for archiving logs.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of logs archived.</returns>
        Task<int> ArchiveLogsAsync(DateTimeOffset olderThan);
    }

    /// <summary>
    /// Represents search criteria for querying orchestration logs.
    /// </summary>
    public class OrchestrationLogSearchCriteria
    {
        /// <summary>
        /// Gets or sets the trace ID to search for.
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// Gets or sets the session ID to search for.
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the agent ID to search for.
        /// </summary>
        public string? AgentId { get; set; }

        /// <summary>
        /// Gets or sets the agent type to search for.
        /// </summary>
        public string? AgentType { get; set; }

        /// <summary>
        /// Gets or sets the user ID to search for.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the organization ID to search for.
        /// </summary>
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the contact ID to search for.
        /// </summary>
        public string? ContactId { get; set; }

        /// <summary>
        /// Gets or sets the operation type to search for.
        /// </summary>
        public string? OperationType { get; set; }

        /// <summary>
        /// Gets or sets the status to search for.
        /// </summary>
        public OperationStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the minimum start date for the search.
        /// </summary>
        public DateTimeOffset? StartDateMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum start date for the search.
        /// </summary>
        public DateTimeOffset? StartDateMax { get; set; }

        /// <summary>
        /// Gets or sets whether to include only failed operations.
        /// </summary>
        public bool? OnlyFailures { get; set; }

        /// <summary>
        /// Gets or sets the search text to match against operation data.
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return.
        /// </summary>
        public int? Limit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the number of results to skip.
        /// </summary>
        public int? Offset { get; set; } = 0;

        /// <summary>
        /// Gets or sets the field to sort by.
        /// </summary>
        public string? SortBy { get; set; } = "StartedAt";

        /// <summary>
        /// Gets or sets whether to sort in descending order.
        /// </summary>
        public bool SortDescending { get; set; } = true;
    }
}
