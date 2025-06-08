namespace Emma.Core.Interfaces;

public interface IEmailAgent
{
    Task SendEmailAsync(string to, string subject, string body);
}
