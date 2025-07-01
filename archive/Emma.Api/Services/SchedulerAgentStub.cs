using System;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services;

public class SchedulerAgentStub : ISchedulerAgent
{
    private readonly ILogger<SchedulerAgentStub> _logger;

    public SchedulerAgentStub(ILogger<SchedulerAgentStub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ScheduleFollowupAsync(string userId, DateTimeOffset when, string message)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        if (string.IsNullOrEmpty(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
            
        _logger.LogInformation("Scheduling follow-up for user {UserId} at {When}: {Message}", 
            userId, when, message);
            
        return Task.CompletedTask;
    }
}
