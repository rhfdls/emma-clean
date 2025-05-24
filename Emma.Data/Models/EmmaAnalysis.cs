using System.Text.Json.Serialization;
using Emma.Data.Enums;

namespace Emma.Data.Models;

public class EmmaAnalysis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
    [JsonPropertyName("lead_status")]
    public string LeadStatus { get; set; } = string.Empty;
    [JsonPropertyName("recommended_strategy")]
    public string RecommendedStrategy { get; set; } = string.Empty;
    [JsonPropertyName("tasks_list")]
    public List<EmmaTask> TasksList { get; set; } = new();
    [JsonPropertyName("agent_assignments")]
    public List<AgentAssignment> AgentAssignments { get; set; } = new();
    [JsonPropertyName("compliance_flags")]
    public List<string> ComplianceFlags { get; set; } = new();
    [JsonPropertyName("followup_guidance")]
    public string FollowupGuidance { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
