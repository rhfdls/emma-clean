using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emma.Models.Enums;

namespace Emma.Core.Models
{
    /// <summary>
    /// Represents a log entry for agent orchestration activities.
    /// Used for auditing, debugging, and compliance purposes.
    /// </summary>
    public class OrchestrationLog
    {
        /// <summary>
        /// Gets or sets the unique identifier for this log entry.
        /// </summary>
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the trace ID that links related operations across services.
        /// </summary>
        [Required]
        public string TraceId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the session ID that links operations within a single user session.
        /// </summary>
        [Required]
        public string SessionId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the agent that performed the operation.
        /// </summary>
        [Required]
        public string AgentId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the type of the agent (e.g., NBA, ContextIntelligence, etc.).
        /// </summary>
        [Required]
        public string AgentType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the user who initiated the operation, if any.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the organization this operation belongs to.
        /// </summary>
        [Required]
        public string OrganizationId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the ID of the contact this operation relates to, if any.
        /// </summary>
        public string? ContactId { get; set; }

        /// <summary>
        /// Gets or sets the type of operation performed.
        /// </summary>
        [Required]
        public string OperationType { get; set; } = null!;

        /// <summary>
        /// Gets or sets the status of the operation.
        /// </summary>
        public OperationStatus Status { get; set; } = OperationStatus.Started;

        /// <summary>
        /// Gets or sets the input data for the operation.
        /// </summary>
        [JsonIgnore]
        public string? InputData { get; set; }

        /// <summary>
        /// Gets or sets the output data from the operation.
        /// </summary>
        [JsonIgnore]
        public string? OutputData { get; set; }

        /// <summary>
        /// Gets or sets the error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the stack trace if the operation failed.
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the duration of the operation in milliseconds.
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the operation started.
        /// </summary>
        [Required]
        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the operation completed.
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the IP address where the request originated from.
        /// </summary>
        public string? SourceIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the user agent string of the client making the request.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Gets or sets the ID of the parent operation, if this is a child operation.
        /// </summary>
        public string? ParentLogId { get; set; }

        /// <summary>
        /// Gets or sets the correlation ID for tracking related operations across services.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the operation.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Creates a new log entry for the start of an operation.
        /// </summary>
        public static OrchestrationLog StartOperation(
            string operationType,
            AgentContext context,
            object? input = null)
        {
            var log = new OrchestrationLog
            {
                TraceId = context.TraceId ?? Guid.NewGuid().ToString(),
                SessionId = context.SessionId ?? Guid.NewGuid().ToString(),
                AgentId = context.AgentId,
                AgentType = context.AgentType,
                UserId = context.UserId,
                OrganizationId = context.OrganizationId,
                ContactId = context.ContactId,
                OperationType = operationType,
                Status = OperationStatus.Started,
                SourceIpAddress = context.SourceIpAddress,
                UserAgent = context.UserAgent,
                CorrelationId = context.CorrelationId,
                StartedAt = DateTimeOffset.UtcNow
            };

            if (input != null)
            {
                log.InputData = JsonSerializer.Serialize(input, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            return log;
        }

        /// <summary>
        /// Marks the operation as completed successfully.
        /// </summary>
        public OrchestrationLog Complete(object? output = null)
        {
            Status = OperationStatus.Completed;
            CompletedAt = DateTimeOffset.UtcNow;
            DurationMs = (long)(CompletedAt - StartedAt)?.TotalMilliseconds;

            if (output != null)
            {
                OutputData = JsonSerializer.Serialize(output, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            return this;
        }

        /// <summary>
        /// Marks the operation as failed with the specified error.
        /// </summary>
        public OrchestrationLog Fail(Exception exception)
        {
            Status = OperationStatus.Failed;
            ErrorMessage = exception.Message;
            StackTrace = exception.StackTrace;
            CompletedAt = DateTimeOffset.UtcNow;
            DurationMs = (long)(CompletedAt - StartedAt)?.TotalMilliseconds;

            return this;
        }

        /// <summary>
        /// Creates a child log entry for a sub-operation.
        /// </summary>
        public OrchestrationLog CreateChildLog(string operationType, AgentContext childContext, object? input = null)
        {
            var childLog = StartOperation(operationType, childContext, input);
            childLog.ParentLogId = this.Id;
            childLog.CorrelationId = this.CorrelationId;
            
            // Copy over any relevant metadata
            foreach (var kvp in this.Metadata)
            {
                childLog.Metadata[kvp.Key] = kvp.Value;
            }
            
            return childLog;
        }
    }

    /// <summary>
    /// Represents the status of an orchestration operation.
    /// </summary>
    public enum OperationStatus
    {
        /// <summary>
        /// The operation has started but not yet completed.
        /// </summary>
        Started,
        
        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        Completed,
        
        /// <summary>
        /// The operation failed with an error.
        /// </summary>
        Failed,
        
        /// <summary>
        /// The operation was canceled before completion.
        /// </summary>
        Canceled,
        
        /// <summary>
        /// The operation timed out.
        /// </summary>
        TimedOut,
        
        /// <summary>
        /// The operation was not authorized.
        /// </summary>
        Unauthorized,
        
        /// <summary>
        /// The operation was skipped because it was not needed.
        /// </summary>
        Skipped
    }
}
