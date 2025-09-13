using System.Threading;
using System.Threading.Tasks;

namespace Emma.Api.Services
{
    /// <summary>
    /// Minimal abstraction over a chat completions client to decouple from any specific SDK.
    /// Implementations may wrap Azure OpenAI or any other provider.
    /// </summary>
    public interface IChatCompletionsClient
    {
        /// <summary>
        /// Sends a chat completion request. Both request and response are untyped objects to avoid
        /// compile-time coupling to a vendor SDK. Implementations are responsible for mapping.
        /// </summary>
        Task<object?> GetChatCompletionsAsync(object chatOptions, CancellationToken cancellationToken = default);
    }
}
