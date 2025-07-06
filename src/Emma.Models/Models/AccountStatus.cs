// SPRINT1: Account status enum for onboarding/email verification
namespace Emma.Models.Models
{
    public enum AccountStatus
    {
        PendingVerification = 0,
        Active = 1,
        Suspended = 2,
        Deleted = 3
    }
}
