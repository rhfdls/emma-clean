using Emma.Models.Models;

namespace Emma.Core.Extensions
{
    /// <summary>
    /// Extension methods for ContactAssignment entity to support compliance and disclosure functionality
    /// </summary>
    public static class ContactAssignmentExtensions
    {
        /// <summary>
        /// Checks if compliance requirements are complete for this assignment
        /// </summary>
        public static bool IsComplianceComplete(this ContactAssignment assignment)
        {
            return assignment.ReferralDisclosureProvided && 
                   assignment.LiabilityDisclaimerAcknowledged;
        }

        /// <summary>
        /// Marks that referral disclosure has been provided to the client
        /// </summary>
        public static void MarkReferralDisclosureProvided(this ContactAssignment assignment)
        {
            assignment.ReferralDisclosureProvided = true;
            assignment.ReferralDisclosureDate = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records that liability disclaimer has been acknowledged by the client
        /// </summary>
        public static void AcknowledgeLiabilityDisclaimer(this ContactAssignment assignment)
        {
            assignment.LiabilityDisclaimerAcknowledged = true;
            assignment.LiabilityDisclaimerDate = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
        }
    }
}
