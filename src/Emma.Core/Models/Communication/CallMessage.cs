using System;
using System.Collections.Generic;

namespace Emma.Core.Models.Communication
{
    /// <summary>
    /// Represents a phone call message.
    /// </summary>
    public class CallMessage : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallMessage"/> class.
        /// </summary>
        public CallMessage()
        {
            MessageType = MessageType.Call;
        }

        /// <summary>
        /// Gets or sets the phone number of the caller.
        /// </summary>
        public string FromNumber { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the callee.
        /// </summary>
        public string ToNumber { get; set; }

        /// <summary>
        /// Gets or sets the direction of the call.
        /// </summary>
        public CallDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets the status of the call.
        /// </summary>
        public CallStatus Status { get; set; } = CallStatus.Initiated;

        /// <summary>
        /// Gets or sets the date and time when the call started.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the call ended.
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Gets or sets the duration of the call.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Gets or sets the ID of the call in the external telephony service.
        /// </summary>
        public string ExternalCallId { get; set; }

        /// <summary>
        /// Gets or sets the SIP call ID, if applicable.
        /// </summary>
        public string SipCallId { get; set; }

        /// <summary>
        /// Gets or sets the call recording URL, if the call was recorded.
        /// </summary>
        public string RecordingUrl { get; set; }

        /// <summary>
        /// Gets or sets the transcription of the call, if available.
        /// </summary>
        public string Transcription { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the transcription, if available.
        /// </summary>
        public float? TranscriptionConfidence { get; set; }

        /// <summary>
        /// Gets or sets the language of the call.
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets the price of the call, if applicable.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency code for the price.
        /// </summary>
        public string PriceCurrency { get; set; } = "USD";

        /// <summary>
        /// Gets or sets the call metadata.
        /// </summary>
        public CallMetadata CallMetadata { get; set; } = new CallMetadata();

        /// <summary>
        /// Gets or sets the metadata specific to the call message.
        /// </summary>
        public new CallMessageMetadata Metadata { get; set; } = new CallMessageMetadata();
    }

    /// <summary>
    /// Represents the direction of a call.
    /// </summary>
    public enum CallDirection
    {
        /// <summary>
        /// Incoming call.
        /// </summary>
        Inbound,

        /// <summary>
        /// Outgoing call.
        /// </summary>
        Outbound
    }

    /// <summary>
    /// Represents the status of a call.
    /// </summary>
    public enum CallStatus
    {
        /// <summary>
        /// Call has been initiated but not yet connected.
        /// </summary>
        Initiated,

        /// <summary>
        /// Call is ringing.
        /// </summary>
        Ringing,

        
        /// <summary>
        /// Call is in progress.
        /// </summary>
        InProgress,

        
        /// <summary>
        /// Call was completed successfully.
        /// </summary>
        Completed,
        
        /// <summary>
        /// Call was busy.
        /// </summary>
        Busy,
        
        /// <summary>
        /// Call was not answered.
        /// </summary>
        NoAnswer,
        
        /// <summary>
        /// Call failed.
        /// </summary>
        Failed,
        
        /// <summary>
        /// Call was cancelled.
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Represents metadata specific to a call.
    /// </summary>
    public class CallMetadata
    {
        /// <summary>
        /// Gets or sets the SIP status code of the call.
        /// </summary>
        public int? SipResponseCode { get; set; }

        /// <summary>
        /// Gets or sets the SIP response text of the call.
        /// </summary>
        public string SipResponseText { get; set; }

        /// <summary>
        /// Gets or sets the caller's carrier.
        /// </summary>
        public string FromCarrier { get; set; }

        /// <summary>
        /// Gets or sets the callee's carrier.
        /// </summary>
        public string ToCarrier { get; set; }

        /// <summary>
        /// Gets or sets the caller's country code.
        /// </summary>
        public string FromCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the callee's country code.
        /// </summary>
        public string ToCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the caller's region code.
        /// </summary>
        public string FromRegion { get; set; }

        /// <summary>
        /// Gets or sets the callee's region code.
        /// </summary>
        public string ToRegion { get; set; }

        /// <summary>
        /// Gets or sets the caller's city.
        /// </summary>
        public string FromCity { get; set; }

        /// <summary>
        /// Gets or sets the callee's city.
        /// </summary>
        public string ToCity { get; set; }

        /// <summary>
        /// Gets or sets the caller's postal code.
        /// </summary>
        public string FromPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the callee's postal code.
        /// </summary>
        public string ToPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the caller's latitude.
        /// </summary>
        public double? FromLatitude { get; set; }

        /// <summary>
        /// Gets or sets the callee's latitude.
        /// </summary>
        public double? ToLatitude { get; set; }

        /// <summary>
        /// Gets or sets the caller's longitude.
        /// </summary>
        public double? FromLongitude { get; set; }

        /// <summary>
        /// Gets or sets the callee's longitude.
        /// </summary>
        public double? ToLongitude { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the call was forwarded.
        /// </summary>
        public bool WasForwarded { get; set; }

        /// <summary>
        /// Gets or sets the forwarded from number, if the call was forwarded.
        /// </summary>
        public string ForwardedFrom { get; set; }

        /// <summary>
        /// Gets or sets the call SID from the telephony provider.
        /// </summary>
        public string ProviderCallId { get; set; }

        /// <summary>
        /// Gets or sets the account SID from the telephony provider.
        /// </summary>
        public string AccountSid { get; set; }

        /// <summary>
        /// Gets or sets the telephony provider used for the call.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the call quality metrics.
        /// </summary>
        public CallQualityMetrics QualityMetrics { get; set; } = new CallQualityMetrics();
    }

    /// <summary>
    /// Represents call quality metrics.
    /// </summary>
    public class CallQualityMetrics
    {
        /// <summary>
        /// Gets or sets the jitter in milliseconds.
        /// </summary>
        public double? JitterMs { get; set; }

        /// <summary>
        /// Gets or sets the packet loss percentage.
        /// </summary>
        public double? PacketLossPercentage { get; set; }

        /// <summary>
        /// Gets or sets the network latency in milliseconds.
        /// </summary>
        public double? LatencyMs { get; set; }

        /// <summary>
        /// Gets or sets the mean opinion score (1-5).
        /// </summary>
        public double? Mos { get; set; }

        /// <summary>
        /// Gets or sets the audio quality (1-5).
        /// </summary>
        public int? AudioQuality { get; set; }

        /// <summary>
        /// Gets or sets the call quality issues encountered.
        /// </summary>
        public ICollection<string> Issues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents metadata specific to a call message.
    /// </summary>
    public class CallMessageMetadata : MessageMetadata
    {
        /// <summary>
        /// Gets or sets the call type (e.g., voice, video).
        /// </summary>
        public string CallType { get; set; } = "voice";

        /// <summary>
        /// Gets or sets the call tags for categorization.
        /// </summary>
        public ICollection<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the call outcome (e.g., successful, voicemail, busy).
        /// </summary>
        public string Outcome { get; set; }

        /// <summary>
        /// Gets or sets the call disposition (e.g., answered, no-answer, busy, failed).
        /// </summary>
        public string Disposition { get; set; }

        /// <summary>
        /// Gets or sets the call duration bucket (e.g., "0-30s", "30s-1m", "1m-5m", "5m+").
        /// </summary>
        public string DurationBucket { get; set; }

        /// <summary>
        /// Gets or sets the call direction (inbound/outbound).
        /// </summary>
        public string Direction { get; set; }

        /// <summary>
        /// Gets or sets the call start time in the local timezone.
        /// </summary>
        public DateTime? LocalStartTime { get; set; }

        /// <summary>
        /// Gets or sets the call end time in the local timezone.
        /// </summary>
        public DateTime? LocalEndTime { get; set; }

        /// <summary>
        /// Gets or sets the timezone ID for local times.
        /// </summary>
        public string TimezoneId { get; set; } = "UTC";
    }
}
