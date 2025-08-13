using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Emma.Api.Dtos;
using FluentAssertions;

namespace Emma.Tests.Integration
{
    #region CreateContact Swagger Test
    public class ContactIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ContactIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostContact_ValidPayload_ReturnsCreatedAndCanRoundtrip()
        {
            // Arrange
            var dto = new ContactCreateDto
            {
                OrganizationId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Doe",
                PreferredName = "JD",
                Title = "Ms.",
                JobTitle = "Manager",
                Company = "Acme Inc.",
                Department = "Sales",
                Source = "Referral",
                OwnerId = null,
                PreferredContactMethod = "email",
                PreferredContactTime = "morning",
                Notes = "Test contact",
                ProfilePictureUrl = "https://example.com/pic.jpg"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Contact", dto);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await response.Content.ReadFromJsonAsync<ContactReadDto>();
            created.Should().NotBeNull();
            created!.FirstName.Should().Be(dto.FirstName);
            created.LastName.Should().Be(dto.LastName);
            created.OrganizationId.Should().Be(dto.OrganizationId);

            // Roundtrip GET
            var getResp = await _client.GetAsync($"/api/Contact/{created.Id}");
            getResp.StatusCode.Should().Be(HttpStatusCode.OK);
            var fetched = await getResp.Content.ReadFromJsonAsync<ContactReadDto>();
            fetched.Should().NotBeNull();
            fetched!.FirstName.Should().Be(dto.FirstName);
            fetched.LastName.Should().Be(dto.LastName);
        }

        [Fact]
        public async Task PostContact_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange: Missing FirstName and OrganizationId
            var dto = new ContactCreateDto
            {
                LastName = "Doe"
            };
            var response = await _client.PostAsJsonAsync("/api/Contact", dto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
    #endregion
}
