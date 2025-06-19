using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Configuration
{
    public class YamlCapabilityMetricsTests : IDisposable
    {
        private readonly YamlCapabilityMetrics _metrics;
        private readonly Mock<ILogger<YamlCapabilityMetrics>> _loggerMock;
        private const string TestFilePath = "test-config.yaml";

        public YamlCapabilityMetricsTests()
        {
            _loggerMock = new Mock<ILogger<YamlCapabilityMetrics>>();
            _metrics = new YamlCapabilityMetrics(_loggerMock.Object);
        }

        [Fact]
        public void RecordFileLoad_ShouldIncrementCounters()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            _metrics.RecordFileLoad(TestFilePath, duration, true, false);
            _metrics.RecordFileLoad(TestFilePath, duration, false, false); // failed load
            _metrics.RecordFileLoad(TestFilePath, duration, true, true); // from cache

            // Assert
            var snapshot = _metrics.GetMetricsSnapshot();
            Assert.Equal(3, snapshot.TotalFileLoads);
            Assert.Equal(1, snapshot.FailedFileLoads);
            Assert.Equal(1, snapshot.CacheHits);
            Assert.Equal(2, snapshot.CacheMisses);
            Assert.Equal(0.333, snapshot.CacheHitRatio, 3);
        }

        [Fact]
        public void RecordValidation_ShouldIncrementCounters()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(50);

            // Act - 2 successful validations, 1 failed with 3 errors, 1 failed with 1 error
            _metrics.RecordValidation(TestFilePath, duration, true);
            _metrics.RecordValidation(TestFilePath, duration, false, 3);
            _metrics.RecordValidation(TestFilePath, duration, true);
            _metrics.RecordValidation(TestFilePath, duration, false, 1);

            // Assert
            var snapshot = _metrics.GetMetricsSnapshot();
            Assert.Equal(4, snapshot.TotalValidations);
            Assert.Equal(2, snapshot.FailedValidations);
            Assert.Equal(4, snapshot.TotalValidationErrors);
            Assert.Equal(2.0, snapshot.AverageValidationErrors);
        }

        [Fact]
        public void RecordHotReload_ShouldIncrementCounters()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(200);

            // Act - 2 successful reloads, 1 failed
            _metrics.RecordHotReload(TestFilePath, duration, true, 5, 10);
            _metrics.RecordHotReload(TestFilePath, duration, false, 0, 0);
            _metrics.RecordHotReload(TestFilePath, duration, true, 3, 8);

            // Assert
            var snapshot = _metrics.GetMetricsSnapshot();
            Assert.Equal(3, snapshot.TotalHotReloads);
            Assert.Equal(1, snapshot.FailedHotReloads);
            Assert.Equal(4.0, snapshot.AverageAgentsPerLoad); // (5 + 3) / 2
            Assert.Equal(9.0, snapshot.AverageCapabilitiesPerLoad); // (10 + 8) / 2
        }

        [Fact]
        public void RecordCacheAccess_ShouldIncrementCounters()
        {
            // Act
            _metrics.RecordCacheAccess(true);
            _metrics.RecordCacheAccess(true);
            _metrics.RecordCacheAccess(false);
            _metrics.RecordCacheAccess(true);

            // Assert
            var snapshot = _metrics.GetMetricsSnapshot();
            Assert.Equal(3, snapshot.CacheHits);
            Assert.Equal(1, snapshot.CacheMisses);
            Assert.Equal(0.75, snapshot.CacheHitRatio);
        }

        [Fact]
        public void Reset_ShouldClearAllCounters()
        {
            // Arrange - Record some metrics
            _metrics.RecordFileLoad(TestFilePath, TimeSpan.Zero, true, false);
            _metrics.RecordValidation(TestFilePath, TimeSpan.Zero, true);
            _metrics.RecordHotReload(TestFilePath, TimeSpan.Zero, true, 1, 2);
            _metrics.RecordCacheAccess(true);

            // Act
            _metrics.Reset();

            // Assert
            var snapshot = _metrics.GetMetricsSnapshot();
            Assert.Equal(0, snapshot.TotalFileLoads);
            Assert.Equal(0, snapshot.TotalValidations);
            Assert.Equal(0, snapshot.TotalHotReloads);
            Assert.Equal(0, snapshot.CacheHits);
            Assert.Equal(0, snapshot.CacheMisses);
        }

        [Fact]
        public void GetMetricsSnapshot_ShouldReturnConsistentSnapshot()
        {
            // Arrange - Start recording metrics
            _metrics.RecordFileLoad(TestFilePath, TimeSpan.Zero, true, false);
            _metrics.RecordValidation(TestFilePath, TimeSpan.Zero, true);
            _metrics.RecordHotReload(TestFilePath, TimeSpan.Zero, true, 1, 2);
            _metrics.RecordCacheAccess(true);

            // Act
            var snapshot1 = _metrics.GetMetricsSnapshot();
            var snapshot2 = _metrics.GetMetricsSnapshot();

            // Assert - Snapshots should be equal but distinct instances
            Assert.NotSame(snapshot1, snapshot2);
            Assert.Equal(snapshot1.TotalFileLoads, snapshot2.TotalFileLoads);
            Assert.Equal(snapshot1.TotalValidations, snapshot2.TotalValidations);
            Assert.Equal(snapshot1.TotalHotReloads, snapshot2.TotalHotReloads);
            Assert.Equal(snapshot1.CacheHits, snapshot2.CacheHits);
            Assert.True(snapshot1.Timestamp <= snapshot2.Timestamp);
        }

        [Fact]
        public void RecordFileLoad_WithNullFilePath_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _metrics.RecordFileLoad(null, TimeSpan.Zero, true, false));
            Assert.Throws<ArgumentException>(() => 
                _metrics.RecordFileLoad("", TimeSpan.Zero, true, false));
        }

        [Fact]
        public void RecordValidation_WithNullFilePath_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _metrics.RecordValidation(null, TimeSpan.Zero, true));
            Assert.Throws<ArgumentException>(() => 
                _metrics.RecordValidation("", TimeSpan.Zero, true));
        }

        [Fact]
        public void RecordHotReload_WithNullFilePath_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _metrics.RecordHotReload(null, TimeSpan.Zero, true, 1, 1));
            Assert.Throws<ArgumentException>(() => 
                _metrics.RecordHotReload("", TimeSpan.Zero, true, 1, 1));
        }

        [Fact]
        public void RecordHotReload_WithNegativeCounts_ShouldHandleGracefully()
        {
            // Act - Should not throw
            _metrics.RecordHotReload(TestFilePath, TimeSpan.Zero, true, -1, -1);
            
            // Assert - Should be treated as zero
            var snapshot = _metrics.GetMetricsSnapshot();
            Assert.Equal(1, snapshot.TotalHotReloads);
            Assert.Equal(0, snapshot.AverageAgentsPerLoad);
        }

        [Fact]
        public void GetMetricsSnapshot_WithNoMetrics_ShouldReturnEmptyCounters()
        {
            // Act
            var snapshot = _metrics.GetMetricsSnapshot();

            // Assert
            Assert.Equal(0, snapshot.TotalFileLoads);
            Assert.Equal(0, snapshot.TotalValidations);
            Assert.Equal(0, snapshot.TotalHotReloads);
            Assert.Equal(0, snapshot.CacheHits);
            Assert.Equal(0, snapshot.CacheMisses);
            Assert.Equal(0, snapshot.AverageAgentsPerLoad);
            Assert.Equal(0, snapshot.AverageCapabilitiesPerLoad);
        }

        public void Dispose()
        {
            _metrics.Dispose();
        }
    }
}
