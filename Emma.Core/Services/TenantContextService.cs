using Emma.Data;
using Emma.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Emma.Core.Services;

/// <summary>
/// Implementation of tenant-aware context service for industry-specific AI operations
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // In-memory industry profiles - could be moved to database or configuration
    private static readonly Dictionary<string, IndustryProfile> _industryProfiles = new()
    {
        ["RealEstate"] = new IndustryProfile
        {
            IndustryCode = "RealEstate",
            DisplayName = "Real Estate",
            SystemPrompt = "You are EMMA, an AI assistant specialized in real estate. You help agents manage client relationships, coordinate transactions, and orchestrate service providers like lenders, inspectors, contractors, and attorneys. Focus on property transactions, client needs, and relationship management.",
            SpecializedAgents = new() { "ContractorAgent", "MortgageAgent", "InspectorAgent", "AttorneyAgent" },
            IndustryTerminology = new()
            {
                ["client"] = "home buyer or seller",
                ["lead"] = "potential home buyer or seller",
                ["transaction"] = "real estate purchase or sale",
                ["closing"] = "final transaction completion"
            },
            ResourceTypes = new() { "Lender", "Inspector", "Contractor", "Attorney", "Appraiser", "Title Company" },
            ComplianceRequirements = new() { "RESPA", "Fair Housing", "State Licensing" },
            NbaActionTypes = new() { "schedule_showing", "request_inspection", "coordinate_closing", "assign_lender" }
        },
        ["Mortgage"] = new IndustryProfile
        {
            IndustryCode = "Mortgage",
            DisplayName = "Mortgage Lending",
            SystemPrompt = "You are EMMA, an AI assistant specialized in mortgage lending. You help loan officers manage borrower relationships, coordinate loan processing, and orchestrate service providers like appraisers, processors, and underwriters. Focus on loan applications, compliance, and borrower communication.",
            SpecializedAgents = new() { "UnderwritingAgent", "ProcessorAgent", "AppraiserAgent", "ComplianceAgent" },
            IndustryTerminology = new()
            {
                ["client"] = "borrower or loan applicant",
                ["lead"] = "potential borrower",
                ["transaction"] = "loan application or refinance",
                ["closing"] = "loan funding"
            },
            ResourceTypes = new() { "Appraiser", "Processor", "Underwriter", "Title Company", "Insurance Agent" },
            ComplianceRequirements = new() { "TRID", "QM Rule", "Fair Lending", "HMDA" },
            NbaActionTypes = new() { "order_appraisal", "request_documents", "schedule_closing", "assign_processor" }
        },
        ["Insurance"] = new IndustryProfile
        {
            IndustryCode = "Insurance",
            DisplayName = "Insurance",
            SystemPrompt = "You are EMMA, an AI assistant specialized in insurance. You help agents manage policyholder relationships, coordinate claims, and orchestrate service providers like adjusters, repair vendors, and medical providers. Focus on policy management, claims processing, and client service.",
            SpecializedAgents = new() { "ClaimsAgent", "UnderwritingAgent", "AdjusterAgent", "VendorAgent" },
            IndustryTerminology = new()
            {
                ["client"] = "policyholder or insured",
                ["lead"] = "potential policyholder",
                ["transaction"] = "policy application or claim",
                ["closing"] = "claim settlement"
            },
            ResourceTypes = new() { "Adjuster", "Repair Vendor", "Medical Provider", "Attorney", "Investigator" },
            ComplianceRequirements = new() { "State Insurance Code", "Privacy Regulations", "Claims Handling" },
            NbaActionTypes = new() { "assign_adjuster", "schedule_inspection", "coordinate_repairs", "process_claim" }
        }
    };
    
    public TenantContextService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<Organization> GetCurrentTenantAsync()
    {
        // In a real implementation, this would extract tenant ID from JWT token, headers, or context
        // For now, get the first organization as a placeholder
        var org = await _context.Organizations
            .Include(o => o.Agents)
            .Include(o => o.Subscriptions)
            .FirstOrDefaultAsync();
            
        if (org == null)
        {
            throw new InvalidOperationException("No organization found for current tenant");
        }
        
        return org;
    }
    
    public async Task<IndustryProfile> GetIndustryProfileAsync()
    {
        var tenant = await GetCurrentTenantAsync();
        var industryCode = tenant.IndustryCode ?? "RealEstate"; // Default to RealEstate
        
        if (_industryProfiles.TryGetValue(industryCode, out var profile))
        {
            return profile;
        }
        
        // Return default profile if industry not found
        return _industryProfiles["RealEstate"];
    }
    
    public async Task<List<string>> GetAvailableAgentsAsync()
    {
        var profile = await GetIndustryProfileAsync();
        return profile.SpecializedAgents;
    }
    
    public async Task<List<string>> GetIndustryResourceTypesAsync()
    {
        var profile = await GetIndustryProfileAsync();
        return profile.ResourceTypes;
    }
    
    public async Task<string> GetSystemPromptAsync()
    {
        var profile = await GetIndustryProfileAsync();
        return profile.SystemPrompt;
    }
    
    public async Task<bool> IsAgentAvailableAsync(string agentType)
    {
        var availableAgents = await GetAvailableAgentsAsync();
        return availableAgents.Contains(agentType);
    }
}
