using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Default implementation of <see cref="IYamlCapabilityMetrics"/> that tracks various metrics
    /// related to YAML capability loading and validation.
    /// </summary>
    public class YamlCapabilityMetrics : IYamlCapabilityMetrics
    {
        private readonly ILogger<YamlCapabilityMetrics> _logger;
        private readonly ConcurrentDictionary<string, FileMetrics> _fileMetrics = new();
        private readonly object _syncLock = new();
        
        private long _totalFileLoads;
        private long _failedFileLoads;
        private long _cacheHits;
        private long _cacheMisses;
        private long _totalValidations;
        private long _failedValidations;
        private long _totalValidationErrors;
        private long _totalHotReloads;
        private long _failedHotReloads;
        private double _totalAgentsLoaded;
        private double _totalCapabilitiesLoaded;
        private long _successfulLoads;

        public YamlCapabilityMetrics(ILogger<YamlCapabilityMetrics> logger = null)
        {
            _logger = logger ?? new LoggerFactory().CreateLogger<YamlCapabilityMetrics>();
        }

        public void RecordFileLoad(string filePath, TimeSpan duration, bool success, bool fromCache)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            Interlocked.Increment(ref _totalFileLoads);
            if (!success) Interlocked.Increment(ref _failedFileLoads);
            if (fromCache) Interlocked.Increment(ref _cacheHits);
            else Interlocked.Increment(ref _cacheMisses);

            var metrics = _fileMetrics.GetOrAdd(filePath, _ => new FileMetrics());
            metrics.RecordLoad(duration, success, fromCache);

            _logger.LogDebug("File {FilePath} loaded in {DurationMs}ms (Cache: {FromCache}, Success: {Success})",
                filePath, duration.TotalMilliseconds, fromCache, success);
        }

        public void RecordValidation(string filePath, TimeSpan duration, bool success, int errorCount = 0)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            Interlocked.Increment(ref _totalValidations);
            if (!success)
            {
                Interlocked.Increment(ref _failedValidations);
                Interlocked.Add(ref _totalValidationErrors, errorCount);
            }

            var metrics = _fileMetrics.GetOrAdd(filePath, _ => new FileMetrics());
            metrics.RecordValidation(duration, success, errorCount);

            _logger.LogDebug("Validation for {FilePath} completed in {DurationMs}ms (Success: {Success}, Errors: {ErrorCount})",
                filePath, duration.TotalMilliseconds, success, errorCount);
        }

        public void RecordHotReload(string filePath, TimeSpan duration, bool success, int agentCount, int capabilityCount)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            Interlocked.Increment(ref _totalHotReloads);
            if (!success) Interlocked.Increment(ref _failedHotReloads);

            if (success)
            {
                Interlocked.Increment(ref _successfulLoads);
                // Use atomic operations for double values
                lock (_syncLock)
                {
                    _totalAgentsLoaded += agentCount;
                    _totalCapabilitiesLoaded += capabilityCount;
                }
            }

            var metrics = _fileMetrics.GetOrAdd(filePath, _ => new FileMetrics());
            metrics.RecordHotReload(duration, success, agentCount, capabilityCount);

            _logger.LogInformation("Hot reload for {FilePath} completed in {DurationMs}ms (Success: {Success}, Agents: {AgentCount}, Capabilities: {CapabilityCount})",
                filePath, duration.TotalMilliseconds, success, agentCount, capabilityCount);
        }

        public void RecordCacheAccess(bool isHit)
        {
            if (isHit) Interlocked.Increment(ref _cacheHits);
            else Interlocked.Increment(ref _cacheMisses);
        }

        public YamlCapabilityMetricsSnapshot GetMetricsSnapshot()
        {
            var snapshot = new YamlCapabilityMetricsSnapshot
            {
                TotalFileLoads = _totalFileLoads,
                FailedFileLoads = _failedFileLoads,
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses,
                TotalValidations = _totalValidations,
                FailedValidations = _failedValidations,
                TotalValidationErrors = _totalValidationErrors,
                TotalHotReloads = _totalHotReloads,
                FailedHotReloads = _failedHotReloads,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Calculate averages safely
            if (_successfulLoads > 0)
            {
                lock (_syncLock)
                {
                    snapshot.AverageAgentsPerLoad = _totalAgentsLoaded / _successfulLoads;
                    snapshot.AverageCapabilitiesPerLoad = _totalCapabilitiesLoaded / _successfulLoads;
                }
            }

            return snapshot;
        }

        public void Reset()
        {
            _totalFileLoads = 0;
            _failedFileLoads = 0;
            _cacheHits = 0;
            _cacheMisses = 0;
            _totalValidations = 0;
            _failedValidations = 0;
            _totalValidationErrors = 0;
            _totalHotReloads = 0;
            _failedHotReloads = 0;
            _totalAgentsLoaded = 0;
            _totalCapabilitiesLoaded = 0;
            _successfulLoads = 0;
            _fileMetrics.Clear();
        }

        public void Dispose()
        {
            // Nothing to dispose in this implementation
        }

        private class FileMetrics
        {
            private long _loadCount;
            private long _failedLoads;
            private long _cacheHits;
            private long _validationCount;
            private long _failedValidations;
            private long _totalValidationErrors;
            private long _hotReloadCount;
            private long _failedHotReloads;
            private long _totalAgentsLoaded;
            private long _totalCapabilitiesLoaded;
            private readonly Stopwatch _totalLoadTime = new();
            private readonly Stopwatch _totalValidationTime = new();
            private readonly Stopwatch _totalHotReloadTime = new();

            public void RecordLoad(TimeSpan duration, bool success, bool fromCache)
            {
                Interlocked.Increment(ref _loadCount);
                if (!success) Interlocked.Increment(ref _failedLoads);
                if (fromCache) Interlocked.Increment(ref _cacheHits);
                _totalLoadTime.Add(duration);
            }

            public void RecordValidation(TimeSpan duration, bool success, int errorCount)
            {
                Interlocked.Increment(ref _validationCount);
                if (!success)
                {
                    Interlocked.Increment(ref _failedValidations);
                    Interlocked.Add(ref _totalValidationErrors, errorCount);
                }
                _totalValidationTime.Add(duration);
            }

            public void RecordHotReload(TimeSpan duration, bool success, int agentCount, int capabilityCount)
            {
                Interlocked.Increment(ref _hotReloadCount);
                if (!success)
                {
                    Interlocked.Increment(ref _failedHotReloads);
                }
                else
                {
                    Interlocked.Add(ref _totalAgentsLoaded, agentCount);
                    Interlocked.Add(ref _totalCapabilitiesLoaded, capabilityCount);
                }
                _totalHotReloadTime.Add(duration);
            }
        }
    }

    internal static class StopwatchExtensions
    {
        public static void Add(this Stopwatch stopwatch, TimeSpan duration)
        {
            if (stopwatch == null) return;
            
            // This is a simplified approach - in a real implementation, you might need to handle
            // overflow cases or use a different approach for thread-safety
            if (stopwatch.IsRunning) stopwatch.Stop();
            stopwatch.Elapsed.Add(duration);
        }
    }
}
