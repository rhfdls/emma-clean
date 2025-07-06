// SPRINT1: FUB-specific mapper implementing ICrmMapper
using Emma.Core.Interfaces.Crm;

namespace Emma.Crm.Integrations.Fub
{
    public class FubCrmMapper : ICrmMapper
    {
        public CrmContactDto MapToContact(object externalContact)
        {
            // TODO: Map FUB contact object to CrmContactDto
            return new CrmContactDto();
        }

        public CrmInteractionDto MapToInteraction(object externalInteraction)
        {
            // TODO: Map FUB interaction object to CrmInteractionDto
            return new CrmInteractionDto();
        }
    }
}
