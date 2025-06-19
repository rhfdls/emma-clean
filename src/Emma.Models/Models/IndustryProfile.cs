namespace Emma.Models.Models;

/// <summary>
/// Industry-specific configuration for AI agents and workflows
/// </summary>
public class IndustryProfile
{
    public string IndustryCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public List<string> SpecializedAgents { get; set; } = new();
    public Dictionary<string, string> IndustryTerminology { get; set; } = new();
    public List<string> ResourceTypes { get; set; } = new();
    public List<string> ComplianceRequirements { get; set; } = new();
    public Dictionary<string, string> WorkflowTemplates { get; set; } = new();
    
    /// <summary>
    /// Industry-specific NBA action types
    /// </summary>
    public List<string> NbaActionTypes { get; set; } = new();
    
    /// <summary>
    /// Default resource categories for this industry
    /// </summary>
    public List<IndustryResourceCategory> DefaultResourceCategories { get; set; } = new();
}

/// <summary>
/// Resource category specific to an industry
/// </summary>
public class IndustryResourceCategory
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredSpecialties { get; set; } = new();
    public List<string> ComplianceChecks { get; set; } = new();
}
