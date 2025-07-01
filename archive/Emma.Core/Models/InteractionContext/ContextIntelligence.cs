using System;
using System.Collections.Generic;

namespace Emma.Core.Models.InteractionContext
{
    /// <summary>
    /// Represents intelligence and insights derived from an interaction.
    /// </summary>
    public class ContextIntelligence
    {
        /// <summary>
        /// Gets or sets the unique identifier for this intelligence record.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the interaction ID this intelligence is associated with.
        /// </summary>
        public Guid InteractionId { get; set; }
        
        /// <summary>
        /// Gets or sets the sentiment analysis result (-1.0 to 1.0).
        /// </summary>
        public double Sentiment { get; set; }
        
        /// <summary>
        /// Gets or sets the confidence score (0.0 to 1.0) of the analysis.
        /// </summary>
        public double Confidence { get; set; } = 1.0;
        
        /// <summary>
        /// Gets or sets the collection of detected buying signals.
        /// </summary>
        public ICollection<string> BuyingSignals { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the collection of recommendations.
        /// </summary>
        public ICollection<string> Recommendations { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the collection of key insights.
        /// </summary>
        public ICollection<string> Insights { get; set; } = new List<string>();
        
        /// <summary>
        /// Gets or sets the timestamp when this intelligence was generated.
        /// </summary>
        public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Gets or sets the audit ID for tracking purposes.
        /// </summary>
        public Guid AuditId { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the reason for generating this intelligence.
        /// </summary>
        public string? Reason { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata for the intelligence.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
