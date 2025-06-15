using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Validation;

namespace Emma.Core.Services.Validation
{
    /// <summary>
    /// Defines the contract for validating message entities and their related data.
    /// </summary>
    public interface IMessageValidator
    {
        /// <summary>
        /// Validates a message entity asynchronously.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <param name="context">The validation context.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
        Task<ValidationResult> ValidateMessageAsync(Message message, ValidationContext context);

        /// <summary>
        /// Validates message metadata asynchronously.
        /// </summary>
        /// <param name="metadata">The message metadata to validate.</param>
        /// <param name="context">The validation context.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
        Task<ValidationResult> ValidateMessageMetadataAsync(MessageMetadata metadata, ValidationContext context);

        /// <summary>
        /// Validates call metadata asynchronously.
        /// </summary>
        /// <param name="callMetadata">The call metadata to validate.</param>
        /// <param name="context">The validation context.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
        Task<ValidationResult> ValidateCallMetadataAsync(CallMetadata callMetadata, ValidationContext context);

        /// <summary>
        /// Validates an email message asynchronously.
        /// </summary>
        /// <param name="emailMessage">The email message to validate.</param>
        /// <param name="context">The validation context.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
        Task<ValidationResult> ValidateEmailMessageAsync(EmailMessage emailMessage, ValidationContext context);

        /// <summary>
        /// Validates an SMS message asynchronously.
        /// </summary>
        /// <param name="smsMessage">The SMS message to validate.</param>
        /// <param name="context">The validation context.</param>
        /// <returns>A task that represents the asynchronous validation operation. The task result contains the validation result.</returns>
        Task<ValidationResult> ValidateSmsMessageAsync(SmsMessage smsMessage, ValidationContext context);
    }
}
