using Emma.Data.Enums;

namespace Emma.Data.Models;

public class Message
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public required string Payload { get; init; }
    public string? AiResponse { get; set; }
    public required string BlobStorageUrl { get; init; }
    public required MessageType Type { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public required DateTime OccurredAt { get; init; }
    public required Guid InteractionId { get; init; }
    public Interaction? Interaction { get; init; }
    public Transcription? Transcription { get; init; }
    public CallMetadata? CallMetadata { get; init; }
    public List<EmmaAnalysis> EmmaAnalyses { get; init; } = new();
}
