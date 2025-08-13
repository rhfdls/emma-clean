using System;
using System.Threading.Tasks;
using Emma.Core.Services;

namespace Emma.Infrastructure.Services
{
    // SPRINT2: Dev stub email sender that logs verification URLs to console
    public class EmailSenderDev : IEmailSender
    {
        public Task SendVerificationAsync(string email, string verificationUrl)
        {
            Console.WriteLine($"[EmailSenderDev] To: {email} | Verify: {verificationUrl}");
            return Task.CompletedTask;
        }
    }
}
