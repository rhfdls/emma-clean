namespace Emma.Api.Models;

public class AnalyzeInteractionRequest
{
    public string InteractionText { get; set; } = "";
    public string? ClientContext { get; set; }
}

public class GetRecommendationRequest
{
    public string ClientSummary { get; set; } = "";
    public string RecentInteractions { get; set; } = "";
    public string? DealStage { get; set; }
}

public class UpdateSummaryRequest
{
    public string? ExistingSummary { get; set; }
    public string NewInteractions { get; set; } = "";
    public string? ClientProfile { get; set; }
}
