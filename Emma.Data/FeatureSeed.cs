using Emma.Data.Models;

namespace Emma.Data;

public static class FeatureSeed
{
    public static List<Feature> Features => new()
    {
        new Feature { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Code = "ASK_EMMA", DisplayName = "Ask Emma", Description = "AI-powered question answering" },
        new Feature { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Code = "SMS_CHATBOT", DisplayName = "SMS Chatbot", Description = "Automated SMS conversations" },
        new Feature { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Code = "AGENT_ORCHESTRATION", DisplayName = "Agent Orchestration", Description = "Advanced agent workflow management" }
    };

    public static List<SubscriptionPlan> Plans => new()
    {
        new SubscriptionPlan
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            Name = "Standard",
            SubscriptionPlanFeatures = new List<SubscriptionPlanFeature>
            {
                new SubscriptionPlanFeature { FeatureId = Guid.Parse("00000000-0000-0000-0000-000000000001") }, // ASK_EMMA
                new SubscriptionPlanFeature { FeatureId = Guid.Parse("00000000-0000-0000-0000-000000000002") }  // SMS_CHATBOT
            }
        },
        new SubscriptionPlan
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            Name = "Pro",
            SubscriptionPlanFeatures = new List<SubscriptionPlanFeature>
            {
                new SubscriptionPlanFeature { FeatureId = Guid.Parse("00000000-0000-0000-0000-000000000001") }, // ASK_EMMA
                new SubscriptionPlanFeature { FeatureId = Guid.Parse("00000000-0000-0000-0000-000000000002") }, // SMS_CHATBOT
                new SubscriptionPlanFeature { FeatureId = Guid.Parse("00000000-0000-0000-0000-000000000003") }  // AGENT_ORCHESTRATION
            }
        }
    };
}
