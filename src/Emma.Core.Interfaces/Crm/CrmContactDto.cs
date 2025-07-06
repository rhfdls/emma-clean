// SPRINT1: Generic DTO for CRM contact data
namespace Emma.Core.Interfaces.Crm
{
    public class CrmContactDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ExternalSourceId { get; set; }
        // Add more fields as needed
    }
}
