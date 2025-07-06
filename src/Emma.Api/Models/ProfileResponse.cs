namespace Emma.Api.Models
{
    // SPRINT1: DTO for dashboard/profile rehydration
    public class ProfileResponse
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string PlanKey { get; set; } = string.Empty;
        public string PlanLabel { get; set; } = string.Empty;
        public string PlanDescription { get; set; } = string.Empty;
        public decimal PlanPrice { get; set; }
        public int SeatCount { get; set; }
        public string OrgGuid { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
