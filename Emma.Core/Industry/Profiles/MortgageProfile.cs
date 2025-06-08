using System.Collections.Generic;

namespace Emma.Core.Industry.Profiles
{
    /// <summary>
    /// Mortgage Lending industry profile for EMMA
    /// </summary>
    public class MortgageProfile : IIndustryProfile
    {
        public string IndustryCode => "Mortgage";
        public string DisplayName => "Mortgage Lending";

        public IndustryPromptTemplates PromptTemplates => new()
        {
            SystemPrompt = @"You are EMMA, an AI assistant specialized in helping mortgage lenders and loan officers manage their client relationships and loan processes.

You understand:
- Mortgage application and approval processes
- Credit analysis and underwriting requirements
- Loan product types and qualification criteria
- Regulatory compliance (TRID, RESPA, etc.)
- Rate shopping and market conditions
- Client communication throughout the loan process
- Referral partnerships with real estate agents

You can help with:
- Qualifying leads and assessing loan readiness
- Managing application pipelines and timelines
- Tracking documentation requirements
- Coordinating with processors, underwriters, and closers
- Providing rate quotes and product recommendations
- Managing client expectations and communications
- Identifying cross-sell and referral opportunities

Always provide actionable advice that considers regulatory requirements and best practices for mortgage professionals.",

            ContextPrompt = @"Based on the following borrower information and loan details:
{ContactContext}

Application status and history:
{LoanHistory}

Current market rates and conditions:
{MarketContext}

Please provide insights and recommendations.",

            QueryTemplates = new Dictionary<string, string>
            {
                ["ready_to_apply"] = "Show me leads who are pre-qualified and ready to submit a formal mortgage application.",
                ["documentation_pending"] = "Identify borrowers with outstanding documentation requirements that could delay closing.",
                ["rate_sensitive"] = "Find borrowers who should be contacted about current rate changes or lock opportunities.",
                ["closing_soon"] = "Show loans scheduled to close within the next 15 days that need attention.",
                ["referral_partners"] = "Identify real estate agents and other partners who have sent quality referrals."
            },

            ActionPrompts = new Dictionary<string, string>
            {
                ["send_rate_quote"] = "Generate a personalized rate quote for {ContactName} based on their loan scenario and current market rates.",
                ["request_documents"] = "Create a documentation request for {ContactName} listing all required items for their {LoanType} application.",
                ["schedule_closing"] = "Coordinate closing for {ContactName}'s loan at {PropertyAddress} with all parties involved.",
                ["rate_lock_reminder"] = "Send rate lock expiration reminder to {ContactName} with options to extend or proceed."
            },

            Terminology = new Dictionary<string, string>
            {
                ["borrower"] = "A person applying for a mortgage loan",
                ["pre_qualification"] = "Initial assessment of borrowing capacity",
                ["pre_approval"] = "Conditional commitment for a specific loan amount",
                ["underwriting"] = "Detailed review and approval of loan application",
                ["closing"] = "Final loan funding and property transfer",
                ["ltv"] = "Loan-to-Value ratio",
                ["dti"] = "Debt-to-Income ratio",
                ["apr"] = "Annual Percentage Rate"
            }
        };

        public List<string> AvailableActions => new()
        {
            "send_rate_quote",
            "request_documents",
            "schedule_appraisal",
            "order_title_work",
            "coordinate_closing",
            "send_pre_approval_letter",
            "rate_lock_notification",
            "underwriting_update",
            "closing_disclosure_review",
            "post_closing_follow_up"
        };

