using System;
using System.Threading.Tasks;
using Emma.Core.Services;
using Emma.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Moq;
using Emma.Models.Interfaces;
using Emma.Data;

namespace Emma.IntegrationTests.Services
{
    public class TimeSimulatorServiceIntegrationTests : IClassFixture<TestDatabaseFixture>, IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly TimeSimulatorService _timeSimulator;
        private readonly IAppDbContext _dbContext;
        private readonly IServiceProvider _serviceProvider;

        public TimeSimulatorServiceIntegrationTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _dbContext = fixture.DbContext;
            _serviceProvider = fixture.ServiceProvider;
            
            // Create a mock logger
            var logger = new Mock<ILogger<TimeSimulatorService>>().Object;
            
            // Create the service with test configuration
            var options = Microsoft.Extensions.Options.Options.Create(new TimeSimulationOptions
            {
                DefaultTimeScale = 1.0,
                StartPaused = true,
                MinTimeScale = 0.1,
                MaxTimeScale = 1000.0
            });
            
            _timeSimulator = new TimeSimulatorService(options, logger, _serviceProvider);
        }

        public Task InitializeAsync()
        {
            // Reset the time simulator state before each test
            _timeSimulator.SetPausedAsync(true).Wait();
            _timeSimulator.SetTimeScaleAsync(1.0).Wait();
            
            // Ensure database is in a known state
            _dbContext.Database.EnsureCreated();
            TestDataSeeder.SeedTestData(_dbContext).Wait();
            
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            // Clean up resources
            await _timeSimulator.SetPausedAsync(true);
        }

        [Fact]
        public async Task TimeAdvances_WhenNotPaused()
        {
            // Arrange
            var initialTime = _timeSimulator.CurrentSimulationTime;
            await _timeSimulator.SetTimeScaleAsync(60.0); // 1 second = 1 minute
            
            // Act
            await _timeSimulator.SetPausedAsync(false);
            await Task.Delay(1100); // Wait a bit over 1 second
            
            // Assert
            var elapsed = _timeSimulator.CurrentSimulationTime - initialTime;
            Assert.True(elapsed.TotalMinutes >= 1.0, "At least 1 minute should have passed in simulation time");
        }

        [Fact]
        public async Task TimeStandsStill_WhenPaused()
        {
            // Arrange
            var initialTime = _timeSimulator.CurrentSimulationTime;
            await _timeSimulator.SetTimeScaleAsync(60.0);
            
            // Act - time is paused by default
            await Task.Delay(1100); // Wait a bit over 1 second
            
            // Assert
            var elapsed = _timeSimulator.CurrentSimulationTime - initialTime;
            Assert.True(elapsed.TotalSeconds < 1, "Simulation time should not advance when paused");
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(5.0)]
        [InlineData(60.0)]
        public async Task TimeAdvancesAtCorrectRate(double timeScale)
        {
            // Arrange
            var initialTime = _timeSimulator.CurrentSimulationTime;
            await _timeSimulator.SetTimeScaleAsync(timeScale);
            
            // Act
            await _timeSimulator.SetPausedAsync(false);
            await Task.Delay(1000); // Wait 1 second
            await _timeSimulator.SetPausedAsync(true);
            
            // Assert
            var elapsed = _timeSimulator.CurrentSimulationTime - initialTime;
            var expectedSeconds = timeScale; // 1 second real time * timeScale
            Assert.InRange(elapsed.TotalSeconds, expectedSeconds * 0.9, expectedSeconds * 1.1);
        }

        [Fact]
        public async Task TimeScaleChange_TakesEffectImmediately()
        {
            // Arrange
            var initialTime = _timeSimulator.CurrentSimulationTime;
            await _timeSimulator.SetPausedAsync(false);
            
            // Act - Change time scale while running
            await _timeSimulator.SetTimeScaleAsync(10.0);
            await Task.Delay(1000); // Wait 1 second at 10x speed
            
            var timeAfterFirstScale = _timeSimulator.CurrentSimulationTime;
            
            await _timeSimulator.SetTimeScaleAsync(60.0); // Faster
            await Task.Delay(1000); // Wait another second at 60x speed
            
            // Assert
            var firstElapsed = timeAfterFirstScale - initialTime;
            var secondElapsed = _timeSimulator.CurrentSimulationTime - timeAfterFirstScale;
            
            Assert.True(secondElapsed.TotalSeconds > firstElapsed.TotalSeconds * 5, 
                "Time should advance faster after increasing time scale");
        }

        [Fact]
        public async Task EventHandlers_ReceiveTimeUpdates()
        {
            // Arrange
            var eventHandler = new TestTimeEventHandler();
            _timeSimulator.AddEventHandler(eventHandler);
            
            // Act
            await _timeSimulator.SetTimeScaleAsync(60.0);
            await _timeSimulator.SetPausedAsync(false);
            await Task.Delay(1100); // Wait a bit over 1 second
            await _timeSimulator.SetPausedAsync(true);
            
            // Assert
            Assert.True(eventHandler.TimeChangedEvents > 0, "Time change events should be raised");
            Assert.True(eventHandler.LastSimulationTime > DateTime.MinValue, "Should have received time updates");
        }
    }
    
    public class TestTimeEventHandler : ISimulationEventHandler
    {
        public int TimeChangedEvents { get; private set; }
        public DateTime LastSimulationTime { get; private set; }
        
        public Task OnSimulationTimeChangedAsync(DateTime simulationTime, TimeSpan elapsed)
        {
            TimeChangedEvents++;
            LastSimulationTime = simulationTime;
            return Task.CompletedTask;
        }
        
        public Task OnSimulationPausedAsync(DateTime simulationTime) => Task.CompletedTask;
        public Task OnSimulationResumedAsync(DateTime simulationTime) => Task.CompletedTask;
        public Task OnSimulationSpeedChangedAsync(DateTime simulationTime, double newSpeed) => Task.CompletedTask;
    }
}
