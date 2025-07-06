using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Emma.Api.IntegrationTests
{
    public class OnboardingAndProfileFlowTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public OnboardingAndProfileFlowTests(TestWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegisterAndProfileFlow_WorksAndIsolated()
        {
            // Use a unique email for each run
            var uniqueEmail = $"testuser_{System.Guid.NewGuid():N}@example.com";
            var orgName = $"TestOrg_{System.Guid.NewGuid():N}";

            // 1. Register
            var registerRequest = new
            {
                organizationName = orgName,
                email = uniqueEmail,
                password = "TestPass123!",
                planKey = "Pro",
                seatCount = 5
            };
            var regResponse = await _client.PostAsJsonAsync("/api/Onboarding/register", registerRequest);
            regResponse.EnsureSuccessStatusCode();
            var token = await regResponse.Content.ReadAsStringAsync();
            token.Should().NotBeNullOrWhiteSpace();

            // 2. Get Profile (unauthenticated, Sprint 1 stub)
            var profileResponse = await _client.GetAsync("/api/Account/profile");
            profileResponse.EnsureSuccessStatusCode();
            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            profileJson.Should().Contain(orgName).And.Contain(uniqueEmail);
        }
    }
}
