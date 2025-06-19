using System;
using System.Collections.Generic;

namespace Emma.Core.Models.Communication
{
    /// <summary>
    /// Represents an SMS message.
    /// </summary>
    public class SmsMessage : Message
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsMessage"/> class.
        /// </summary>
        public SmsMessage()
        {
            MessageType = MessageType.Sms;
            MediaUrls = new List<string>();
        }

        /// <summary>
        /// Gets or sets the phone number of the sender.
        /// </summary>
        public string FromNumber { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the recipient.
        /// </summary>
        public string ToNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the SMS is a flash message.
        /// </summary>
        public bool IsFlash { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether delivery confirmation is requested.
        /// </summary>
        public bool RequestDeliveryConfirmation { get; set; }

        /// <summary>
        /// Gets or sets the time when the message was delivered, if delivery confirmation was requested.
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Gets or sets the status callback URL for delivery notifications.
        /// </summary>
        public string StatusCallbackUrl { get; set; }

        /// <summary>
        /// Gets or sets the number of segments the message was split into.
        /// </summary>
        public int SegmentCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the character encoding used for the message.
        /// </summary>
        public string Encoding { get; set; } = "GSM";

        /// <summary>
        /// Gets or sets the list of media URLs to include in the MMS message.
        /// </summary>
        public ICollection<string> MediaUrls { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message in the external SMS service.
        /// </summary>
        public string ExternalMessageId { get; set; }

        /// <summary>
        /// Gets or sets the error code if message delivery failed.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error message if message delivery failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the price charged for the message, if applicable.
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the currency code for the price.
        /// </summary>
        public string PriceCurrency { get; set; } = "USD";

        /// <summary>
        /// Gets or sets the metadata specific to the SMS message.
        /// </summary>
        public new SmsMetadata Metadata { get; set; } = new SmsMetadata();
    }

    /// <summary>
    /// Represents metadata specific to an SMS message.
    /// </summary>
    public class SmsMetadata : MessageMetadata
    {
        /// <summary>
        /// Gets or sets the SMS service provider used to send the message.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Gets or sets the ID of the SMS campaign, if any.
        /// </summary>
        public string CampaignId { get; set; }

        /// <summary>
        /// Gets or sets the type of the message (SMS or MMS).
        /// </summary>
        public SmsMessageType MessageType { get; set; } = SmsMessageType.Sms;

        /// <summary>
        /// Gets or sets the status of the message in the SMS service.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was sent by the carrier.
        /// </summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the message was received by the carrier.
        /// </summary>
        public DateTime? ReceivedAt { get; set; }

        /// <summary>
        /// Gets or sets the carrier that delivered the message.
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Gets or sets the country code of the recipient's phone number.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the region code of the recipient's phone number.
        /// </summary>
        public string RegionCode { get; set; }
    }

    /// <summary>
    /// Represents the type of SMS message.
    /// </summary>
    public enum SmsMessageType
    {
        /// <summary>
        /// Standard SMS message (text only).
        /// </summary>
        Sms,


        /// <summary>
        /// Multimedia Messaging Service (MMS) message.
        /// </summary>
        Mms
    }
}
