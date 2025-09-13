using System;
using System.Threading;
using System.Threading.Tasks;

namespace Emma.Api.Services
{
    /// <summary>
    /// Reflection-based adapter that wraps a vendor-specific chat client (e.g., Azure OpenAI)
    /// without introducing a compile-time dependency.
    /// </summary>
    public class OpenAIChatClientAdapter : IChatCompletionsClient
    {
        private readonly object _innerClient;

        public OpenAIChatClientAdapter(object innerClient)
        {
            _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
        }

        public async Task<object?> GetChatCompletionsAsync(object chatOptions, CancellationToken cancellationToken = default)
        {
            if (chatOptions == null) throw new ArgumentNullException(nameof(chatOptions));

            var t = _innerClient.GetType();
            var method = t.GetMethod("GetChatCompletionsAsync", new[] { chatOptions.GetType(), typeof(CancellationToken) });
            if (method == null)
            {
                // Try generic fallbacks (signature variations)
                foreach (var m in t.GetMethods())
                {
                    if (m.Name == "GetChatCompletionsAsync")
                    {
                        method = m;
                        break;
                    }
                }
            }
            if (method == null)
            {
                throw new InvalidOperationException("The provided client does not expose a compatible GetChatCompletionsAsync method.");
            }

            var taskObj = method.Invoke(_innerClient, new object?[] { chatOptions, cancellationToken });
            if (taskObj is not Task task)
            {
                throw new InvalidOperationException("Unexpected return type from GetChatCompletionsAsync.");
            }
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
    }
}
