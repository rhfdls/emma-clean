using System.Threading.Tasks;
using Emma.Api.Models;

namespace Emma.Api.Interfaces
{
    // SPRINT1: Onboarding service interface for registration/profile
    public interface IOnboardingService
    {
        Task<string?> RegisterOrganizationAsync(RegisterRequest request);
        Task<ProfileResponse?> GetProfileAsync(string email);
    }
}