        public ContactWorkflowDefinitions WorkflowDefinitions => new()
        {
            ContactStates = new List<string>
            {
                "Lead",
                "Pre-Qualified",
                "Pre-Approved", 
                "Application Submitted",
                "In Underwriting",
                "Approved",
                "Closed",
                "Past Client"
            },

            NBAByState = new Dictionary<string, List<string>>
            {
                ["Lead"] = new() { "qualify_borrower", "send_rate_quote", "schedule_consultation" },
                ["Pre-Qualified"] = new() { "send_pre_approval_application", "provide_loan_options", "connect_with_realtor" },
                ["Pre-Approved"] = new() { "monitor_rate_changes", "assist_home_search", "prepare_for_application" },
                ["Application Submitted"] = new() { "request_documents", "order_appraisal", "submit_to_underwriting" },
                ["In Underwriting"] = new() { "respond_to_conditions", "coordinate_with_underwriter", "update_borrower" },
                ["Approved"] = new() { "schedule_closing", "prepare_closing_disclosure", "coordinate_final_details" },
                ["Closed"] = new() { "send_satisfaction_survey", "request_referrals", "offer_future_services" }
            },

            StateTransitions = new List<ContactStateTransition>
            {
                new() { FromState = "Lead", ToState = "Pre-Qualified", RequiredActions = new() { "qualify_borrower" } },
                new() { FromState = "Pre-Qualified", ToState = "Pre-Approved", RequiredActions = new() { "complete_pre_approval" } },
                new() { FromState = "Pre-Approved", ToState = "Application Submitted", RequiredActions = new() { "submit_full_application" } },
                new() { FromState = "Application Submitted", ToState = "In Underwriting", RequiredActions = new() { "complete_file_submission" } },
                new() { FromState = "In Underwriting", ToState = "Approved", RequiredActions = new() { "satisfy_all_conditions" } },
                new() { FromState = "Approved", ToState = "Closed", RequiredActions = new() { "fund_loan" }, IsAutomatic = true }
            },

            AutomationTriggers = new List<WorkflowTrigger>
            {
                new() 
                { 
                    Name = "Rate Lock Expiration", 
                    Condition = "RateLockExpiration <= 7 days",
                    Actions = new() { "send_rate_lock_reminder", "offer_extension_options" },
                    Priority = 1
                },
                new() 
                { 
                    Name = "Document Follow-up", 
                    Condition = "DocumentsRequested && DaysSinceRequest > 3",
                    Actions = new() { "send_document_reminder", "offer_assistance" },
                    Priority = 2
                },
                new() 
                { 
                    Name = "Closing Coordination", 
                    Condition = "DaysToClosing <= 5",
                    Actions = new() { "confirm_closing_details", "send_final_instructions" },
                    Priority = 1
                }
            }
        };

        public IndustryConfiguration Configuration => new()
        {
            CustomFields = new Dictionary<string, string>
            {
                ["CreditScore"] = "int",
                ["AnnualIncome"] = "decimal",
                ["EmploymentType"] = "string",
                ["DownPaymentAmount"] = "decimal",
                ["LoanPurpose"] = "string",
                ["PropertyType"] = "string",
                ["OccupancyType"] = "string"
            },

            ContactProperties = new Dictionary<string, object>
            {
                ["LoanPurposes"] = new[] { "Purchase", "Refinance", "Cash-Out Refinance", "Construction", "Investment" },
                ["PropertyTypes"] = new[] { "Single Family", "Condo", "Townhouse", "Multi-Family", "Manufactured" },
                ["OccupancyTypes"] = new[] { "Primary Residence", "Second Home", "Investment Property" },
                ["LoanPrograms"] = new[] { "Conventional", "FHA", "VA", "USDA", "Jumbo", "Non-QM" }
            },

            CommunicationTemplates = new Dictionary<string, string>
            {
                ["welcome_email"] = "Welcome! I'm excited to help you with your mortgage needs. Let's get started on your homeownership journey.",
                ["rate_quote"] = "Based on your scenario, here are your current rate options: {RateOptions}",
                ["document_request"] = "To move forward with your application, please provide: {DocumentList}",
                ["closing_confirmation"] = "Your closing is confirmed for {DateTime} at {Location}. Congratulations!"
            }
        };

        public List<IndustrySampleQuery> SampleQueries => new()
        {
            new() { Query = "Who's ready to lock their rate today?", Description = "Find borrowers with expiring rate locks or favorable market conditions", Category = "Rate Management" },
            new() { Query = "What loans are at risk of missing closing?", Description = "Identify loans with potential delays or issues", Category = "Pipeline Management" },
            new() { Query = "Which borrowers need document follow-up?", Description = "Find applications with outstanding documentation", Category = "Document Management" },
            new() { Query = "Show me my top referral partners this month", Description = "Identify most productive referral relationships", Category = "Partner Management" },
            new() { Query = "What's my pipeline for next month's closings?", Description = "View upcoming loan closings and revenue", Category = "Pipeline Analysis" },
            new() { Query = "Who should I contact about refinancing?", Description = "Find past clients who could benefit from current rates", Category = "Business Development" }
        };
    }
}
