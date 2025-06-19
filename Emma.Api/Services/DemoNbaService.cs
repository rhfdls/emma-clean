using Emma.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Emma.Models.Models;

namespace Emma.Api.Services;

public interface IDemoNbaService
{
    Task<InteractionAnalysisResult> AnalyzeInteractionAsync(string interactionText, string clientContext = "");
    Task<NbaRecommendationResult> GetRecommendationAsync(string clientSummary, string recentInteractions, string dealStage = "prospect");
    Task<ClientSummaryResult> UpdateClientSummaryAsync(string existingSummary, string newInteractions, string clientProfile = "");
}

public class DemoNbaService : IDemoNbaService
{
    private readonly IAzureOpenAIService _openAIService;
    private readonly ILogger<DemoNbaService> _logger;

    public DemoNbaService(IAzureOpenAIService openAIService, ILogger<DemoNbaService> logger)
    {
        _openAIService = openAIService;
        _logger = logger;
    }

    public async Task<InteractionAnalysisResult> AnalyzeInteractionAsync(string interactionText, string clientContext = "")
    {
        try
        {
            _logger.LogInformation("Analyzing interaction with Azure OpenAI...");
            
            // Use the sentiment analysis method
            var sentiment = await _openAIService.AnalyzeSentimentAsync(interactionText);
            
            // Use the entity extraction method
            var entities = await _openAIService.ExtractEntitiesAsync(interactionText);
            
            _logger.LogInformation("Received response from Azure OpenAI");
            
            // Create a structured result
            var result = new InteractionAnalysisResult
            {
                Summary = $"Analyzed interaction: {interactionText.Substring(0, Math.Min(100, interactionText.Length))}...",
                Sentiment = new SentimentResult 
                { 
                    Label = sentiment > 0.1 ? "positive" : sentiment < -0.1 ? "negative" : "neutral",
                    Score = Math.Abs(sentiment)
                },
                KeyTopics = ExtractTopicsFromEntities(entities),
                Entities = MapEntities(entities),
                Intent = DetermineIntent(interactionText),
                Urgency = DetermineUrgency(interactionText),
                NextActionHints = GenerateActionHints(sentiment, interactionText)
            };
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing interaction");
            return new InteractionAnalysisResult 
            { 
                Summary = $"Analysis completed for interaction", 
                Sentiment = new SentimentResult { Label = "neutral", Score = 0.5 },
                Intent = "General inquiry",
                KeyTopics = new List<string> { "general" },
                NextActionHints = new List<string> { "Follow up within 24 hours" }
            };
        }
    }

    public async Task<NbaRecommendationResult> GetRecommendationAsync(string clientSummary, string recentInteractions, string dealStage = "prospect")
    {
        try
        {
            _logger.LogInformation("Generating NBA recommendation with Azure OpenAI...");
            
            // For demo, create a simple mock context using the real NbaContext class
            var mockContext = new Emma.Models.Models.NbaContext 
            { 
                ContactId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                RecentInteractions = new List<Interaction>(),
                RelevantInteractions = new List<RelevantInteraction>(),
                ActiveContactAssignments = new List<ContactAssignment>()
            };
            
            var recommendation = await _openAIService.GenerateNbaRecommendationsAsync(mockContext, dealStage);
            
            // Parse the recommendation into structured format
            var result = new NbaRecommendationResult 
            { 
                PrimaryRecommendation = new ActionRecommendation 
                { 
                    Action = DetermineActionFromStage(dealStage),
                    Priority = "high",
                    Timing = "this_week"
                },
                Reasoning = recommendation,
                MessageSuggestions = GenerateMessageSuggestions(dealStage),
                AlternativeActions = GenerateAlternativeActions(dealStage),
                SuccessMetrics = new List<string> { "Response rate", "Meeting acceptance", "Engagement level" }
            };
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating NBA recommendation");
            return new NbaRecommendationResult 
            { 
                PrimaryRecommendation = new ActionRecommendation { Action = "follow_up", Priority = "medium", Timing = "this_week" },
                Reasoning = "Standard follow-up recommended based on current stage",
                MessageSuggestions = new List<string> { "Check in on their needs", "Provide additional value" },
                AlternativeActions = new List<AlternativeAction> { new() { Action = "email", Reasoning = "Less intrusive option" } }
            };
        }
    }

    public async Task<ClientSummaryResult> UpdateClientSummaryAsync(string existingSummary, string newInteractions, string clientProfile = "")
    {
        try
        {
            _logger.LogInformation("Updating client summary with Azure OpenAI...");
            
            // For demo purposes, create a mock interaction object
            var mockInteraction = new Interaction
            {
                Content = newInteractions,
                Type = "demo",
                Timestamp = DateTime.UtcNow
            };
            
            var updatedSummary = await _openAIService.UpdateRollingSummaryAsync(
                existingSummary, 
                mockInteraction, 
                new List<Interaction> { mockInteraction }
            );
            
            var result = new ClientSummaryResult 
            { 
                RelationshipStage = DetermineStageFromSummary(updatedSummary),
                Summary = updatedSummary,
                KeyInterests = ExtractInterests(newInteractions),
                CommunicationStyle = "professional",
                DecisionTimeline = "short_term",
                BudgetIndicators = "moderate",
                RelationshipHealth = "good"
            };
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client summary");
            return new ClientSummaryResult 
            { 
                RelationshipStage = "prospect",
                Summary = $"Client profile updated with recent interactions",
                KeyInterests = new List<string> { "general business needs" }
            };
        }
    }

