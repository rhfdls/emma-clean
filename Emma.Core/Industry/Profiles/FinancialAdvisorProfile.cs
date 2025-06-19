using System.Collections.Generic;

namespace Emma.Core.Industry.Profiles
{
    /// <summary>
    /// Financial Advisory industry profile for EMMA
    /// </summary>
    public class FinancialAdvisorProfile : IIndustryProfile
    {
        public string IndustryCode => "Financial";
        public string DisplayName => "Financial Advisory";

        public IndustryPromptTemplates PromptTemplates => new()
        {
            SystemPrompt = @"You are EMMA, an AI assistant specialized in helping financial advisors manage their client relationships and advisory practices.

You understand:
- Financial planning processes and methodologies
- Investment management and portfolio strategies
- Retirement planning and wealth accumulation
- Risk assessment and insurance needs analysis
- Tax planning and estate planning coordination
- Regulatory compliance (SEC, FINRA, state regulations)
- Client onboarding and relationship management

You can help with:
- Identifying client review and planning opportunities
- Managing client communication and follow-ups
- Tracking financial goals and progress
- Coordinating with CPAs, attorneys, and other professionals
- Monitoring portfolio performance and rebalancing needs
- Identifying cross-selling and referral opportunities
- Managing compliance and documentation requirements

Always provide advice that considers fiduciary responsibilities and regulatory requirements for financial professionals.",

            ContextPrompt = @"Based on the following client information and financial profile:
{ContactContext}

Portfolio and planning history:
{FinancialHistory}

Current market conditions and opportunities:
{MarketContext}

Please provide insights and recommendations.",

            QueryTemplates = new Dictionary<string, string>
            {
                ["review_due"] = "Show me clients who are due for their annual or quarterly financial reviews.",
                ["rebalancing_needed"] = "Identify portfolios that need rebalancing based on target allocations and market movements.",
                ["goal_tracking"] = "Find clients approaching major financial goals or milestones that need attention.",
                ["tax_planning"] = "Show clients who could benefit from year-end tax planning strategies.",
                ["referral_opportunities"] = "Identify satisfied clients who could provide quality referrals."
            },

            ActionPrompts = new Dictionary<string, string>
            {
                ["schedule_review"] = "Schedule a comprehensive financial review for {ContactName} to discuss their {PlanningArea} goals.",
                ["rebalance_portfolio"] = "Create rebalancing recommendations for {ContactName}'s portfolio based on current allocations.",
                ["tax_strategy"] = "Develop tax planning strategies for {ContactName} considering their {TaxSituation}.",
                ["goal_update"] = "Update financial goals and progress tracking for {ContactName}'s {FinancialGoal}."
            },

            Terminology = new Dictionary<string, string>
            {
                ["client"] = "A person receiving ongoing financial advisory services",
                ["prospect"] = "A potential client considering financial advisory services",
                ["aum"] = "Assets Under Management",
                ["financial_plan"] = "Comprehensive strategy for achieving financial goals",
                ["portfolio"] = "Collection of investments managed for a client",
                ["risk_tolerance"] = "Client's comfort level with investment volatility",
                ["asset_allocation"] = "Distribution of investments across asset classes",
                ["rebalancing"] = "Adjusting portfolio to maintain target allocations"
            }
        };

        public List<string> AvailableActions { get; private set; }

        public void InitializeAvailableActions()
        {
            AvailableActions = AvailableActions ?? new List<string>
            {
                "schedule_review",
                "rebalance_portfolio",
                "update_financial_plan",
                "tax_loss_harvesting",
                "insurance_review",
                "estate_planning_update",
                "retirement_projection",
                "goal_progress_report",
                "market_commentary",
                "referral_request"
            };
        }

        public List<string> SpecializedAgents { get; private set; }

        public void InitializeSpecializedAgents()
        {
            SpecializedAgents = SpecializedAgents ?? new List<string>
            {
                "FinancialPlanningAgent",
                "PortfolioManagementAgent", 
                "TaxPlanningAgent",
                "RetirementPlanningAgent",
                "NbaAgent"
            };
        }

