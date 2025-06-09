using System.Collections.Generic;

namespace Emma.Core.Industry
{
    /// <summary>
    /// Defines industry-specific configurations and behaviors for EMMA
    /// </summary>
    public interface IIndustryProfile
    {
        /// <summary>
        /// Unique industry identifier (e.g., "RealEstate", "Mortgage", "Financial")
        /// </summary>
        string IndustryCode { get; }

        /// <summary>
        /// Human-readable industry name
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Industry-specific prompt templates for AI interactions
        /// </summary>
        IndustryPromptTemplates PromptTemplates { get; }

        /// <summary>
        /// Available EMMA actions for this industry
        /// </summary>
        List<string> AvailableActions { get; }

        /// <summary>
        /// Specialized AI agents available for this industry
        /// </summary>
        List<string> SpecializedAgents { get; }

        /// <summary>
        /// Industry-specific NBA action types
        /// </summary>
        List<string> NbaActionTypes { get; }

        /// <summary>
        /// Industry-specific contact states and workflows
        /// </summary>
        ContactWorkflowDefinitions WorkflowDefinitions { get; }

        /// <summary>
        /// Custom fields and terminology specific to this industry
        /// </summary>
        IndustryConfiguration Configuration { get; }

        /// <summary>
        /// Sample queries and use cases for this industry
        /// </summary>
        List<IndustrySampleQuery> SampleQueries { get; }
    }

    /// <summary>
    /// Industry-specific AI prompt templates
    /// </summary>
    public class IndustryPromptTemplates
    {
        /// <summary>
        /// Base system prompt that defines EMMA's role in this industry
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Template for building context from contact data
        /// </summary>
        public string ContextPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Templates for common query types (e.g., "hot_leads", "follow_ups")
        /// </summary>
        public Dictionary<string, string> QueryTemplates { get; set; } = new();

        /// <summary>
        /// Templates for action-oriented prompts
        /// </summary>
        public Dictionary<string, string> ActionPrompts { get; set; } = new();

        /// <summary>
        /// Industry-specific terminology and language patterns
        /// </summary>
        public Dictionary<string, string> Terminology { get; set; } = new();
    }

    /// <summary>
    /// Defines contact states and workflows for an industry
    /// </summary>
    public class ContactWorkflowDefinitions
    {
        /// <summary>
        /// Valid contact states for this industry (e.g., Lead, Prospect, Client)
        /// </summary>
        public List<string> ContactStates { get; set; } = new();

        /// <summary>
        /// Workflow automation triggers and rules
        /// </summary>
        public List<WorkflowTrigger> AutomationTriggers { get; set; } = new();

        /// <summary>
        /// Next Best Action definitions by contact state
        /// </summary>
        public Dictionary<string, List<string>> NBAByState { get; set; } = new();

        /// <summary>
        /// Typical contact lifecycle progression
        /// </summary>
        public List<ContactStateTransition> StateTransitions { get; set; } = new();
    }

    /// <summary>
    /// Industry-specific configuration and customizations
    /// </summary>
    public class IndustryConfiguration
    {
        /// <summary>
        /// Custom field definitions
        /// </summary>
        public Dictionary<string, string> CustomFields { get; set; } = new();

        /// <summary>
        /// Industry-specific contact properties
        /// </summary>
        public Dictionary<string, object> ContactProperties { get; set; } = new();

        /// <summary>
        /// Communication preferences and templates
        /// </summary>
        public Dictionary<string, string> CommunicationTemplates { get; set; } = new();

        /// <summary>
        /// Integration-specific settings
        /// </summary>
        public Dictionary<string, object> IntegrationSettings { get; set; } = new();
    }

    /// <summary>
    /// Sample queries to demonstrate industry capabilities
    /// </summary>
    public class IndustrySampleQuery
    {
        public string Query { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Defines workflow automation triggers
    /// </summary>
    public class WorkflowTrigger
    {
        public string Name { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public int Priority { get; set; }
    }

    /// <summary>
    /// Defines valid contact state transitions
    /// </summary>
    public class ContactStateTransition
    {
        public string FromState { get; set; } = string.Empty;
        public string ToState { get; set; } = string.Empty;
        public List<string> RequiredActions { get; set; } = new();
        public bool IsAutomatic { get; set; }
    }
}
