using Emma.Core.Interfaces;
using Emma.Core.Models;
using Emma.Models.Interfaces;
using Emma.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Emma.Core.Services
{
    public class SqlContextExtractor : ISqlContextExtractor
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<SqlContextExtractor> _logger;
        private readonly ITenantContextService _tenantService;

        public SqlContextExtractor(
            IAppDbContext context,
            ILogger<SqlContextExtractor> logger,
            ITenantContextService tenantService)
        {
            _context = context;
            _logger = logger;
            _tenantService = tenantService;
        }

        public async Task<SqlContextData> ExtractContextAsync(
            Guid contactId,
            Guid requestingAgentId,
            UserRole requestingRole = UserRole.Agent,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Extracting SQL context for Contact {ContactId} by Agent {AgentId} with role {Role}",
                    contactId, requestingAgentId, requestingRole);

                // Get tenant context for security filtering
                var tenantContext = await _tenantService.GetCurrentTenantAsync();
                if (tenantContext == null)
                {
                    throw new UnauthorizedAccessException("No valid tenant context found");
                }

                // Validate access to the contact
                var hasAccess = await ValidateContactAccessAsync(contactId, requestingAgentId, requestingRole);
                if (!hasAccess)
                {
                    _logger.LogWarning("Access denied to Contact {ContactId} for Agent {AgentId}", contactId, requestingAgentId);
                    throw new UnauthorizedAccessException($"Access denied to contact {contactId}");
                }

                var contextData = new SqlContextData
                {
                    ContextType = requestingRole.ToString().ToLower(),
                    SchemaVersion = "1.0",
                    GeneratedAt = DateTime.UtcNow,
                    Security = new SecurityMetadata
                    {
                        RequestingAgentId = requestingAgentId,
                        RequestingRole = requestingRole.ToString(),
                        TenantId = tenantContext.TenantId,
                        AppliedFilters = new List<string>(),
                        DataClassification = "Business"
                    }
                };

                // Extract role-specific context
                switch (requestingRole)
                {
                    case UserRole.Agent:
                        contextData.Agent = await ExtractAgentContextAsync(requestingAgentId, contactId);
                        contextData.Security.DataClassification = DetermineAgentDataClassification(contactId, requestingAgentId);
                        break;

                    case UserRole.Admin:
                        contextData.Admin = await ExtractAdminContextAsync(requestingAgentId);
                        contextData.Security.DataClassification = "Business";
                        break;

                    case UserRole.AIWorkflow:
                        contextData.AIWorkflow = await ExtractAIWorkflowContextAsync(contactId, requestingAgentId);
                        contextData.Security.DataClassification = "Business";
                        break;

                    default:
                        throw new ArgumentException($"Unsupported role: {requestingRole}");
                }

                _logger.LogInformation("Successfully extracted SQL context for Contact {ContactId}", contactId);
                return contextData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract SQL context for Contact {ContactId}", contactId);
                throw;
            }
        }

        private async Task<Emma.Core.Models.AgentContext> ExtractAgentContextAsync(Guid agentId, Guid? focusContactId = null)
        {
            var context = new Emma.Core.Models.AgentContext();

            // Get agent's assigned contacts (limit to recent/active for performance)
            var contactsQuery = _context.Contacts.AsQueryable();
            
            // For demo: get all contacts, in production filter by assignment/ownership
            var contacts = await contactsQuery
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Where(c => c.RelationshipState == RelationshipState.Lead || 
                           c.RelationshipState == RelationshipState.Prospect || 
                           c.RelationshipState == RelationshipState.Client)
                .OrderByDescending(c => c.UpdatedAt > c.CreatedAt ? c.UpdatedAt : c.CreatedAt)
                .Take(20) // Limit for performance
                .ToListAsync();

            context.Contacts = contacts.Select(c => new AssignedContact
            {
                ContactId = c.Id,
                Name = $"{c.FirstName} {c.LastName}".Trim(),
                Email = c.Emails.FirstOrDefault()?.Address,
                Phone = c.Phones.FirstOrDefault()?.Number,
                RelationshipState = c.RelationshipState.ToString(),
                CurrentStage = c.RelationshipState.ToString(), // Could be enhanced with actual pipeline stages
                LastInteraction = c.UpdatedAt > c.CreatedAt ? c.UpdatedAt : c.CreatedAt,
                Tags = c.Tags,
                IsActiveClient = c.IsActiveClient,
                Preferences = new ContactPreferences
                {
                    PreferredContactMethod = "Email", // Default, could be enhanced
                    BestTimeToContact = "Business Hours",
                    EmailOptIn = true,
                    SmsOptIn = true,
                    DoNotContact = false
                }
            }).ToList();

            // Get recent interactions (simplified for demo)
            context.RecentInteractions = new List<RecentInteraction>();

            // Get tasks (placeholder for demo)
            context.Tasks = new List<TaskItem>();

            // Calculate agent performance
            context.Performance = new AgentPerformance
            {
                ContactsThisMonth = contacts.Count(),
                InteractionsThisWeek = 0, // Would be calculated from actual interaction data
                ConversionRate = 0.25m, // Placeholder
                TasksCompleted = 0,
                TasksOverdue = 0
            };

            // Build activity timeline (simplified)
            context.Timeline = contacts.Take(5).Select(c => new ActivityTimelineItem
            {
                Timestamp = c.UpdatedAt > c.CreatedAt ? c.UpdatedAt : c.CreatedAt,
                Type = "Contact",
                Description = $"Contact {c.FirstName} {c.LastName} updated",
                ContactId = c.Id,
                ContactName = $"{c.FirstName} {c.LastName}".Trim()
            }).ToList();

            return context;
        }

        private async Task<AdminContext> ExtractAdminContextAsync(Guid adminAgentId)
        {
            var context = new AdminContext();

            // Get organization KPIs
            var totalContacts = await _context.Contacts.CountAsync();
            var activeClients = await _context.Contacts.CountAsync(c => c.IsActiveClient);
            var leadsThisMonth = await _context.Contacts
                .CountAsync(c => c.RelationshipState == RelationshipState.Lead && 
                               c.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            context.KPIs = new OrganizationKPIs
            {
                TotalContacts = totalContacts,
                ActiveClients = activeClients,
                LeadsThisMonth = leadsThisMonth,
                OverallConversionRate = totalContacts > 0 ? (decimal)activeClients / totalContacts : 0,
                TotalInteractionsThisWeek = 0, // Would be calculated from interaction data
                ContactsByStage = await GetContactsByStageAsync(),
                InteractionsByType = new Dictionary<string, int>() // Would be populated from interaction data
            };

            // Get agent summaries
            var agents = await _context.Agents
                .Include(a => a.Organization)
                .ToListAsync();

            context.Agents = agents.Select(a => new AgentSummary
            {
                AgentId = a.Id,
                Name = $"{a.FirstName} {a.LastName}".Trim(),
                Email = a.Email,
                IsActive = a.IsActive,
                LastLogin = null, // Would be tracked in login audit
                AssignedContacts = 0, // Would be calculated from assignments
                ConversionRate = 0, // Would be calculated from performance data
                InteractionsThisWeek = 0 // Would be calculated from interaction data
            }).ToList();

            // Get recent audit logs (placeholder)
            context.RecentAuditLogs = new List<AuditLogEntry>();

            // System health check
            context.Health = new SystemHealth
            {
                DatabaseConnected = true,
                EmailServiceConnected = true,
                SmsServiceConnected = true,
                QueueBacklog = 0,
                LastHealthCheck = DateTime.UtcNow
            };

            // Subscription info (placeholder)
            context.Subscription = new SubscriptionInfo
            {
                PlanName = "Professional",
                ExpiresAt = DateTime.UtcNow.AddMonths(12),
                AgentLimit = 10,
                ContactLimit = 10000,
                CurrentAgentCount = agents.Count,
                CurrentContactCount = totalContacts,
                Status = "Active"
            };

            return context;
        }

        private async Task<AIWorkflowContext> ExtractAIWorkflowContextAsync(Guid contactId, Guid requestingAgentId)
        {
            var contact = await _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .FirstOrDefaultAsync(c => c.Id == contactId);

            if (contact == null)
            {
                throw new ArgumentException($"Contact {contactId} not found");
            }

            var agent = await _context.Agents.FindAsync(requestingAgentId);
            if (agent == null)
            {
                throw new ArgumentException($"Agent {requestingAgentId} not found");
            }

            var context = new AIWorkflowContext
            {
                Contact = new WorkflowContact
                {
                    ContactId = contact.Id,
                    Name = $"{contact.FirstName} {contact.LastName}".Trim(),
                    Email = contact.Emails.FirstOrDefault()?.Address,
                    Phone = contact.Phones.FirstOrDefault()?.Number,
                    RelationshipState = contact.RelationshipState.ToString(),
                    EmailOptIn = true, // Default, would be from actual consent data
                    SmsOptIn = true,
                    DoNotContact = false,
                    PreferredContactMethod = "Email",
                    Tags = contact.Tags
                },
                Deal = null, // Would be populated if deal data exists
                AssignedAgent = new WorkflowAgent
                {
                    AgentId = agent.Id,
                    Name = $"{agent.FirstName} {agent.LastName}".Trim(),
                    Email = agent.Email,
                    Phone = null // Would be from agent phone data
                },
                Triggers = new List<WorkflowTrigger>(), // Would be populated based on workflow rules
                WorkflowData = new Dictionary<string, object>()
            };

            return context;
        }

        private async Task<Dictionary<string, int>> GetContactsByStageAsync()
        {
            var stageGroups = await _context.Contacts
                .GroupBy(c => c.RelationshipState)
                .Select(g => new { Stage = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            return stageGroups.ToDictionary(g => g.Stage, g => g.Count);
        }

        /// <summary>
        /// Determines the data classification level for agent access to contact data.
        /// </summary>
        private string DetermineAgentDataClassification(Guid contactId, Guid requestingAgentId)
        {
            // For demo purposes, return a safe default classification
            // In production, this would analyze:
            // - Contact sensitivity level
            // - Agent clearance level
            // - Regulatory requirements
            // - Data residency rules
            
            return "Business"; // Safe default for demo
        }

        private async Task<bool> ValidateContactAccessAsync(Guid contactId, Guid requestingAgentId, UserRole role)
        {
            // For demo purposes, allow access to all contacts
            // In production, implement proper access control based on:
            // - Contact ownership/assignment
            // - Organization membership
            // - Collaboration permissions
            // - Role-based access rules
            
            var contact = await _context.Contacts.FindAsync(contactId);
            return contact != null;
        }

        public async Task<string> SerializeContextAsync(SqlContextData contextData, CancellationToken cancellationToken = default)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                return JsonSerializer.Serialize(contextData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to serialize SQL context data");
                throw;
            }
        }
    }
}
