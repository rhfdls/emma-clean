// SPRINT1: Generalized CRM integration interface for modular connectors
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emma.Core.Interfaces.Crm
{
    public interface ICrmIntegration
    {
        Task<bool> ValidateConnectionAsync(string apiKey);
        Task<List<CrmContactDto>> FetchContactsAsync(string apiKey);
        Task<List<CrmInteractionDto>> FetchInteractionsAsync(string apiKey, string contactId);
        // Add more as needed (sync, update, etc.)
    }
}
