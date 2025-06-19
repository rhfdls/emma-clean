using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Defines metrics collection for YAML capability system.
    /// </summary>
    public interface IYamlCapabilityMetrics : IDisposable
    {
        /// <summary>
        /// Records a file load operation.
        /// </summary>
        /// <param name="filePath">Path to the loaded file.</param>
        /// <param name="duration">Duration of the load operation.</param>
        /// <param name="success">Whether the load was successful.</param>
        /// <param name="fromCache">Whether the content was served from cache.</param>
        void RecordFileLoad(string filePath, TimeSpan duration, bool success, bool fromCache);

        /// <summary>
        /// Records a YAML validation operation.
        /// </summary>
        /// <param name="filePath">Path to the validated file.</param>
        /// <param name="duration">Duration of the validation.</param>
        /// <param name="success">Whether validation succeeded.</param>
        /// <param name="errorCount">Number of validation errors, if any.</param>
        void RecordValidation(string filePath, TimeSpan duration, bool success, int errorCount = 0);

        /// <summary>
        /// Records a hot reload operation.
        /// </summary>
        /// <param name="filePath">Path to the reloaded file.</param>
        /// <param name="duration">Duration of the reload operation.</param>
        /// <param name="success">Whether the reload was successful.</param>
        /// <param name="agentCount">Number of agents loaded.</param>
        /// <param name="capabilityCount">Total number of capabilities loaded.</param>
        void RecordHotReload(string filePath, TimeSpan duration, bool success, int agentCount, int capabilityCount);

        /// <summary>
        /// Records a cache hit or miss.
        /// </summary>
        /// <param name="isHit">Whether it was a cache hit.</param>
        void RecordCacheAccess(bool isHit);

        /// <summary>
        /// Gets the current metrics snapshot.
        /// </summary>
        YamlCapabilityMetricsSnapshot GetMetricsSnapshot();

        /// <summary>
        /// Resets all metrics counters.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Represents a snapshot of YAML capability metrics.
    /// </summary>
    public class YamlCapabilityMetricsSnapshot
    {
        /// <summary>
        /// Gets the total number of file load operations.
        /// </summary>
        public long TotalFileLoads { get; set; }

        /// <summary>
        /// Gets the total number of failed file load operations.
        /// </summary>
        public long FailedFileLoads { get; set; }


        /// <summary>
        /// Gets the total number of cache hits.
        /// </summary>
        public long CacheHits { get; set; }

        /// <summary>
        /// Gets the total number of cache misses.
        /// </summary>
        public long CacheMisses { get; set; }

        /// <summary>
        /// Gets the cache hit ratio (hits / (hits + misses)).
        /// </summary>
        public double CacheHitRatio => (CacheHits + CacheMisses) > 0 
            ? (double)CacheHits / (CacheHits + CacheMisses) 
            : 0;

        /// <summary>
        /// Gets the total number of validation operations.
        /// </summary>
        public long TotalValidations { get; set; }

        /// <summary>
        /// Gets the total number of failed validations.
        /// </summary>
        public long FailedValidations { get; set; }

        /// <summary>
        /// Gets the total number of validation errors across all validations.
        /// </summary>
        public long TotalValidationErrors { get; set; }

        /// <summary>
        /// Gets the average number of validation errors per failed validation.
        /// </summary>
        public double AverageValidationErrors => FailedValidations > 0 
            ? (double)TotalValidationErrors / FailedValidations 
            : 0;

        /// <summary>
        /// Gets the total number of hot reload operations.
        /// </summary>
        public long TotalHotReloads { get; set; }

        /// <summary>
        /// Gets the total number of failed hot reload operations.
        /// </summary>
        public long FailedHotReloads { get; set; }

        /// <summary>
        /// Gets the average number of agents loaded per successful load.
        /// </summary>
        public double AverageAgentsPerLoad { get; set; }

        /// <summary>
        /// Gets the average number of capabilities loaded per successful load.
        /// </summary>
        public double AverageCapabilitiesPerLoad { get; set; }

        /// <summary>
        /// Gets the timestamp when the metrics were captured.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
