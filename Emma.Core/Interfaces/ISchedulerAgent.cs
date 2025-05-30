namespace Emma.Core.Interfaces;

public interface ISchedulerAgent
{
    Task ScheduleFollowupAsync(string userId, DateTimeOffset when, string message);
}
