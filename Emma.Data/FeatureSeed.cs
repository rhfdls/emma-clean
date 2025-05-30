using Emma.Data.Models;

namespace Emma.Data;

public static class FeatureSeed
{
    public static List<Feature> Features => new()
    {
        new Feature { Id = Guid.NewGuid(), Code = "ASK_EMMA", DisplayName = "Ask Emma", Description = "AI-powered question answering" },
        new Feature { Id = Guid.NewGuid(), Code = "SMS_CHATBOT", DisplayName = "SMS Chatbot", Description = "Automated SMS conversations" },
        new Feature { Id = Guid.NewGuid(), Code = "AGENT_ORCHESTRATION", DisplayName = "Agent Orchestration", Description = "Advanced agent workflow management" }
    };

    public static List<SubscriptionPlan> Plans => new()
    {
        new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            SubscriptionPlanFeatures = new List<SubscriptionPlanFeature>
            {
                new SubscriptionPlanFeature { FeatureId = Guid.NewGuid() }, // ASK_EMMA
                new SubscriptionPlanFeature { FeatureId = Guid.NewGuid() }  // SMS_CHATBOT
            }
        },
        new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            SubscriptionPlanFeatures = new List<SubscriptionPlanFeature>
            {
                new SubscriptionPlanFeature { FeatureId = Guid.NewGuid() }, // ASK_EMMA
                new SubscriptionPlanFeature { FeatureId = Guid.NewGuid() }, // SMS_CHATBOT
                new SubscriptionPlanFeature { FeatureId = Guid.NewGuid() }  // AGENT_ORCHESTRATION
            }
        }
    };
}