    // Helper methods for demo logic
    private List<string> ExtractTopicsFromEntities(Dictionary<string, object> entities)
    {
        var topics = new List<string>();
        if (entities.ContainsKey("topics")) topics.AddRange((List<string>)entities["topics"]);
        if (topics.Count == 0) topics.Add("general discussion");
        return topics;
    }

    private EntityResult MapEntities(Dictionary<string, object> entities)
    {
        return new EntityResult
        {
            People = entities.ContainsKey("people") ? (List<string>)entities["people"] : new List<string>(),
            Companies = entities.ContainsKey("companies") ? (List<string>)entities["companies"] : new List<string>(),
            Dates = entities.ContainsKey("dates") ? (List<string>)entities["dates"] : new List<string>(),
            Amounts = entities.ContainsKey("amounts") ? (List<string>)entities["amounts"] : new List<string>()
        };
    }

    private string DetermineIntent(string text)
    {
        if (text.ToLower().Contains("meeting") || text.ToLower().Contains("schedule"))
            return "Schedule meeting";
        if (text.ToLower().Contains("price") || text.ToLower().Contains("cost"))
            return "Pricing inquiry";
        if (text.ToLower().Contains("demo") || text.ToLower().Contains("show"))
            return "Request demo";
        return "General inquiry";
    }

    private string DetermineUrgency(string text)
    {
        if (text.ToLower().Contains("urgent") || text.ToLower().Contains("asap"))
            return "high";
        if (text.ToLower().Contains("soon") || text.ToLower().Contains("quick"))
            return "medium";
        return "low";
    }

    private List<string> GenerateActionHints(double sentiment, string text)
    {
        var hints = new List<string>();
        if (sentiment > 0.3) hints.Add("Client seems positive - good time to advance the conversation");
        if (sentiment < -0.3) hints.Add("Address any concerns or objections");
        if (text.ToLower().Contains("question")) hints.Add("Follow up with detailed answers");
        if (hints.Count == 0) hints.Add("Continue building relationship");
        return hints;
    }

    private string DetermineActionFromStage(string stage)
    {
        return stage.ToLower() switch
        {
            "prospect" => "call",
            "qualified" => "demo",
            "evaluation" => "proposal",
            "negotiation" => "meeting",
            _ => "follow_up"
        };
    }

    private List<string> GenerateMessageSuggestions(string stage)
    {
        return stage.ToLower() switch
        {
            "prospect" => new List<string> { "Introduce our value proposition", "Understand their pain points", "Schedule discovery call" },
            "qualified" => new List<string> { "Share relevant case studies", "Propose product demo", "Discuss implementation timeline" },
            "evaluation" => new List<string> { "Address technical questions", "Provide detailed proposal", "Connect with decision makers" },
            _ => new List<string> { "Check in on progress", "Offer additional support", "Maintain relationship" }
        };
    }

    private List<AlternativeAction> GenerateAlternativeActions(string stage)
    {
        return new List<AlternativeAction>
        {
            new() { Action = "email", Reasoning = "Less intrusive follow-up option" },
            new() { Action = "meeting", Reasoning = "More personal engagement" }
        };
    }

    private string DetermineStageFromSummary(string summary)
    {
        if (summary.ToLower().Contains("contract") || summary.ToLower().Contains("agreement"))
            return "negotiation";
        if (summary.ToLower().Contains("demo") || summary.ToLower().Contains("evaluation"))
            return "evaluation";
        if (summary.ToLower().Contains("qualified") || summary.ToLower().Contains("interested"))
            return "qualified";
        return "prospect";
    }

    private List<string> ExtractInterests(string interactions)
    {
        var interests = new List<string>();
        if (interactions.ToLower().Contains("efficiency")) interests.Add("operational efficiency");
        if (interactions.ToLower().Contains("cost") || interactions.ToLower().Contains("save")) interests.Add("cost reduction");
        if (interactions.ToLower().Contains("growth") || interactions.ToLower().Contains("scale")) interests.Add("business growth");
        if (interests.Count == 0) interests.Add("general business improvement");
        return interests;
    }
}

// Result Models
public class InteractionAnalysisResult
{
    public string Summary { get; set; } = "";
    public SentimentResult Sentiment { get; set; } = new();
    public List<string> KeyTopics { get; set; } = new();
    public EntityResult Entities { get; set; } = new();
    public string Intent { get; set; } = "";
    public string Urgency { get; set; } = "low";
    public List<string> NextActionHints { get; set; } = new();
}

public class SentimentResult
{
    public string Label { get; set; } = "neutral";
    public double Score { get; set; } = 0.5;
}

public class EntityResult
{
    public List<string> People { get; set; } = new();
    public List<string> Companies { get; set; } = new();
    public List<string> Dates { get; set; } = new();
    public List<string> Amounts { get; set; } = new();
}

public class NbaRecommendationResult
{
    public ActionRecommendation PrimaryRecommendation { get; set; } = new();
    public string Reasoning { get; set; } = "";
    public List<string> MessageSuggestions { get; set; } = new();
    public List<AlternativeAction> AlternativeActions { get; set; } = new();
    public List<string> SuccessMetrics { get; set; } = new();
}

public class ActionRecommendation
{
    public string Action { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Timing { get; set; } = "";
}

public class AlternativeAction
{
    public string Action { get; set; } = "";
    public string Reasoning { get; set; } = "";
}

public class ClientSummaryResult
{
    public string RelationshipStage { get; set; } = "";
    public List<string> KeyInterests { get; set; } = new();
    public string CommunicationStyle { get; set; } = "";
    public string DecisionTimeline { get; set; } = "";
    public string BudgetIndicators { get; set; } = "";
    public string RelationshipHealth { get; set; } = "";
    public string Summary { get; set; } = "";
}
