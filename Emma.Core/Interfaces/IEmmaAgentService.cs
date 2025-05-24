using System.Threading;
using System.Threading.Tasks;
using Emma.Core.Dtos;

namespace Emma.Core.Interfaces;

public interface IEmmaAgentService
{
    /// <summary>
    /// Processes a message and returns an AI-generated response with actions.
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response DTO.</returns>
    Task<EmmaResponseDto> ProcessMessageAsync(string message, CancellationToken cancellationToken = default);
}
