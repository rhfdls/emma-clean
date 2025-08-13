using System.Threading.Tasks;

namespace Emma.Core.Services
{
    // SPRINT2: Simple email sender interface for verification emails
    public interface IEmailSender
    {
        Task SendVerificationAsync(string email, string verificationUrl);
    }
}
