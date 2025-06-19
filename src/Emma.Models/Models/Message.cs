using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Emma.Models.Enums;

namespace Emma.Models.Models;

/// <summary>
/// Represents a message in the system, which can be part of an interaction.
/// Messages can be of different types (e.g., email, SMS, chat, etc.) and may contain
/// various forms of content including text, attachments, and metadata.
/// </summary>
public class Message : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user who sent or received this message.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the user associated with this message.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
    
    /// <summary>
    /// Gets or sets the raw payload of the message.
    /// </summary>
    [Required]
    [Column(TypeName = "text")]
    public string Payload { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the AI-generated response for this message, if any.
    /// </summary>
    [Column(TypeName = "text")]
    public string? AiResponse { get; set; }
    
    /// <summary>
    /// Gets or sets the URL to the message content in blob storage, if applicable.
    /// </summary>
    [MaxLength(1000)]
    public string? BlobStorageUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the message (e.g., Email, SMS, Chat, etc.).
    /// </summary>
    [Required]
    public MessageType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the message was sent or received.
    /// This is different from CreatedAt which is when the record was created in the database.
    /// </summary>
    [Required]
    public DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the interaction this message belongs to.
    /// </summary>
    [Required]
    public Guid InteractionId { get; set; }
    
    /// <summary>
    /// Gets or sets the interaction this message is part of.
    /// </summary>
    [ForeignKey(nameof(InteractionId))]
    public virtual Interaction? Interaction { get; set; }
    
    /// <summary>
    /// Gets or sets the transcription of this message, if available.
    /// </summary>
    public virtual Transcription? Transcription { get; set; }
    
    /// <summary>
    /// Gets or sets the call metadata associated with this message, if it's a call.
    /// </summary>
    public virtual CallMetadata? CallMetadata { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of analyses performed on this message by EMMA.
    /// </summary>
    public virtual ICollection<EmmaAnalysis> Analyses { get; set; } = new List<EmmaAnalysis>();
    
    /// <summary>
    /// Gets or sets the direction of the message (Inbound, Outbound, or Internal).
    /// </summary>
    public MessageDirection Direction { get; set; } = MessageDirection.Inbound;
    
    /// <summary>
    /// Gets or sets the status of the message (e.g., Sent, Delivered, Read, Failed).
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Sent";
    
    /// <summary>
    /// Gets or sets any error information if the message failed to send or process.
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorDetails { get; set; }
    
    /// <summary>
    /// Gets or sets any additional metadata for the message in JSON format.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}
