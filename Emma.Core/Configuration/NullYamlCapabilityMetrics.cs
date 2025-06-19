using System;

namespace Emma.Core.Configuration
{
    /// <summary>
    /// Null object pattern implementation of IYamlCapabilityMetrics that does nothing.
    /// </summary>
    public class NullYamlCapabilityMetrics : IYamlCapabilityMetrics
    {
        /// <inheritdoc />
        public void RecordFileLoad(string filePath, TimeSpan duration, bool success, bool fromCache)
        {
            // No-op
        }

        /// <inheritdoc />
        public void RecordValidation(string filePath, TimeSpan duration, bool success, int errorCount = 0)
        {
            // No-op
        }

        /// <inheritdoc />
        public void RecordHotReload(string filePath, TimeSpan duration, bool success, int agentCount, int capabilityCount)
        {
            // No-op
        }

        /// <inheritdoc />
        public void RecordCacheAccess(bool isHit)
        {
            // No-op
        }

        /// <inheritdoc />
        public YamlCapabilityMetricsSnapshot GetMetricsSnapshot()
        {
            return new YamlCapabilityMetricsSnapshot();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // No resources to dispose
        }
    }
}
