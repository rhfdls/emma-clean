using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Emma.Models.Enums;

namespace Emma.Models.Models;

/// <summary>
/// Represents an analysis performed by EMMA on a message, including AI-generated insights,
/// recommended actions, and compliance information.
/// </summary>
public class EmmaAnalysis : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the message this analysis is associated with.
    /// </summary>
    [Required]
    public Guid MessageId { get; set; }
    
    /// <summary>
    /// Gets or sets the message this analysis is associated with.
    /// </summary>
    [ForeignKey(nameof(MessageId))]
    public virtual Message? Message { get; set; }
    
    /// <summary>
    /// Gets or sets the lead status determined by the analysis.
    /// </summary>
    [Required]
    [MaxLength(50)]
    [JsonPropertyName("lead_status")]
    public string LeadStatus { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the recommended strategy based on the analysis.
    /// </summary>
    [Required]
    [MaxLength(500)]
    [JsonPropertyName("recommended_strategy")]
    public string RecommendedStrategy { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of tasks recommended by the analysis.
    /// </summary>
    [JsonPropertyName("tasks_list")]
    public virtual ICollection<EmmaTask> TasksList { get; set; } = new List<EmmaTask>();
    
    /// <summary>
    /// Gets or sets the list of agent assignments recommended by the analysis.
    /// </summary>
    [JsonPropertyName("agent_assignments")]
    public virtual ICollection<AgentAssignment> AgentAssignments { get; set; } = new List<AgentAssignment>();
    
    /// <summary>
    /// Gets or sets the list of compliance flags raised during analysis.
    /// </summary>
    [JsonPropertyName("compliance_flags")]
    public ICollection<string> ComplianceFlags { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets the follow-up guidance provided by the analysis.
    /// </summary>
    [MaxLength(2000)]
    [JsonPropertyName("followup_guidance")]
    public string FollowupGuidance { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the confidence score of the analysis (0-100).
    /// </summary>
    [Range(0, 100)]
    public int? ConfidenceScore { get; set; }
    
    /// <summary>
    /// Gets or sets the version of the analysis model used.
    /// </summary>
    [MaxLength(50)]
    public string? ModelVersion { get; set; }
    
    /// <summary>
    /// Gets or sets any additional metadata or context for the analysis.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}
