using Emma.Data.Enums;

namespace Emma.Data.Models;

public class Transcription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BlobStorageUrl { get; set; } = string.Empty;
    public TranscriptionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }
}
