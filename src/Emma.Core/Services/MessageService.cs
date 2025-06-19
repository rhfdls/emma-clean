using System;
using System.Linq;
using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Results;
using Emma.Core.Models.Validation;
using Emma.Core.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emma.Core.Services
{
    /// <summary>
    /// Service for managing messages in the system.
    /// </summary>
    /// <summary>
    /// Service implementation for managing messages in the EMMA platform.
    /// Handles both user-to-user and agent-to-user messaging with proper access control.
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly ILogger<MessageService> _logger;
        private readonly IMessageValidator _messageValidator;
        private readonly IAppDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="messageValidator">The message validator.</param>
        /// <param name="dbContext">The database context.</param>
        public MessageService(
            ILogger<MessageService> logger,
            IMessageValidator messageValidator,
            IAppDbContext dbContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageValidator = messageValidator ?? throw new ArgumentNullException(nameof(messageValidator));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Message>> CreateMessageAsync(Message message, Guid requestingUserId, Guid organizationId)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _logger.LogInformation("Creating new message of type {MessageType} from {SenderId} to {RecipientId}", 
                message.GetType().Name, message.SenderId, message.RecipientId);

            // Verify the requesting user has permission to send this message
            var senderValidation = await ValidateSenderPermissionAsync(message.SenderId, requestingUserId, organizationId);
            if (!senderValidation.IsSuccess)
            {
                return ServiceResult<Message>.Failure(senderValidation.Errors);
            }


            // Verify the recipient is valid and in the same organization
            var recipientValidation = await ValidateRecipientAsync(message.RecipientId, organizationId);
            if (!recipientValidation.IsSuccess)
            {
                return ServiceResult<Message>.Failure(recipientValidation.Errors);
            }


            // Create validation context
            var validationContext = new ValidationContext
            {
                UserId = requestingUserId,
                OrganizationId = organizationId,
                OperationType = "Create",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Set message metadata
            message.Id = Guid.NewGuid();
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            message.OrganizationId = organizationId;

            // Validate the message content and structure
            var validationResult = await _messageValidator.ValidateMessageAsync(message, validationContext);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Message validation failed with {ErrorCount} errors", validationResult.Errors.Count);
                return ServiceResult<Message>.Failure("Message validation failed.", validationResult.Errors);
            }


            try
            {
                // Save the message to the database
                await _dbContext.Messages.AddAsync(message);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully created message with ID {MessageId}", message.Id);
                return ServiceResult<Message>.Success(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message");
                return ServiceResult<Message>.Failure("An error occurred while creating the message.");
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Message>> GetMessageByIdAsync(Guid messageId, Guid requestingUserId, Guid organizationId)
        {
            if (messageId == Guid.Empty)
                return ServiceResult<Message>.Failure("Message ID is required.");

            _logger.LogDebug("Retrieving message {MessageId} for user {UserId}", messageId, requestingUserId);

            try
            {
                var message = await _dbContext.Messages
                    .Include(m => m.Metadata)
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.OrganizationId == organizationId);

                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found in organization {OrganizationId}", messageId, organizationId);
                    return ServiceResult<Message>.Failure("Message not found");
                }


                // Verify the requesting user has permission to view this message
                if (message.SenderId != requestingUserId && message.RecipientId != requestingUserId)
                {
                    // Check if the requesting user is an admin or has appropriate permissions
                    var isAuthorized = await IsUserAuthorizedForMessageAsync(requestingUserId, messageId, organizationId);
                    if (!isAuthorized)
                    {
                        _logger.LogWarning("User {UserId} is not authorized to view message {MessageId}", 
                            requestingUserId, messageId);
                        return ServiceResult<Message>.Failure("Not authorized to view this message");
                    }
                }

                return ServiceResult<Message>.Success(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message {MessageId}", messageId);
                return ServiceResult<Message>.Failure("An error occurred while retrieving the message");
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult> UpdateMessageAsync(Message message, Guid requestingUserId, Guid organizationId)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            _logger.LogInformation("Updating message {MessageId}", message.Id);

            try
            {
                // Retrieve the existing message to verify ownership and permissions
                var existingMessage = await _dbContext.Messages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == message.Id && m.OrganizationId == organizationId);

                if (existingMessage == null)
                {
                    _logger.LogWarning("Message {MessageId} not found for update", message.Id);
                    return ServiceResult.Failure("Message not found");
                }


                // Verify the requesting user is the original sender or has admin rights
                if (existingMessage.SenderId != requestingUserId)
                {
                    var isAdmin = await IsUserAdminAsync(requestingUserId, organizationId);
                    if (!isAdmin)
                    {
                        _logger.LogWarning("User {UserId} is not authorized to update message {MessageId}", 
                            requestingUserId, message.Id);
                        return ServiceResult.Failure("Not authorized to update this message");
                    }
                }


                // Create validation context
                var validationContext = new ValidationContext
                {
                    UserId = requestingUserId,
                    OrganizationId = organizationId,
                    OperationType = "Update",
                    CorrelationId = Guid.NewGuid().ToString()
                };

                // Validate the message content and structure
                var validationResult = await _messageValidator.ValidateMessageAsync(message, validationContext);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Message update validation failed with {ErrorCount} errors", validationResult.Errors.Count);
                    return ServiceResult.Failure("Message validation failed.", validationResult.Errors);
                }


                // Update only the allowed fields
                existingMessage.Content = message.Content;
                existingMessage.Status = message.Status;
                existingMessage.UpdatedAt = DateTime.UtcNow;

                if (message.Metadata != null)
                {
                    existingMessage.Metadata = message.Metadata;
                }

                _dbContext.Messages.Update(existingMessage);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Message {MessageId} updated successfully", message.Id);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message {MessageId}", message.Id);
                return ServiceResult.Failure("An error occurred while updating the message");
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult> DeleteMessageAsync(Guid messageId, Guid requestingUserId, Guid organizationId)
        {
            if (messageId == Guid.Empty)
                return ServiceResult.Failure("Message ID is required.");

            _logger.LogInformation("Deleting message {MessageId}", messageId);

            try
            {
                var message = await _dbContext.Messages
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.OrganizationId == organizationId);

                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found for deletion", messageId);
                    return ServiceResult.Failure("Message not found");
                }


                // Verify the requesting user is the sender, recipient, or an admin
                if (message.SenderId != requestingUserId && message.RecipientId != requestingUserId)
                {
                    var isAdmin = await IsUserAdminAsync(requestingUserId, organizationId);
                    if (!isAdmin)
                    {
                        _logger.LogWarning("User {UserId} is not authorized to delete message {MessageId}", 
                            requestingUserId, messageId);
                        return ServiceResult.Failure("Not authorized to delete this message");
                    }
                }


                // Soft delete the message
                message.DeletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Message {MessageId} soft-deleted successfully", messageId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return ServiceResult.Failure("An error occurred while deleting the message");
            }
        }

        #region Private Helper Methods

        private async Task<ServiceResult> ValidateSenderPermissionAsync(Guid senderId, Guid requestingUserId, Guid organizationId)
        {
            // The sender must be either the requesting user or an agent owned by the requesting user
            if (senderId == requestingUserId)
            {
                return ServiceResult.Success();
            }

            // Check if the sender is an agent owned by the requesting user
            var isOwnedAgent = await _dbContext.Agents
                .AnyAsync(a => a.Id == senderId && 
                             a.OwnerId == requestingUserId && 
                             a.OrganizationId == organizationId);

            if (!isOwnedAgent)
            {
                _logger.LogWarning("User {UserId} is not authorized to send messages as {SenderId}", 
                    requestingUserId, senderId);
                return ServiceResult.Failure("Not authorized to send messages as the specified sender");
            }

            return ServiceResult.Success();
        }

        private async Task<ServiceResult> ValidateRecipientAsync(Guid recipientId, Guid organizationId)
        {
            // Check if recipient exists and is in the same organization
            var recipientExists = await _dbContext.Users.AnyAsync(u => u.Id == recipientId && u.OrganizationId == organizationId) ||
                                await _dbContext.Agents.AnyAsync(a => a.Id == recipientId && a.OrganizationId == organizationId);

            if (!recipientExists)
            {
                _logger.LogWarning("Recipient {RecipientId} not found in organization {OrganizationId}", 
                    recipientId, organizationId);
                return ServiceResult.Failure("Recipient not found or not in the same organization");
            }

            return ServiceResult.Success();
        }

        private async Task<bool> IsUserAuthorizedForMessageAsync(Guid userId, Guid messageId, Guid organizationId)
        {
            // Check if user is an admin in the organization
            var isAdmin = await _dbContext.OrganizationUsers
                .AnyAsync(ou => ou.UserId == userId && 
                               ou.OrganizationId == organizationId && 
                               ou.Role == UserRole.Admin);

            if (isAdmin)
            {
                return true;
            }

            // Check if user is a manager with message access
            var hasMessageAccess = await _dbContext.MessageAccessGrants
                .AnyAsync(mag => mag.MessageId == messageId && 
                               mag.GrantedToUserId == userId && 
                               mag.ExpiresAt > DateTime.UtcNow);

            return hasMessageAccess;
        }

        private async Task<bool> IsUserAdminAsync(Guid userId, Guid organizationId)
        {
            return await _dbContext.OrganizationUsers
                .AnyAsync(ou => ou.UserId == userId && 
                               ou.OrganizationId == organizationId && 
                               ou.Role == UserRole.Admin);
        }

        #endregion
    }
}
