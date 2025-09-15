using System;
using System.Collections.Generic;
using Emma.Models.Interfaces;

namespace Emma.Models.Models
{
    /// <summary>
    /// Represents a Next Best Action (NBA) recommendation for a contact
    /// </summary>
    public class NbaRecommendation : IAgentAction
    {
        // Core Properties
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ContactId { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // Email, Call, Task, etc.
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
        
        // IAgentAction implementation - using explicit interface implementation to avoid conflicts
        string IAgentAction.ValidationReason { get => ValidationReason ?? string.Empty; set => ValidationReason = value; }
        bool IAgentAction.RequiresApproval { get => RequiresApproval; set => RequiresApproval = value; }
        string IAgentAction.ApprovalRequestId { get => ApprovalRequestId ?? string.Empty; set => ApprovalRequestId = value; }
        Dictionary<string, object> IAgentAction.Parameters { get => Parameters ??= new(); set => Parameters = value ?? new(); }
        
        // Backing field for Parameters
        public Dictionary<string, object> Parameters { get; set; } = new();
        
        // Explicit interface implementation for Priority
        int IAgentAction.Priority { get => GetNumericPriority(Priority); set => Priority = GetPriorityString(value); }
        
        private int GetNumericPriority(string priority) => priority?.ToLower() switch
        {
            "low" => 1,
            "medium" => 2,
            "high" => 3,
            "critical" => 4,
            _ => 2 // default to Medium
        };
        
        private string GetPriorityString(int priority) => priority switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Critical",
            _ => "Medium" // default to Medium
        };
        
        public DateTime? RecommendedTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        
        // Confidence and Validation
        public double ConfidenceScore { get; set; } = 0.0;
        public string? ValidationReason { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; }
        public string? ApprovalRequestId { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        
        // Status Tracking
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Completed, Expired
        public string? RejectionReason { get; set; }
        public bool IsExecuted { get; set; } = false;
        public DateTime? ExecutedAt { get; set; }
        
        // Context and Metadata
        public Dictionary<string, object> Context { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = "System";
        public string? UpdatedBy { get; set; }
        
        // Related Entities
        public string? RelatedTaskId { get; set; }
        public string? RelatedInteractionId { get; set; }
        
        // Helper Methods
        public bool IsExpired()
        {
            return ExpirationTime.HasValue && DateTime.UtcNow > ExpirationTime.Value;
        }
        
        public bool CanBeExecuted()
        {
            return !IsExpired() && 
                   Status == "Approved" && 
                   !IsExecuted &&
                   (!RequiresApproval || ApprovalRequestId != null);
        }
        
        public void MarkAsApproved(string approvedBy)
        {
            Status = "Approved";
            ApprovedBy = approvedBy;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = approvedBy;
        }
        
        public void MarkAsRejected(string rejectedBy, string reason)
        {
            Status = "Rejected";
            RejectionReason = reason;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = rejectedBy;
        }
        
        public void MarkAsExecuted()
        {
            IsExecuted = true;
            ExecutedAt = DateTime.UtcNow;
            Status = "Completed";
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
