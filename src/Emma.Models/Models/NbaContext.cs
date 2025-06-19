namespace Emma.Models.Models;

/// <summary>
/// Curated context for Next Best Action (NBA) recommendations
/// </summary>
public class NbaContext
{
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    
    public ContactSummary? RollingSummary { get; set; }
    public ContactState? CurrentState { get; set; }
    
    public List<Interaction> RecentInteractions { get; set; } = new();
    public List<RelevantInteraction> RelevantInteractions { get; set; } = new();
    public List<ContactAssignment> ActiveContactAssignments { get; set; } = new();
    
    public NbaContextMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata about NBA context generation
/// </summary>
public class NbaContextMetadata
{
    public DateTime GeneratedAt { get; set; }
    public int TotalInteractionCount { get; set; }
    public int RelevantInteractionCount { get; set; }
    public int ActiveAssignmentCount { get; set; }
    public string? ContextVersion { get; set; }
    public bool IncludesSqlContext { get; set; }
}
