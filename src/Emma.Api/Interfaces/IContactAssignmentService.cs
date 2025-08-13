using System.Threading.Tasks;

namespace Emma.Api.Interfaces
{
    public interface IContactAssignmentService
    {
        Task<bool> AssignContactAsync(int contactId, int userId, int assignedByAgentId, string sourceContext, string traceId);
    }
}
