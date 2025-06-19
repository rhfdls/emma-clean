using System.Collections.Generic;

namespace Emma.Models.Interfaces;

/// <summary>
/// Interface that all agent actions must implement for standardized validation
/// </summary>
public interface IAgentAction
{
    string ActionType { get; set; }
    string Description { get; set; }
    int Priority { get; set; }
    double ConfidenceScore { get; set; }
    string ValidationReason { get; set; }
    bool RequiresApproval { get; set; }
    string ApprovalRequestId { get; set; }
    Dictionary<string, object> Parameters { get; set; }
}
