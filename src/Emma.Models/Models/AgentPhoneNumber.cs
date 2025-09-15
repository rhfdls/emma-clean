namespace Emma.Models.Models
{
    public class AgentPhoneNumber
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public int AgentId { get; set; }
        // Add additional properties as needed
    }
}
