using System;
using System.Threading.Tasks;
using Emma.Core.Models.Communication;
using Emma.Core.Models.Results;

namespace Emma.Core.Services
{
    /// <summary>
    /// Defines the contract for message-related operations.
    /// </summary>
    public interface IMessageService
    {
        /// <summary>
        /// Creates a new message asynchronously.
        /// </summary>
        /// <param name="message">The message to create.</param>
        /// <param name="userId">The ID of the user creating the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created message.</returns>
        Task<ServiceResult<Message>> CreateMessageAsync(Message message, Guid userId, Guid organizationId);

        /// <summary>
        /// Retrieves a message by its ID asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message to retrieve.</param>
        /// <param name="userId">The ID of the user requesting the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the message if found.</returns>
        Task<ServiceResult<Message>> GetMessageByIdAsync(Guid messageId, Guid userId, Guid organizationId);

        /// <summary>
        /// Updates an existing message asynchronously.
        /// </summary>
        /// <param name="message">The updated message data.</param>
        /// <param name="userId">The ID of the user updating the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates success or failure.</returns>
        Task<ServiceResult> UpdateMessageAsync(Message message, Guid userId, Guid organizationId);

        /// <summary>
        /// Deletes a message asynchronously.
        /// </summary>
        /// <param name="messageId">The ID of the message to delete.</param>
        /// <param name="userId">The ID of the user deleting the message.</param>
        /// <param name="organizationId">The ID of the organization the message belongs to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates success or failure.</returns>
        Task<ServiceResult> DeleteMessageAsync(Guid messageId, Guid userId, Guid organizationId);
    }
}
