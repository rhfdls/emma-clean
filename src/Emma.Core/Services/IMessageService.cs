using System;
using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Results;

namespace Emma.Core.Services
{
    /// <summary>
    /// Defines the contract for message-related operations in the EMMA platform.
    /// Handles both user-to-user and agent-to-user messaging with proper access control.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Creates a new message asynchronously.
        /// </summary>
        /// <param name="message">The message to create. Must include SenderId and RecipientId.</param>
        /// <param name="requestingUserId">The ID of the user or agent initiating the request.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created message.</returns>
        /// <remarks>
        /// The sender must be either the requesting user or an agent owned by the requesting user.
        /// The recipient must be a user or agent in the same organization.
        /// </remarks>
        Task<ServiceResult<Message>> CreateMessageAsync(Message message, Guid requestingUserId, Guid organizationId);

        /// <summary>
        /// Retrieves a message by its ID asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message to retrieve.</param>
        /// <param name="requestingUserId">The ID of the user or agent requesting the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the message if found.</returns>
        /// <remarks>
        /// The requesting user must be either the sender, recipient, or have appropriate permissions.
        /// </remarks>
        Task<ServiceResult<Message>> GetMessageByIdAsync(Guid messageId, Guid requestingUserId, Guid organizationId);

        /// <summary>
        /// Updates an existing message asynchronously.
        /// </summary>
        /// <param name="message">The updated message data.</param>
        /// <param name="requestingUserId">The ID of the user or agent updating the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates success or failure.</returns>
        /// <remarks>
        /// Only the original sender or an admin can update a message.
        /// Some message properties may be immutable after creation.
        /// </remarks>
        Task<ServiceResult> UpdateMessageAsync(Message message, Guid requestingUserId, Guid organizationId);

        /// <summary>
        /// Deletes a message asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="requestingUserId">The ID of the user or agent deleting the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates success or failure.</returns>
        /// <remarks>
        /// Messages are soft-deleted by default and can be restored within a configurable retention period.
        /// Only the original sender, recipient, or an admin can delete a message.
        /// </remarks>
        Task<ServiceResult> DeleteMessageAsync(Guid messageId, Guid requestingUserId, Guid organizationId);
    }
}
