using System.Text.Json.Serialization;
using Emma.Models;
using Emma.Models.Models;

namespace Emma.Core.Models
{
    // Base context envelope
    public class SqlContextData
    {
        public string ContextType { get; set; } = string.Empty; // "agent", "admin", "ai_workflow"
        public string SchemaVersion { get; set; } = "1.0";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public SecurityMetadata Security { get; set; } = new();
        
        // Role-specific data (only one will be populated based on requesting role)
        public AgentContext? Agent { get; set; }
        public AdminContext? Admin { get; set; }
        public AIWorkflowContext? AIWorkflow { get; set; }
    }

    // Security metadata for all contexts
    public class SecurityMetadata
    {
        public Guid RequestingAgentId { get; set; }
        public string RequestingRole { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public List<string> AppliedFilters { get; set; } = new();
        public string DataClassification { get; set; } = "Business"; // Business, Private, Confidential
    }

    // 1. AGENT/USER CONTEXT - Daily workflow focused
    // Duplicate AgentContext class definition removed. See AgentContext.cs for the canonical implementation.

    public class AssignedContact
    {
        public Guid ContactId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string RelationshipState { get; set; } = string.Empty; // Lead, Prospect, Client, etc.
        public string? CurrentStage { get; set; } // Pipeline stage
        public DateTime? LastInteraction { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool IsActiveClient { get; set; }
        public ContactPreferences? Preferences { get; set; }
    }

    public class ContactPreferences
    {
        public string? PreferredContactMethod { get; set; }
        public string? BestTimeToContact { get; set; }
        public bool EmailOptIn { get; set; } = true;
        public bool SmsOptIn { get; set; } = true;
        public bool DoNotContact { get; set; } = false;
    }

    public class RecentInteraction
    {
        public Guid InteractionId { get; set; }
        public Guid ContactId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Email, Call, SMS, Meeting
        public string Summary { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Sentiment { get; set; }
        public List<string> KeyTopics { get; set; } = new();
    }

    // TaskItem moved to Emma.Models namespace

    public class AgentPerformance
    {
        public int ContactsThisMonth { get; set; }
        public int InteractionsThisWeek { get; set; }
        public decimal ConversionRate { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksOverdue { get; set; }
    }

    public class ActivityTimelineItem
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty; // Interaction, Task, StateChange
        public string Description { get; set; } = string.Empty;
        public Guid? ContactId { get; set; }
        public string? ContactName { get; set; }
    }

    // 2. ADMIN/OWNER CONTEXT - Oversight and reporting focused
    public class AdminContext
    {
        public OrganizationKPIs KPIs { get; set; } = new();
        public List<AgentSummary> Agents { get; set; } = new();
        public List<AuditLogEntry> RecentAuditLogs { get; set; } = new();
        public SystemHealth Health { get; set; } = new();
        public SubscriptionInfo? Subscription { get; set; }
    }

    public class OrganizationKPIs
    {
        public int TotalContacts { get; set; }
        public int ActiveClients { get; set; }
        public int LeadsThisMonth { get; set; }
        public decimal OverallConversionRate { get; set; }
        public int TotalInteractionsThisWeek { get; set; }
        public Dictionary<string, int> ContactsByStage { get; set; } = new();
        public Dictionary<string, int> InteractionsByType { get; set; } = new();
    }

    public class AgentSummary
    {
        public Guid AgentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public int AssignedContacts { get; set; }
        public decimal ConversionRate { get; set; }
        public int InteractionsThisWeek { get; set; }
    }

    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? Details { get; set; }
    }

    public class SystemHealth
    {
        public bool DatabaseConnected { get; set; } = true;
        public bool EmailServiceConnected { get; set; } = true;
        public bool SmsServiceConnected { get; set; } = true;
        public int QueueBacklog { get; set; }
        public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    }

    public class SubscriptionInfo
    {
        public string PlanName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int AgentLimit { get; set; }
        public int ContactLimit { get; set; }
        public int CurrentAgentCount { get; set; }
        public int CurrentContactCount { get; set; }
        public string Status { get; set; } = "Active"; // Active, Expired, Suspended
    }

    // 3. AI WORKFLOW CONTEXT - Minimal data for automation
    public class AIWorkflowContext
    {
        public WorkflowContact Contact { get; set; } = new();
        public WorkflowDeal? Deal { get; set; }
        public WorkflowAgent AssignedAgent { get; set; } = new();
        public List<WorkflowTrigger> Triggers { get; set; } = new();
        public Dictionary<string, object> WorkflowData { get; set; } = new();
    }

    public class WorkflowContact
    {
        public Guid ContactId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string RelationshipState { get; set; } = string.Empty;
        public bool EmailOptIn { get; set; } = true;
        public bool SmsOptIn { get; set; } = true;
        public bool DoNotContact { get; set; } = false;
        public string? PreferredContactMethod { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class WorkflowDeal
    {
        public Guid DealId { get; set; }
        public string Stage { get; set; } = string.Empty;
        public decimal? Value { get; set; }
        public DateTime? CloseDate { get; set; }
        public string? NextAction { get; set; }
    }

    public class WorkflowAgent
    {
        public Guid AgentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class WorkflowTrigger
    {
        public string TriggerType { get; set; } = string.Empty; // TimeBasedFollowUp, StageChange, etc.
        public DateTime? ScheduledFor { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    // Enums for type safety
    public enum UserRole
    {
        Agent,
        Admin,
        Observer,
        AIWorkflow
    }

    public enum DataClassification
    {
        Business,
        Private,
        Confidential
    }

    public enum AccessLevel
    {
        Restricted,
        ReadOnly,
        Standard,
        Full
    }

    // Industry Profile for Agent Orchestrator
    public class IndustryProfile
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public List<string> ResourceTypes { get; set; } = new();
        public List<string> ComplianceRequirements { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}
