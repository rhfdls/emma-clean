using System;
using System.Collections.Generic;

namespace Emma.Core.Models.InteractionContext
{
    /// <summary>
    /// Represents tenant/organization-specific context for interactions.
    /// </summary>
    public class TenantContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the tenant/organization.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the name of the tenant/organization.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the industry profile for this tenant.
        /// </summary>
        public IndustryProfile IndustryProfile { get; set; } = new();
        
        /// <summary>
        /// Gets or sets the collection of enabled features for this tenant.
        /// </summary>
        public ICollection<string> EnabledFeatures { get; set; } = new HashSet<string>();
        
        /// <summary>
        /// Gets or sets the timezone for this tenant.
        /// </summary>
        public string TimeZone { get; set; } = "UTC";
        
        /// <summary>
        /// Gets or sets the default language for this tenant.
        /// </summary>
        public string DefaultLanguage { get; set; } = "en-US";
        
        /// <summary>
        /// Gets or sets the audit ID for tracking purposes.
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the reason for the last update.
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the tenant context.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    
    /// <summary>
    /// Represents industry-specific settings and configurations.
    /// </summary>
    public class IndustryProfile
    {
        /// <summary>
        /// Gets or sets the industry name (e.g., "RealEstate", "Banking").
        /// </summary>
        public string Name { get; set; } = "Default";
        
        /// <summary>
        /// Gets or sets the industry-specific settings.
        /// </summary>
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}
