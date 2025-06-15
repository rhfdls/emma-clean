using System;
using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Validation;
using Emma.Core.Services;
using Emma.Core.Services.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emma.Core.Tests.Services
{
    public class MessageServiceTests
    {
        private readonly Mock<ILogger<MessageService>> _loggerMock;
        private readonly Mock<IMessageValidator> _validatorMock;
        private readonly MessageService _messageService;

        public MessageServiceTests()
        {
            _loggerMock = new Mock<ILogger<MessageService>>();
            _validatorMock = new Mock<IMessageValidator>();
            _messageService = new MessageService(_loggerMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task CreateMessageAsync_WithNullMessage_ThrowsArgumentNullException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _messageService.CreateMessageAsync(null, userId, organizationId));
        }

        [Fact]
        public async Task CreateMessageAsync_WithInvalidMessage_ReturnsValidationErrors()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var message = new Message
            {
                Content = string.Empty, // Invalid
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow
            };

            var validationResult = new ValidationResult();
            validationResult.AddError("Content", "Message content is required.");

            _validatorMock
                .Setup(v => v.ValidateMessageAsync(It.IsAny<Message>(), It.IsAny<ValidationContext>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _messageService.CreateMessageAsync(message, userId, organizationId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Message validation failed.", result.Message);
            Assert.Single(result.Errors);
            _validatorMock.Verify(
                v => v.ValidateMessageAsync(It.IsAny<Message>(), It.IsAny<ValidationContext>()), 
                Times.Once);
        }

        [Fact]
        public async Task CreateMessageAsync_WithValidMessage_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var message = new Message
            {
                Content = "Test message",
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow
            };

            _validatorMock
                .Setup(v => v.ValidateMessageAsync(It.IsAny<Message>(), It.IsAny<ValidationContext>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            var result = await _messageService.CreateMessageAsync(message, userId, organizationId);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            _validatorMock.Verify(
                v => v.ValidateMessageAsync(It.IsAny<Message>(), It.IsAny<ValidationContext>()), 
                Times.Once);
        }

        [Fact]
        public async Task UpdateMessageAsync_WithInvalidMessage_ReturnsValidationErrors()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();
            var message = new Message
            {
                Id = Guid.NewGuid(),
                Content = string.Empty, // Invalid
                SenderId = Guid.NewGuid(),
                RecipientId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow
            };

            var validationResult = new ValidationResult();
            validationResult.AddError("Content", "Message content is required.");

            _validatorMock
                .Setup(v => v.ValidateMessageAsync(It.IsAny<Message>(), It.IsAny<ValidationContext>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _messageService.UpdateMessageAsync(message, userId, organizationId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Message validation failed.", result.Message);
            Assert.Single(result.Errors);
        }

        [Fact]
        public async Task GetMessageByIdAsync_WithEmptyId_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();

            // Act
            var result = await _messageService.GetMessageByIdAsync(Guid.Empty, userId, organizationId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Message ID is required.", result.Message);
        }

        [Fact]
        public async Task DeleteMessageAsync_WithEmptyId_ReturnsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var organizationId = Guid.NewGuid();

            // Act
            var result = await _messageService.DeleteMessageAsync(Guid.Empty, userId, organizationId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Message ID is required.", result.Message);
        }
    }
}