        public ContactWorkflowDefinitions WorkflowDefinitions => new()
        {
            ContactStates = new List<string>
            {
                "Prospect",
                "New Client",
                "Active Client", 
                "Planning Client",
                "Investment Client",
                "Comprehensive Client",
                "Inactive Client",
                "Former Client"
            },

            NBAByState = new Dictionary<string, List<string>>
            {
                ["Prospect"] = new() { "schedule_discovery_meeting", "send_planning_materials", "assess_fit" },
                ["New Client"] = new() { "complete_onboarding", "create_financial_plan", "set_initial_goals" },
                ["Active Client"] = new() { "monitor_progress", "schedule_regular_reviews", "provide_updates" },
                ["Planning Client"] = new() { "update_financial_plan", "track_goal_progress", "coordinate_professionals" },
                ["Investment Client"] = new() { "monitor_portfolio", "rebalance_as_needed", "review_performance" },
                ["Comprehensive Client"] = new() { "holistic_planning_review", "coordinate_all_services", "proactive_recommendations" },
                ["Inactive Client"] = new() { "re_engagement_outreach", "assess_current_needs", "offer_limited_services" }
            },

            StateTransitions = new List<ContactStateTransition>
            {
                new() { FromState = "Prospect", ToState = "New Client", RequiredActions = new() { "sign_advisory_agreement" } },
                new() { FromState = "New Client", ToState = "Planning Client", RequiredActions = new() { "complete_financial_plan" } },
                new() { FromState = "New Client", ToState = "Investment Client", RequiredActions = new() { "fund_investment_account" } },
                new() { FromState = "Planning Client", ToState = "Comprehensive Client", RequiredActions = new() { "add_investment_management" } },
                new() { FromState = "Investment Client", ToState = "Comprehensive Client", RequiredActions = new() { "add_financial_planning" } },
                new() { FromState = "Active Client", ToState = "Inactive Client", RequiredActions = new() { "no_contact_12_months" }, IsAutomatic = true }
            },

            AutomationTriggers = new List<WorkflowTrigger>
            {
                new() 
                { 
                    Name = "Quarterly Review Due", 
                    Condition = "LastReviewDate <= 90 days ago",
                    Actions = new() { "schedule_review_meeting", "prepare_performance_report" },
                    Priority = 1
                },
                new() 
                { 
                    Name = "Portfolio Drift Alert", 
                    Condition = "PortfolioDrift > 5%",
                    Actions = new() { "recommend_rebalancing", "schedule_portfolio_review" },
                    Priority = 2
                },
                new() 
                { 
                    Name = "Goal Milestone Approaching", 
                    Condition = "GoalTargetDate <= 6 months",
                    Actions = new() { "review_goal_progress", "adjust_strategy_if_needed" },
                    Priority = 1
                }
            }
        };

        public IndustryConfiguration Configuration => new()
        {
            CustomFields = new Dictionary<string, string>
            {
                ["NetWorth"] = "decimal",
                ["AnnualIncome"] = "decimal",
                ["RiskTolerance"] = "string",
                ["InvestmentExperience"] = "string",
                ["RetirementGoalDate"] = "datetime",
                ["FinancialGoals"] = "string[]",
                ["InsuranceCoverage"] = "string"
            },

            ContactProperties = new Dictionary<string, object>
            {
                ["RiskToleranceOptions"] = new[] { "Conservative", "Moderate Conservative", "Moderate", "Moderate Aggressive", "Aggressive" },
                ["GoalTypes"] = new[] { "Retirement", "Education", "Major Purchase", "Emergency Fund", "Estate Planning", "Tax Planning" },
                ["ServiceTypes"] = new[] { "Financial Planning", "Investment Management", "Retirement Planning", "Tax Planning", "Estate Planning" },
                ["ReviewFrequencies"] = new[] { "Monthly", "Quarterly", "Semi-Annual", "Annual" }
            },

            CommunicationTemplates = new Dictionary<string, string>
            {
                ["welcome_email"] = "Welcome to our financial advisory practice! I look forward to helping you achieve your financial goals.",
                ["review_reminder"] = "It's time for your {ReviewType} review. Let's schedule a meeting to discuss your progress.",
                ["market_update"] = "Here's your personalized market update and how it affects your portfolio.",
                ["goal_progress"] = "Great progress on your {GoalType} goal! You're {PercentComplete}% of the way there."
            }
        };

        public List<IndustrySampleQuery> SampleQueries => new()
        {
            new() { Query = "Who needs their quarterly review scheduled?", Description = "Find clients due for regular portfolio and planning reviews", Category = "Review Management" },
            new() { Query = "Which portfolios need rebalancing?", Description = "Identify accounts with significant allocation drift", Category = "Portfolio Management" },
            new() { Query = "Show me clients approaching retirement", Description = "Find clients within 5 years of retirement goals", Category = "Retirement Planning" },
            new() { Query = "Who could benefit from tax loss harvesting?", Description = "Identify tax planning opportunities in client portfolios", Category = "Tax Planning" },
            new() { Query = "Which clients haven't been contacted recently?", Description = "Find clients needing proactive outreach", Category = "Relationship Management" },
            new() { Query = "What's my AUM growth this quarter?", Description = "Analyze assets under management and business growth", Category = "Business Analytics" }
        };

        public List<string> NbaActionTypes => new()
        {
            "schedule_review",
            "rebalance_portfolio", 
            "update_financial_plan",
            "tax_loss_harvesting",
            "insurance_review",
            "estate_planning_update",
            "retirement_projection",
            "goal_progress_report",
            "market_commentary",
            "referral_request",
            "risk_assessment",
            "portfolio_analysis",
            "client_check_in",
            "compliance_review"
        };

        public List<string> ResourceTypes { get; } = new List<string>
        {
            "InvestmentPortfolio",
            "RetirementPlan",
            "InsurancePolicy"
        };

        public List<string> DefaultResourceCategories { get; } = new List<string>
        {
            "FinancialPlanning",
            "PortfolioManagement",
            "TaxStrategy"
        };
    }
}
