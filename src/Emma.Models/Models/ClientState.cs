using System.ComponentModel.DataAnnotations;

namespace Emma.Models.Models;

/// <summary>
/// Current state and stage of client in the sales/service process
/// </summary>
public class ClientState
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    public Guid ContactId { get; set; }
    public Guid OrganizationId { get; set; }
    
    /// <summary>
    /// Current stage in the client journey
    /// </summary>
    public string CurrentStage { get; set; } = "Initial Contact";
    
    /// <summary>
    /// Next milestone or goal
    /// </summary>
    public string? NextMilestone { get; set; }
    
    /// <summary>
    /// Priority level (High, Medium, Low)
    /// </summary>
    public string Priority { get; set; } = "Medium";
    
    /// <summary>
    /// User assigned to this client
    /// </summary>
    public Guid? AssignedUserId { get; set; }
    
    /// <summary>
    /// Navigation property for the assigned user
    /// </summary>
    public User? AssignedUser { get; set; }
    
    /// <summary>
    /// Pending tasks or follow-ups
    /// </summary>
    public List<string> PendingTasks { get; set; } = new();
    
    /// <summary>
    /// Open objections or concerns
    /// </summary>
    public List<string> OpenObjections { get; set; } = new();
    
    /// <summary>
    /// Important dates and deadlines
    /// </summary>
    public Dictionary<string, DateTime> ImportantDates { get; set; } = new();
    
    /// <summary>
    /// Property-related information
    /// </summary>
    public Dictionary<string, object> PropertyInfo { get; set; } = new();
    
    /// <summary>
    /// Financial information and constraints
    /// </summary>
    public Dictionary<string, object> FinancialInfo { get; set; } = new();
    
    /// <summary>
    /// Custom fields for additional state information
    /// </summary>
    public Dictionary<string, object> CustomFields { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Contact Contact { get; set; } = null!;
}
