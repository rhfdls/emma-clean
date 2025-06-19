using System;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Validation;
using Emma.Core.Services.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Services.Validation
{
    public class MessageValidatorTests
    {
        private readonly Mock<ILogger<MessageValidator>> _loggerMock;
        private readonly MessageValidator _validator;

        public MessageValidatorTests()
        {
            _loggerMock = new Mock<ILogger<MessageValidator>>();
            _validator = new MessageValidator(_loggerMock.Object);
        }

        [Fact]
        public async Task ValidateMessageAsync_WithNullMessage_ReturnsError()
        {
            // Arrange
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateMessageAsync(null, context);

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Message cannot be null.", result.Errors[0].ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task ValidateMessageAsync_WithInvalidContent_ReturnsError(string content)
        {
            // Arrange
            var message = new Message
            {
                Content = content,
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow
            };
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateMessageAsync(message, context);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Content" && e.ErrorMessage == "Message content is required.");
        }

        [Fact]
        public async Task ValidateEmailMessageAsync_WithValidEmail_ReturnsValidResult()
        {
            // Arrange
            var email = new EmailMessage
            {
                Content = "Test email content",
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow,
                Subject = "Test Subject",
                From = "test@example.com",
                To = new[] { "recipient@example.com" }
            };
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateEmailMessageAsync(email, context);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("invalid-email")]
        [InlineData("test@")]
        [InlineData("@example.com")]
        public async Task ValidateEmailMessageAsync_WithInvalidFromEmail_ReturnsError(string fromEmail)
        {
            // Arrange
            var email = new EmailMessage
            {
                Content = "Test email content",
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow,
                Subject = "Test Subject",
                From = fromEmail,
                To = new[] { "recipient@example.com" }
            };
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateEmailMessageAsync(email, context);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "From");
        }


        [Fact]
        public async Task ValidateSmsMessageAsync_WithValidSms_ReturnsValidResult()
        {
            // Arrange
            var sms = new SmsMessage
            {
                Content = "Test SMS content",
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow,
                PhoneNumber = "+1234567890"
            };
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateSmsMessageAsync(sms, context);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }


        [Fact]
        public async Task ValidateCallMessageAsync_WithValidCall_ReturnsValidResult()
        {
            // Arrange
            var call = new CallMessage
            {
                Content = "Test call content",
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow,
                CallMetadata = new CallMetadata
                {
                    Duration = TimeSpan.FromMinutes(5),
                    WasRecorded = true,
                    RecordingUrl = "https://example.com/recording.mp3"
                }
            };
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateMessageAsync(call, context);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateMessageMetadataAsync_WithInvalidImportance_ReturnsError()
        {
            // Arrange
            var metadata = new MessageMetadata
            {
                Importance = 11 // Invalid, should be between 0 and 10
            };
            var context = new ValidationContext();

            // Act
            var result = await _validator.ValidateMessageMetadataAsync(metadata, context);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Importance");
        }
    }
}
