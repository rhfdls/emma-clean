using System;
using System.Threading.Tasks;
using Emma.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Emma.Api.Services;

public class EmailAgentStub : IEmailAgent
{
    private readonly ILogger<EmailAgentStub> _logger;

    public EmailAgentStub(ILogger<EmailAgentStub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient email cannot be null or whitespace", nameof(to));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject cannot be null or whitespace", nameof(subject));
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Email body cannot be null or whitespace", nameof(body));
            
        _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
        _logger.LogDebug("Email body: {Body}", body);
        
        // In a real implementation, we would send the email here
        // For the stub, we'll just log and return a completed task
        return Task.CompletedTask;
    }
}
