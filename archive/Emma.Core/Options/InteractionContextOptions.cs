using System;

namespace Emma.Core.Options
{
    /// <summary>
    /// Configuration section name for InteractionContextOptions.
    /// </summary>
    

    /// <summary>
    /// Configuration options for InteractionContextProvider.
    /// </summary>
    public class InteractionContextOptions
    {
        /// <summary>
        /// Gets or sets the default cache expiration time.
        /// Default is 15 minutes.
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets the cache expiration time for intelligence data.
        /// Default is 5 minutes as it might be more volatile.
        /// </summary>
        public TimeSpan IntelligenceCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets a value indicating whether to enable caching.
        /// Default is true.
        /// </summary>
        public bool EnableCaching { get; set; } = true;
    }
}
