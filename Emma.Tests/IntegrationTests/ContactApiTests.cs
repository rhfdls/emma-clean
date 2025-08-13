using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Emma.Models.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;

namespace Emma.Tests.IntegrationTests
{
    public class ContactApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ContactApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Environment", "Testing");
            });
            _client = _factory.CreateClient();
        }

        [Fact(DisplayName = "POST /contacts creates and returns contact")] // SPRINT2
        public async Task PostContact_CreatesAndReturnsContact()
        {
            var payload = new
            {
                firstName = "Test",
                lastName = "User",
                organizationId = TestData.OrganizationId,
                emailAddresses = new[] { new { address = "test.user@example.com" } },
                phoneNumbers = new[] { new { number = "+1234567890" } },
                addresses = new[] { new { line1 = "123 Main St", city = "Testville", state = "TS", postalCode = "12345" } }
            };

            var response = await _client.PostAsJsonAsync("/contacts", payload);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<Contact>();
            created.Should().NotBeNull();
            created.FirstName.Should().Be("Test");
            created.LastName.Should().Be("User");
            created.EmailAddresses.Should().ContainSingle(e => e.Address == "test.user@example.com");
        }

        [Fact(DisplayName = "POST /contacts with missing fields returns 400")] // SPRINT2
        public async Task PostContact_MissingFields_ReturnsBadRequest()
        {
            var payload = new { lastName = "User" };
            var response = await _client.PostAsJsonAsync("/contacts", payload);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "GET /contacts/{id} returns created contact")] // SPRINT2
        public async Task GetContact_ReturnsCreatedContact()
        {
            var payload = new
            {
                firstName = "Test2",
                lastName = "User2",
                organizationId = TestData.OrganizationId
            };
            var postResponse = await _client.PostAsJsonAsync("/contacts", payload);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await postResponse.Content.ReadFromJsonAsync<Contact>();
            var getResponse = await _client.GetAsync($"/contacts/{created.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResponse.Content.ReadFromJsonAsync<Contact>();
            fetched.Should().NotBeNull();
            fetched.Id.Should().Be(created.Id);
            fetched.FirstName.Should().Be("Test2");
        }
    }

    public static class TestData
    {
        // Replace with logic to get or create an isolated org for tests
        public static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    }
}
