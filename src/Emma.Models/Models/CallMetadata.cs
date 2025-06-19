using Emma.Models.Enums;

namespace Emma.Models.Models;

public class CallMetadata
{
    public required Guid MessageId { get; init; }
    public required string ClientPhoneNumber { get; init; }
    public required int DurationInSeconds { get; init; }
    public required CallDirection DirectionBasedOnAgent { get; init; }
    public required CallStatus Status { get; init; }
    public required string ReferenceId { get; init; }
    public Message? Message { get; init; }
}
