using System.Threading.Tasks;

namespace Emma.Api.Interfaces
{
    public interface IInteractionService
    {
        Task<bool> LogInteractionAsync(int contactId, int agentId, object interactionDto, string sourceTraceId);
    }
}
