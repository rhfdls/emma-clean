using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Emma.Core.Services;

namespace Emma.Core.Tests.Services
{
    public class TimeSimulatorServiceTests : IDisposable
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IOptions<TimeSimulationOptions>> _optionsMock;
        private readonly Mock<ILogger<TimeSimulatorService>> _loggerMock;
        private readonly TimeSimulatorService _service;
        private readonly CancellationTokenSource _cts;

        public TimeSimulatorServiceTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _optionsMock = new Mock<IOptions<TimeSimulationOptions>>();
            _loggerMock = new Mock<ILogger<TimeSimulatorService>>();
            _cts = new CancellationTokenSource();

            // Setup default options
            var options = new TimeSimulationOptions
            {
                DefaultTimeScale = 1.0,
                StartPaused = true,
                MinTimeScale = 0.1,
                MaxTimeScale = 1000.0
            };

            _optionsMock.Setup(o => o.Value).Returns(options);

            _service = new TimeSimulatorService(
                _serviceProviderMock.Object,
                _optionsMock.Object,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _service.Dispose();
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Assert
            Assert.NotNull(_service);
            Assert.True(_service.IsPaused);
            Assert.Equal(1.0, _service.TimeScale);
        }

        [Fact]
        public async Task SetTimeScaleAsync_WithValidValue_UpdatesTimeScale()
        {
            // Arrange
            const double newScale = 2.0;

            // Act
            await _service.SetTimeScaleAsync(newScale);

            // Assert
            Assert.Equal(newScale, _service.TimeScale);
        }

        [Fact]
        public async Task SetTimeScaleAsync_WithValueBelowMin_ClampsToMin()
        {
            // Arrange
            const double belowMin = 0.05; // Min is 0.1

            // Act
            await _service.SetTimeScaleAsync(belowMin);

            // Assert
            Assert.Equal(0.1, _service.TimeScale);
        }

        [Fact]
        public async Task SetTimeScaleAsync_WithValueAboveMax_ClampsToMax()
        {
            // Arrange
            const double aboveMax = 2000.0; // Max is 1000.0

            // Act
            await _service.SetTimeScaleAsync(aboveMax);

            // Assert
            Assert.Equal(1000.0, _service.TimeScale);
        }

        [Fact]
        public async Task SetPausedAsync_WhenPaused_UpdatesIsPaused()
        {
            // Act
            await _service.SetPausedAsync(true);

            // Assert
            Assert.True(_service.IsPaused);
        }

        [Fact]
        public async Task SetPausedAsync_WhenResumed_UpdatesIsPaused()
        {
            // Arrange
            await _service.SetPausedAsync(true);

            // Act
            await _service.SetPausedAsync(false);


            // Assert
            Assert.False(_service.IsPaused);
        }

        [Fact]
        public async Task CurrentSimulationTime_WhenTimePasses_UpdatesCorrectly()
        {
            // Arrange
            var startTime = _service.CurrentSimulationTime;
            await _service.SetPausedAsync(false);
            await _service.SetTimeScaleAsync(60.0); // 1 second = 1 minute

            // Act
            await Task.Delay(1100); // Wait a bit over 1 second
            var currentTime = _service.CurrentSimulationTime;

            // Assert
            var elapsed = currentTime - startTime;
            Assert.True(elapsed.TotalMinutes >= 1.0, "At least 1 minute should have passed in simulation time");
        }

        [Fact]
        public async Task ExecuteAsync_WhenPaused_DoesNotAdvanceTime()
        {
            // Arrange
            var startTime = _service.CurrentSimulationTime;
            await _service.SetPausedAsync(true);

            // Act
            await Task.Delay(1100); // Wait a bit over 1 second
            var currentTime = _service.CurrentSimulationTime;

            // Assert
            var elapsed = currentTime - startTime;
            Assert.True(elapsed.TotalSeconds < 1.0, "Simulation time should not advance when paused");
        }
    }
}
