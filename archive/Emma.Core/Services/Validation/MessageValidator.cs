using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Validation;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services.Validation
{
    /// <summary>
    /// Validates message entities and their related data.
    /// </summary>
    public class MessageValidator : IMessageValidator
    {
        private readonly ILogger<MessageValidator> _logger;
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const int MaxSmsLength = 1600;
        private const int MaxSubjectLength = 200;
        private const int MaxEmailLength = 10000;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageValidator"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MessageValidator(ILogger<MessageValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<ValidationResult> ValidateMessageAsync(Message message, ValidationContext context)
        {
            var result = new ValidationResult();

            if (message == null)
            {
                result.AddError(nameof(message), "Message cannot be null.");
                return result;
            }

            // Common message validation
            if (string.IsNullOrWhiteSpace(message.Content))
                result.AddError(nameof(Message.Content), "Message content is required.");
            
            if (message.SentAt == default)
                result.AddError(nameof(Message.SentAt), "Sent timestamp is required.");
            
            if (message.SenderId == Guid.Empty)
                result.AddError(nameof(Message.SenderId), "Sender ID is required.");
            
            if (message.RecipientId == Guid.Empty)
                result.AddError(nameof(Message.RecipientId), "Recipient ID is required.");

            // Type-specific validation
            switch (message)
            {
                case EmailMessage email:
                    await ValidateEmailMessageAsync(email, result, context);
                    break;
                    
                case SmsMessage sms:
                    await ValidateSmsMessageAsync(sms, result, context);
                    break;
                    
                case CallMessage call:
                    await ValidateCallMessageAsync(call, result, context);
                    break;
            }

            // If we have any errors, log them
            if (!result.IsValid)
            {
                _logger.LogWarning("Message validation failed with {ErrorCount} errors: {Errors}", 
                    result.Errors.Count, 
                    string.Join("; ", result.Errors.Select(e => e.ToString())));
            }

            return result;
        }

        /// <inheritdoc />
        public Task<ValidationResult> ValidateEmailMessageAsync(EmailMessage emailMessage, ValidationContext context)
        {
            var result = new ValidationResult();
            return ValidateEmailMessageAsync(emailMessage, result, context);
        }

        private async Task<ValidationResult> ValidateEmailMessageAsync(EmailMessage email, ValidationResult result, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(email.Subject))
                result.AddError(nameof(EmailMessage.Subject), "Email subject is required.");
            else if (email.Subject.Length > MaxSubjectLength)
                result.AddError(nameof(EmailMessage.Subject), $"Email subject cannot exceed {MaxSubjectLength} characters.");

            if (string.IsNullOrWhiteSpace(email.From))
                result.AddError(nameof(EmailMessage.From), "Sender email is required.");
            else if (!IsValidEmail(email.From))
                result.AddError(nameof(EmailMessage.From), "Invalid sender email format.");

            if (email.To == null || !email.To.Any())
                result.AddError(nameof(EmailMessage.To), "At least one recipient is required.");
            else
            {
                foreach (var recipient in email.To)
                {
                    if (string.IsNullOrWhiteSpace(recipient))
                        result.AddError(nameof(EmailMessage.To), "Recipient email cannot be empty.");
                    else if (!IsValidEmail(recipient))
                        result.AddError(nameof(EmailMessage.To), $"Invalid recipient email format: {recipient}");
                }
            }

            // Validate CC and BCC if present
            if (email.Cc != null)
            {
                foreach (var cc in email.Cc.Where(cc => !string.IsNullOrWhiteSpace(cc)))
                {
                    if (!IsValidEmail(cc))
                        result.AddError(nameof(EmailMessage.Cc), $"Invalid CC email format: {cc}");
                }
            }

            if (email.Bcc != null)
            {
                foreach (var bcc in email.Bcc.Where(bcc => !string.IsNullOrWhiteSpace(bcc)))
                {
                    if (!IsValidEmail(bcc))
                        result.AddError(nameof(EmailMessage.Bcc), $"Invalid BCC email format: {bcc}");
                }
            }

            // Content length check
            if (!string.IsNullOrEmpty(email.Content) && email.Content.Length > MaxEmailLength)
                result.AddError(nameof(EmailMessage.Content), $"Email content cannot exceed {MaxEmailLength} characters.");

            // If we have metadata, validate it
            if (email.Metadata != null)
            {
                var metadataResult = await ValidateMessageMetadataAsync(email.Metadata, context);
                if (!metadataResult.IsValid)
                {
                    result.AddErrors(metadataResult.Errors);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task<ValidationResult> ValidateSmsMessageAsync(SmsMessage smsMessage, ValidationContext context)
        {
            var result = new ValidationResult();
            return ValidateSmsMessageAsync(smsMessage, result, context);
        }

        private Task<ValidationResult> ValidateSmsMessageAsync(SmsMessage sms, ValidationResult result, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(sms.PhoneNumber))
                result.AddError(nameof(SmsMessage.PhoneNumber), "Phone number is required.");
            // Additional phone number validation could be added here

            if (!string.IsNullOrEmpty(sms.Content) && sms.Content.Length > MaxSmsLength)
                result.AddError(nameof(SmsMessage.Content), $"SMS content cannot exceed {MaxSmsLength} characters.");

            return Task.FromResult(result);
        }

        private async Task<ValidationResult> ValidateCallMessageAsync(CallMessage call, ValidationResult result, ValidationContext context)
        {
            if (call.CallMetadata == null)
            {
                result.AddError(nameof(CallMessage.CallMetadata), "Call metadata is required.");
                return result;
            }

            var metadataResult = await ValidateCallMetadataAsync(call.CallMetadata, context);
            if (!metadataResult.IsValid)
            {
                result.AddErrors(metadataResult.Errors);
            }

            return result;
        }

        /// <inheritdoc />
        public Task<ValidationResult> ValidateMessageMetadataAsync(MessageMetadata metadata, ValidationContext context)
        {
            var result = new ValidationResult();

            if (metadata == null)
            {
                result.AddError(nameof(metadata), "Message metadata cannot be null.");
                return Task.FromResult(result);
            }


            // Add any metadata-specific validation here
            if (metadata.Importance < 0 || metadata.Importance > 10)
                result.AddError(nameof(MessageMetadata.Importance), "Importance must be between 0 and 10.");

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<ValidationResult> ValidateCallMetadataAsync(CallMetadata callMetadata, ValidationContext context)
        {
            var result = new ValidationResult();

            if (callMetadata == null)
            {
                result.AddError(nameof(callMetadata), "Call metadata cannot be null.");
                return Task.FromResult(result);
            }

            if (callMetadata.Duration < TimeSpan.Zero)
                result.AddError(nameof(CallMetadata.Duration), "Call duration cannot be negative.");

            if (callMetadata.WasRecorded && string.IsNullOrEmpty(callMetadata.RecordingUrl))
                result.AddError(nameof(CallMetadata.RecordingUrl), "Recording URL is required for recorded calls.");

            return Task.FromResult(result);
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return EmailRegex.IsMatch(email);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
