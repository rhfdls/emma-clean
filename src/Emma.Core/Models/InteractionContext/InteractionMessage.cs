using System;

namespace Emma.Core.Models.InteractionContext
{
    /// <summary>
    /// Represents a message within an interaction.
    /// </summary>
    public class InteractionMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for the message.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the ID of the sender (user or agent).
        /// </summary>
        public string SenderId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the type of the sender ("user" or "agent").
        /// </summary>
        public string SenderType { get; set; } = "user";
        
        /// <summary>
        /// Gets or sets the display name of the sender.
        /// </summary>
        public string SenderName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the timestamp when the message was sent.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Gets or sets the type of the message (e.g., "text", "image", "file").
        /// </summary>
        public string MessageType { get; set; } = "text";
        
        /// <summary>
        /// Gets or sets additional metadata for the message.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
