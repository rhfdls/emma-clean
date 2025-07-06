// SPRINT1: Interface for mapping CRM data to EMMA DTOs
namespace Emma.Core.Interfaces.Crm
{
    public interface ICrmMapper
    {
        CrmContactDto MapToContact(object externalContact);
        CrmInteractionDto MapToInteraction(object externalInteraction);
    }
}
