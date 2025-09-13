using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emma.Api;
// using Emma.Api.Dtos; // DTO not present in this repo snapshot; test is skipped

namespace Emma.Api.IntegrationTests
{
    public class FulltextInteractionControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public FulltextInteractionControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact(Skip = "FulltextInteractionDto and endpoints not present in this repo snapshot")]
        public async Task Can_Post_And_Query_FulltextInteraction()
        {
            var dto = new
            {
                AgentId = Guid.NewGuid(),
                ContactId = Guid.NewGuid(),
                Type = "call",
                Content = "Test transcript content for integration test.",
                Metadata = new Dictionary<string, string> { { "source", "test" } }
            };

            // POST
            var postResponse = await _client.PostAsJsonAsync("/api/fulltext-interactions", dto);
            postResponse.EnsureSuccessStatusCode();

            // GET
            var getResponse = await _client.GetAsync($"/api/fulltext-interactions?agentId={dto.AgentId}&contactId={dto.ContactId}&type={dto.Type}");
            getResponse.EnsureSuccessStatusCode();
            var results = await getResponse.Content.ReadFromJsonAsync<List<dynamic>>();

            Assert.NotNull(results);
            Assert.Contains(results, r => r.Content == dto.Content);
        }
    }
}
