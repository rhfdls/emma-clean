using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Emma.Models.Interfaces;
using Emma.Models.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Emma.Core.Services
{
    /// <summary>
    /// Handles NBA (Next Best Action) recommendations during time simulation.
    /// Generates and processes NBA recommendations for contacts based on simulated time.
    /// </summary>
    public class NbaTimeEventHandler : ISimulationEventHandler, IDisposable
    {
        private readonly ILogger<NbaTimeEventHandler> _logger;
        private readonly IServiceProvider _serviceProvider;

        private readonly IAgentActionValidator _actionValidator;
        private readonly SemaphoreSlim _processingLock = new(1, 1);
        private DateTime _lastRunTime = DateTime.MinValue;
        private const double MinimumRunIntervalHours = 1.0; // Minimum time between NBA recommendation runs

        public NbaTimeEventHandler(
            ILogger<NbaTimeEventHandler> logger,
            IServiceProvider serviceProvider,

            IAgentActionValidator actionValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _actionValidator = actionValidator ?? throw new ArgumentNullException(nameof(actionValidator));
        }

        public async Task OnSimulationTimeChangedAsync(DateTime simulationTime, TimeSpan elapsed)
        {
            // Only process if we have a significant time change and it's been a while since our last run
            if (elapsed <= TimeSpan.Zero || 
                (simulationTime - _lastRunTime).TotalHours < MinimumRunIntervalHours)
            {
                return;
            }

            // Use a lock to prevent concurrent processing
            if (!await _processingLock.WaitAsync(0))
            {
                _logger.LogDebug("NBA processing already in progress, skipping this update");
                return;
            }

            try
            {
                _logger.LogInformation("Processing NBA recommendations at simulation time: {SimulationTime}", simulationTime);
                
                // Process NBA recommendations for active contacts
                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                
                // Get active contacts that need NBA recommendations
                // Using IsActiveClient as a proxy for active status
                var activeContacts = await dbContext.Contacts
                    .Where(c => c.IsActiveClient)
                    .ToListAsync();
                
                _logger.LogInformation(new EventId(1001, "NbaProcessing"), "Found {Count} active contacts for NBA processing", activeContacts.Count);
                
                // Process each contact
                foreach (var contact in activeContacts)
                {
                    try
                    {
                        await ProcessContactRecommendations(contact, simulationTime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(new EventId(1002, "NbaContactError"), ex, "Error processing NBA recommendations for contact {ContactId}", contact.Id);
                    }
                }
                
                _lastRunTime = simulationTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(1003, "NbaHandlerError"), ex, "Error in NBA time event handler");
            }
            finally
            {
                _processingLock.Release();
            }
        }

        private async Task ProcessContactRecommendations(Contact contact, DateTime simulationTime)
        {
            try
            {
                _logger.LogDebug(new EventId(1004, "NbaContactProcessing"), "Processing NBA recommendations for contact: {ContactId}", contact.Id);
                
                // Get NBA recommendations for this contact
                var response = await _nbaAgent.RecommendNextBestActionsAsync(
                    contactId: contact.Id,
                    organizationId: contact.OrganizationId,
                    requestingAgentId: Guid.Empty, // System-initiated
                    maxRecommendations: 3,
                    traceId: $"nba-sim-{Guid.NewGuid()}");
                
                if (!response.Success)
                {
                    _logger.LogWarning("Failed to get NBA recommendations for contact {ContactId}: {Message}", 
                        contact.Id, response.Message);
                    return;
                }
                
                // Process the recommendations
                if (response.Data is Dictionary<string, object> data && 
                    data.TryGetValue("recommendations", out var recsObj) && 
                    recsObj is IEnumerable<NbaRecommendation> recommendations)
                {
                    foreach (var recommendation in recommendations)
                    {
                        try
                        {
                            // Set the recommendation properties for validation
                            recommendation.ContactId = contact.Id.ToString();
                            recommendation.OrganizationId = contact.OrganizationId.ToString();
                            
                            // Add any additional parameters needed for validation
                            recommendation.Parameters["DueDate"] = recommendation.RecommendedTime ?? simulationTime.AddDays(1);
                            recommendation.Parameters["Metadata"] = new Dictionary<string, object>
                            {
                                ["nbaRecommendationId"] = recommendation.Id,
                                ["confidenceScore"] = recommendation.ConfidenceScore,
                                ["source"] = "NBA_TimeSimulation"
                            };
                            
                            // Validate the action before processing
                            var validationContext = new AgentActionValidationContext
                            {
                                ContactId = Guid.Parse(contact.Id),
                                OrganizationId = Guid.Parse(contact.OrganizationId),
                                UserId = "system",
                                AgentId = nameof(NbaTimeEventHandler),
                                AgentType = "NBA",
                                AdditionalContext = new Dictionary<string, object>
                                {
                                    ["simulationTime"] = simulationTime,
                                    ["recommendation"] = recommendation
                                },
                                UserOverrides = new Dictionary<string, object>() // Empty for system-initiated actions
                            };
                            
                            var traceId = Guid.NewGuid().ToString();
                            var validatedRecommendations = await _actionValidator.ValidateAgentActionsAsync(
                                new List<NbaRecommendation> { recommendation }, 
                                validationContext, 
                                new Dictionary<string, object>(),
                                traceId);
                            
                            if (validatedRecommendations == null || !validatedRecommendations.Any())
                            {
                                _logger.LogInformation("Skipping invalid NBA recommendation for contact {ContactId}", contact.Id);
                                continue;
                            }
                            
                            // Process the valid recommendation (e.g., create tasks, send notifications, etc.)
                            // Use the first validated recommendation (should only be one in this case)
                            await ProcessValidRecommendation(contact, validatedRecommendations.First(), simulationTime);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing NBA recommendation for contact {ContactId}", contact.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing contact recommendations for contact {ContactId}", contact.Id);
            }
        }

        private async Task ProcessValidRecommendation(Contact contact, NbaRecommendation recommendation, DateTime simulationTime)
        {
            _logger.LogInformation("Processing valid NBA recommendation for contact {ContactId}: {ActionType} - {Description}", 
                contact.Id, recommendation.ActionType, recommendation.Description);
            
            try
            {
                // Create a task for this recommendation
                var task = new TaskItem
                {
                    Id = Guid.NewGuid().ToString(),
                    TaskId = Guid.NewGuid().ToString(),
                    Title = $"NBA: {recommendation.Description}",
                    Description = $"Recommended action: {recommendation.Description}",
                    DueDate = recommendation.RecommendedTime ?? simulationTime.AddDays(1),
                    Priority = recommendation.Priority,
                    Status = "Pending",
                    ContactId = contact.Id,
                    ContactName = $"{contact.FirstName} {contact.LastName}".Trim(),
                    OrganizationId = contact.OrganizationId,
                    CreatedAt = simulationTime,
                    UpdatedAt = simulationTime,
                    NbaRecommendationId = recommendation.Id,
                    ConfidenceScore = recommendation.ConfidenceScore,
                    ValidationReason = recommendation.ValidationReason ?? "Auto-generated from NBA recommendation",
                    RequiresApproval = recommendation.RequiresApproval,
                    ApprovalRequestId = recommendation.ApprovalRequestId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["nbaRecommendationId"] = recommendation.Id,
                        ["actionType"] = recommendation.ActionType,
                        ["confidenceScore"] = recommendation.ConfidenceScore,
                        ["source"] = "NBA_TimeSimulation"
                    }
                };
                
                // Save the task to the database
                await using var scope = _serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                
                dbContext.TaskItems.Add(task);
                await dbContext.SaveChangesAsync();
                
                _logger.LogInformation("Created task {TaskId} from NBA recommendation for contact {ContactId}", 
                    task.Id, contact.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task from NBA recommendation for contact {ContactId}", contact.Id);
                throw; // Re-throw to allow retry logic if needed
            }
        }

        // Other required interface methods with empty implementations since we don't need them
        public Task OnSimulationPausedAsync(DateTime simulationTime) => Task.CompletedTask;
        public Task OnSimulationResumedAsync(DateTime simulationTime) => Task.CompletedTask;
        public Task OnSimulationSpeedChangedAsync(DateTime simulationTime, double newSpeed) => Task.CompletedTask;

        public void Dispose()
        {
            _processingLock?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
