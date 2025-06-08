using System.Collections.Generic;

namespace Emma.Core.Industry.Profiles
{
    /// <summary>
    /// Real Estate industry profile for EMMA
    /// </summary>
    public class RealEstateProfile : IIndustryProfile
    {
        public string IndustryCode => "RealEstate";
        public string DisplayName => "Real Estate";

        public IndustryPromptTemplates PromptTemplates => new()
        {
            SystemPrompt = @"You are EMMA, an AI assistant specialized in helping real estate agents manage their client relationships and business workflows. 

You understand:
- Real estate sales processes and timelines
- Lead qualification and nurturing
- Property showing coordination
- Transaction management
- Market analysis and pricing strategies
- Client communication preferences
- Referral and relationship building

You can help with:
- Identifying next best actions for leads and clients
- Scheduling property showings and follow-ups
- Analyzing client engagement and interest levels
- Managing transaction timelines and deadlines
- Coordinating with other service providers (lenders, inspectors, etc.)
- Generating market insights and property recommendations

Always provide actionable, specific advice tailored to real estate professionals.",

            ContextPrompt = @"Based on the following contact information and interaction history:
{ContactContext}

Recent communications:
{CommunicationHistory}

Current market conditions:
{MarketContext}

Please provide insights and recommendations.",

            QueryTemplates = new Dictionary<string, string>
            {
                ["hot_leads"] = "Show me contacts who are actively looking to buy or sell property, have engaged recently, and show high interest indicators.",
                ["follow_ups"] = "Identify contacts who need follow-up based on their last interaction, property viewing history, or transaction timeline.",
                ["new_listings"] = "Find contacts who might be interested in new listings based on their search criteria and preferences.",
                ["closing_soon"] = "Show contacts with transactions closing within the next 30 days that need attention.",
                ["referral_opportunities"] = "Identify satisfied clients who could provide referrals or repeat business opportunities."
            },

            ActionPrompts = new Dictionary<string, string>
            {
                ["schedule_showing"] = "Schedule a property showing for {ContactName} at {PropertyAddress} considering their availability and preferences.",
                ["send_market_update"] = "Draft a personalized market update for {ContactName} including relevant properties and market trends.",
                ["follow_up_viewing"] = "Create a follow-up message for {ContactName} after their property viewing at {PropertyAddress}.",
                ["transaction_update"] = "Generate a transaction status update for {ContactName} regarding their {TransactionType} at {PropertyAddress}."
            },

            Terminology = new Dictionary<string, string>
            {
                ["client"] = "A person actively buying or selling property",
                ["lead"] = "A potential client who has shown interest in real estate services",
                ["prospect"] = "A qualified lead who is likely to become a client",
                ["listing"] = "A property available for sale",
                ["showing"] = "A scheduled viewing of a property",
                ["closing"] = "The final step in a real estate transaction",
                ["cma"] = "Comparative Market Analysis",
                ["mls"] = "Multiple Listing Service"
            }
        };

        public List<string> AvailableActions => new()
        {
            "schedule_showing",
            "send_market_update",
            "create_cma",
            "follow_up_viewing",
            "update_listing_status",
            "coordinate_inspection",
            "send_contract_reminder",
            "generate_referral_request",
            "schedule_closing",
            "create_buyer_consultation"
        };

