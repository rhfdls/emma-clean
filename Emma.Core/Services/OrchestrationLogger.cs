using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Emma.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Emma.Core.Services
{
    /// <summary>
    /// Implementation of <see cref="IOrchestrationLogger"/> that logs agent orchestration activities to a data store.
    /// </summary>
    public class OrchestrationLogger : IOrchestrationLogger, IDisposable
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<OrchestrationLogger> _logger;
        private readonly OrchestrationLoggerOptions _options;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationLogger"/> class.
        /// </summary>
        public OrchestrationLogger(
            IAppDbContext context,
            ILogger<OrchestrationLogger> logger,
            IOptions<OrchestrationLoggerOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public async Task<OrchestrationLog> StartOperationAsync(string operationType, AgentContext context, object? input = null)
        {
            if (string.IsNullOrEmpty(operationType))
                throw new ArgumentException("Operation type is required", nameof(operationType));
            
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var log = OrchestrationLog.StartOperation(operationType, context, input);
            
            try
            {
                _context.OrchestrationLogs.Add(log);
                await _context.SaveChangesAsync();
                return log;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log operation start. Operation: {OperationType}, Agent: {AgentId}", 
                    operationType, context.AgentId);
                throw new OrchestrationLoggingException("Failed to log operation start", ex);
            }
        }

        /// <inheritdoc />
        public async Task CompleteOperationAsync(string logId, object? output = null)
        {
            if (string.IsNullOrEmpty(logId))
                throw new ArgumentException("Log ID is required", nameof(logId));

            var log = await _context.OrchestrationLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("Could not find log entry with ID {LogId} to mark as complete", logId);
                return;
            }

            log.Complete(output);
            
            try
            {
                _context.OrchestrationLogs.Update(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark operation as complete. Log ID: {LogId}", logId);
                throw new OrchestrationLoggingException("Failed to mark operation as complete", ex);
            }
        }

        /// <inheritdoc />
        public async Task FailOperationAsync(string logId, Exception exception)
        {
            if (string.IsNullOrEmpty(logId))
                throw new ArgumentException("Log ID is required", nameof(logId));
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            var log = await _context.OrchestrationLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("Could not find log entry with ID {LogId} to mark as failed", logId);
                return;
            }

            log.Fail(exception);
            
            try
            {
                _context.OrchestrationLogs.Update(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark operation as failed. Log ID: {LogId}", logId);
                throw new OrchestrationLoggingException("Failed to mark operation as failed", ex);
            }
        }

        /// <inheritdoc />
        public async Task<OrchestrationLog?> GetLogByIdAsync(string logId)
        {
            if (string.IsNullOrEmpty(logId))
                throw new ArgumentException("Log ID is required", nameof(logId));

            return await _context.OrchestrationLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == logId);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrchestrationLog>> GetLogsByTraceIdAsync(string traceId, bool includeRelated = true)
        {
            if (string.IsNullOrEmpty(traceId))
                throw new ArgumentException("Trace ID is required", nameof(traceId));

            var query = _context.OrchestrationLogs
                .AsNoTracking()
                .Where(x => x.TraceId == traceId);

            if (!includeRelated)
            {
                query = query.Where(x => x.ParentLogId == null);
            }

            return await query
                .OrderBy(x => x.StartedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrchestrationLog>> GetLogsBySessionIdAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentException("Session ID is required", nameof(sessionId));

            return await _context.OrchestrationLogs
                .AsNoTracking()
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.StartedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrchestrationLog>> GetLogsByAgentIdAsync(
            string agentId, 
            DateTimeOffset? startDate = null, 
            DateTimeOffset? endDate = null)
        {
            if (string.IsNullOrEmpty(agentId))
                throw new ArgumentException("Agent ID is required", nameof(agentId));

            var query = _context.OrchestrationLogs
                .AsNoTracking()
                .Where(x => x.AgentId == agentId);

            if (startDate.HasValue)
                query = query.Where(x => x.StartedAt >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(x => x.StartedAt <= endDate.Value);

            return await query
                .OrderByDescending(x => x.StartedAt)
                .Take(_options.MaxQueryResults)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrchestrationLog>> GetLogsByUserIdAsync(
            Guid userId, 
            DateTimeOffset? startDate = null, 
            DateTimeOffset? endDate = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID is required", nameof(userId));

            var query = _context.OrchestrationLogs
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(x => x.StartedAt >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(x => x.StartedAt <= endDate.Value);

            return await query
                .OrderByDescending(x => x.StartedAt)
                .Take(_options.MaxQueryResults)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrchestrationLog>> GetLogsByContactIdAsync(
            string contactId, 
            DateTimeOffset? startDate = null, 
            DateTimeOffset? endDate = null)
        {
            if (string.IsNullOrEmpty(contactId))
                throw new ArgumentException("Contact ID is required", nameof(contactId));

            var query = _context.OrchestrationLogs
                .AsNoTracking()
                .Where(x => x.ContactId == contactId);

            if (startDate.HasValue)
                query = query.Where(x => x.StartedAt >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(x => x.StartedAt <= endDate.Value);

            return await query
                .OrderByDescending(x => x.StartedAt)
                .Take(_options.MaxQueryResults)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<OrchestrationLog>> SearchLogsAsync(OrchestrationLogSearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            var query = _context.OrchestrationLogs
                .AsNoTracking()
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.TraceId))
                query = query.Where(x => x.TraceId == criteria.TraceId);
                
            if (!string.IsNullOrEmpty(criteria.SessionId))
                query = query.Where(x => x.SessionId == criteria.SessionId);
                
            if (!string.IsNullOrEmpty(criteria.AgentId))
                query = query.Where(x => x.AgentId == criteria.AgentId);
                
            if (!string.IsNullOrEmpty(criteria.AgentType))
                query = query.Where(x => x.AgentType == criteria.AgentType);
                
            if (criteria.UserId.HasValue)
                query = query.Where(x => x.UserId == criteria.UserId);
                
            if (!string.IsNullOrEmpty(criteria.OrganizationId))
                query = query.Where(x => x.OrganizationId == criteria.OrganizationId);
                
            if (!string.IsNullOrEmpty(criteria.ContactId))
                query = query.Where(x => x.ContactId == criteria.ContactId);
                
            if (!string.IsNullOrEmpty(criteria.OperationType))
                query = query.Where(x => x.OperationType == criteria.OperationType);
                
            if (criteria.Status.HasValue)
                query = query.Where(x => x.Status == criteria.Status);
                
            if (criteria.StartDateMin.HasValue)
                query = query.Where(x => x.StartedAt >= criteria.StartDateMin.Value);
                
            if (criteria.StartDateMax.HasValue)
                query = query.Where(x => x.StartedAt <= criteria.StartDateMax.Value);
                
            if (criteria.OnlyFailures == true)
                query = query.Where(x => x.Status == OperationStatus.Failed);
                
            if (!string.IsNullOrEmpty(criteria.SearchText))
            {
                var searchText = criteria.SearchText.ToLower();
                query = query.Where(x => 
                    (x.OperationType != null && x.OperationType.ToLower().Contains(searchText)) ||
                    (x.ErrorMessage != null && x.ErrorMessage.ToLower().Contains(searchText)) ||
                    (x.InputData != null && x.InputData.ToLower().Contains(searchText)) ||
                    (x.OutputData != null && x.OutputData.ToLower().Contains(searchText)));
            }

            // Apply sorting
            query = criteria.SortBy?.ToLower() switch
            {
                "startedat" => criteria.SortDescending 
                    ? query.OrderByDescending(x => x.StartedAt)
                    : query.OrderBy(x => x.StartedAt),
                "operationtype" => criteria.SortDescending
                    ? query.OrderByDescending(x => x.OperationType)
                    : query.OrderBy(x => x.OperationType),
                "status" => criteria.SortDescending
                    ? query.OrderByDescending(x => x.Status)
                    : query.OrderBy(x => x.Status),
                _ => query.OrderByDescending(x => x.StartedAt)
            };

            // Apply pagination
            if (criteria.Offset.HasValue && criteria.Offset.Value > 0)
                query = query.Skip(criteria.Offset.Value);
                
            if (criteria.Limit.HasValue && criteria.Limit.Value > 0)
                query = query.Take(Math.Min(criteria.Limit.Value, _options.MaxQueryResults));
            else
                query = query.Take(_options.MaxQueryResults);

            return await query.ToListAsync();
        }

        /// <inheritdoc />
        public async Task<int> ArchiveLogsAsync(DateTimeOffset olderThan)
        {
            if (olderThan > DateTimeOffset.UtcNow.AddDays(-1))
            {
                _logger.LogWarning("Attempted to archive logs newer than 1 day. This operation is not allowed.");
                return 0;
            }

            try
            {
                var logsToArchive = await _context.OrchestrationLogs
                    .Where(x => x.StartedAt < olderThan)
                    .ToListAsync();

                if (!logsToArchive.Any())
                    return 0;

                // In a real implementation, you would move these logs to cold storage
                // For this example, we'll just delete them
                _context.OrchestrationLogs.RemoveRange(logsToArchive);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Archived {Count} logs older than {OlderThan}", 
                    logsToArchive.Count, olderThan);

                return logsToArchive.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive logs older than {OlderThan}", olderThan);
                throw new OrchestrationLoggingException("Failed to archive logs", ex);
            }
        }

        /// <summary>
        /// Disposes the logger and releases any resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the logger and releases any resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (_context is IDisposable disposableContext)
                    {
                        disposableContext.Dispose();
                    }
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Options for configuring the OrchestrationLogger.
    /// </summary>
    public class OrchestrationLoggerOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of results to return from a query.
        /// </summary>
        public int MaxQueryResults { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable archiving of old logs.
        /// </summary>
        public bool EnableArchiving { get; set; } = true;

        /// <summary>
        /// Gets or sets the age at which logs should be archived.
        /// </summary>
        public TimeSpan ArchiveAfter { get; set; } = TimeSpan.FromDays(30);
    }

    /// <summary>
    /// Exception thrown when an error occurs during orchestration logging.
    /// </summary>
    [Serializable]
    public class OrchestrationLoggingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationLoggingException"/> class.
        /// </summary>
        public OrchestrationLoggingException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationLoggingException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public OrchestrationLoggingException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationLoggingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public OrchestrationLoggingException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationLoggingException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected OrchestrationLoggingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
