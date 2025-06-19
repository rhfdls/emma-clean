using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Emma.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Emma.IntegrationTests.Api
{
    public class TimeSimulationApiTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public TimeSimulationApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetStatus_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/timesimulation/status");
            
            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task Pause_WhenCalled_ReturnsSuccess()
        {
            // Act
            var response = await _client.PostAsync("/api/timesimulation/pause", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify status reflects paused state
            var statusResponse = await _client.GetAsync("/api/timesimulation/status");
            var status = await statusResponse.Content.ReadFromJsonAsync<TimeSimulationStatusResponse>();
            Assert.True(status.IsPaused);
        }

        [Fact]
        public async Task Resume_WhenCalled_ReturnsSuccess()
        {
            // Arrange - ensure we're starting from a paused state
            await _client.PostAsync("/api/timesimulation/pause", null);
            
            // Act
            var response = await _client.PostAsync("/api/timesimulation/resume", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify status reflects running state
            var statusResponse = await _client.GetAsync("/api/timesimulation/status");
            var status = await statusResponse.Content.ReadFromJsonAsync<TimeSimulationStatusResponse>();
            Assert.False(status.IsPaused);
        }

        [Fact]
        public async Task SetTimeScale_WithValidValue_ReturnsSuccess()
        {
            // Arrange
            const double newScale = 5.0;
            
            // Act
            var response = await _client.PostAsync($"/api/timesimulation/timescale?scale={newScale}", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify the time scale was updated
            var statusResponse = await _client.GetAsync("/api/timesimulation/status");
            var status = await statusResponse.Content.ReadFromJsonAsync<TimeSimulationStatusResponse>();
            Assert.Equal(newScale, status.TimeScale);
        }

        [Fact]
        public async Task SetTimeScale_WithInvalidValue_ReturnsBadRequest()
        {
            // Arrange
            const double invalidScale = -1.0;
            
            // Act
            var response = await _client.PostAsync($"/api/timesimulation/timescale?scale={invalidScale}", null);
            
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("day")]
        [InlineData("week")]
        [InlineData("month")]
        [InlineData("year")]
        public async Task SetPreset_WithValidPreset_ReturnsSuccess(string preset)
        {
            // Act
            var response = await _client.PostAsync($"/api/timesimulation/preset/{preset}", null);
            
            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify the time scale was updated to a positive value
            var statusResponse = await _client.GetAsync("/api/timesimulation/status");
            var status = await statusResponse.Content.ReadFromJsonAsync<TimeSimulationStatusResponse>();
            Assert.True(status.TimeScale > 0);
        }

        [Fact]
        public async Task SetPreset_WithInvalidPreset_ReturnsBadRequest()
        {
            // Arrange
            const string invalidPreset = "invalid";
            
            // Act
            var response = await _client.PostAsync($"/api/timesimulation/preset/{invalidPreset}", null);
            
            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    public class TimeSimulationStatusResponse
    {
        public DateTime CurrentSimulationTime { get; set; }
        public double TimeScale { get; set; }
        public bool IsPaused { get; set; }
    }
}