        public ContactWorkflowDefinitions WorkflowDefinitions => new()
        {
            ContactStates = new List<string>
            {
                "Lead",
                "Prospect", 
                "Buyer",
                "Seller",
                "Client",
                "PastClient",
                "Referral Source"
            },

            NBAByState = new Dictionary<string, List<string>>
            {
                ["Lead"] = new() { "qualify_lead", "schedule_consultation", "send_market_info" },
                ["Prospect"] = new() { "schedule_showing", "send_listings", "follow_up_interest" },
                ["Buyer"] = new() { "show_properties", "submit_offer", "coordinate_inspection" },
                ["Seller"] = new() { "create_listing", "schedule_photos", "review_offers" },
                ["Client"] = new() { "manage_transaction", "coordinate_closing", "provide_updates" },
                ["PastClient"] = new() { "request_referral", "send_market_updates", "check_satisfaction" }
            },

            StateTransitions = new List<ContactStateTransition>
            {
                new() { FromState = "Lead", ToState = "Prospect", RequiredActions = new() { "qualify_lead" } },
                new() { FromState = "Prospect", ToState = "Buyer", RequiredActions = new() { "schedule_consultation" } },
                new() { FromState = "Prospect", ToState = "Seller", RequiredActions = new() { "schedule_listing_consultation" } },
                new() { FromState = "Buyer", ToState = "Client", RequiredActions = new() { "accepted_offer" } },
                new() { FromState = "Seller", ToState = "Client", RequiredActions = new() { "signed_listing_agreement" } },
                new() { FromState = "Client", ToState = "PastClient", RequiredActions = new() { "closed_transaction" }, IsAutomatic = true }
            },

            AutomationTriggers = new List<WorkflowTrigger>
            {
                new() 
                { 
                    Name = "New Lead Follow-up", 
                    Condition = "ContactState == 'Lead' && DaysSinceLastContact > 2",
                    Actions = new() { "send_welcome_email", "schedule_consultation" },
                    Priority = 1
                },
                new() 
                { 
                    Name = "Showing Follow-up", 
                    Condition = "LastActivity == 'property_showing' && HoursSinceActivity > 24",
                    Actions = new() { "send_follow_up_message", "request_feedback" },
                    Priority = 2
                },
                new() 
                { 
                    Name = "Transaction Milestone", 
                    Condition = "TransactionStage == 'under_contract' && DaysToClosing <= 7",
                    Actions = new() { "send_closing_reminder", "coordinate_final_walkthrough" },
                    Priority = 1
                }
            }
        };

        public IndustryConfiguration Configuration => new()
        {
            CustomFields = new Dictionary<string, string>
            {
                ["PreferredPriceRange"] = "string",
                ["PreferredAreas"] = "string[]",
                ["PropertyType"] = "string",
                ["TimeframeToMove"] = "string",
                ["FinancingPreApproved"] = "boolean",
                ["CurrentHomeStatus"] = "string"
            },

            ContactProperties = new Dictionary<string, object>
            {
                ["DefaultTimeframeOptions"] = new[] { "Immediately", "1-3 months", "3-6 months", "6+ months", "Just browsing" },
                ["PropertyTypes"] = new[] { "Single Family", "Condo", "Townhouse", "Multi-Family", "Land", "Commercial" },
                ["TransactionTypes"] = new[] { "Buy", "Sell", "Rent", "Lease" }
            },

            CommunicationTemplates = new Dictionary<string, string>
            {
                ["welcome_email"] = "Welcome to our real estate services! I'm excited to help you with your property needs.",
                ["showing_confirmation"] = "Your property showing is confirmed for {DateTime} at {Address}. See you there!",
                ["market_update"] = "Here's your personalized market update with properties matching your criteria.",
                ["transaction_update"] = "Update on your {TransactionType}: {Status}. Next steps: {NextSteps}"
            }
        };

        public List<IndustrySampleQuery> SampleQueries => new()
        {
            new() { Query = "Who are my hottest leads right now?", Description = "Find leads with high engagement and buying signals", Category = "Lead Management" },
            new() { Query = "What's the next best action for Sarah Johnson?", Description = "Get personalized recommendations for a specific contact", Category = "Contact Management" },
            new() { Query = "Show me all clients closing this month", Description = "View upcoming closings and required actions", Category = "Transaction Management" },
            new() { Query = "Who needs a follow-up after their showing?", Description = "Identify contacts requiring post-showing follow-up", Category = "Follow-up Management" },
            new() { Query = "Which past clients could provide referrals?", Description = "Find satisfied clients for referral opportunities", Category = "Business Development" },
            new() { Query = "What properties match John's criteria?", Description = "Find listings matching a client's search preferences", Category = "Property Matching" }
        };
    }
}
