using Emma.Core.Models;

namespace Emma.Core.Interfaces
{
    /// <summary>
    /// Service for extracting SQL data and converting it to role-based JSON context objects for AI consumption.
    /// Implements security filtering and privacy controls per user permissions.
    /// </summary>
    public interface ISqlContextExtractor
    {
        /// <summary>
        /// Extracts role-based SQL context for a contact, applying security filters
        /// </summary>
        /// <param name="contactId">Contact identifier</param>
        /// <param name="requestingAgentId">Agent making the request (for permission filtering)</param>
        /// <param name="requestingRole">Role of the requesting user (Agent, Admin, AIWorkflow)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Role-specific structured JSON context object</returns>
        Task<SqlContextData> ExtractContextAsync(
            Guid contactId,
            Guid requestingAgentId,
            UserRole requestingRole = UserRole.Agent,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes context data to JSON string with appropriate formatting
        /// </summary>
        /// <param name="contextData">Context data to serialize</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>JSON string representation</returns>
        Task<string> SerializeContextAsync(
            SqlContextData contextData,
            CancellationToken cancellationToken = default);
    }
}
