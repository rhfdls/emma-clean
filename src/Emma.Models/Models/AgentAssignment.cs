using System;

namespace Emma.Models.Models;

// AgentAssignment is a true entity with a foreign key to EmmaAnalysis
public class AgentAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Primary Key
    public Guid EmmaAnalysisId { get; set; }       // Foreign Key
    public EmmaAnalysis? EmmaAnalysis { get; set; } // Navigation property

    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public string AssignmentType { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
