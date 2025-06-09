using Emma.Data.Models;

namespace Emma.Core.Extensions
{
    /// <summary>
    /// Extension methods for Contact entity to support resource management functionality
    /// </summary>
    public static class ContactExtensions
    {
        /// <summary>
        /// Gets the primary email address for the contact
        /// </summary>
        public static string? Email(this Contact contact)
        {
            return contact.Emails?.FirstOrDefault()?.Address;
        }

        /// <summary>
        /// Gets the primary phone number for the contact
        /// </summary>
        public static string? Phone(this Contact contact)
        {
            return contact.Phones?.FirstOrDefault()?.Number;
        }

        /// <summary>
        /// Determines if this contact is a resource/service provider
        /// </summary>
        public static bool IsResource(this Contact contact)
        {
            return contact.RelationshipState == RelationshipState.ServiceProvider ||
                   contact.RelationshipState == RelationshipState.Agent;
        }

        /// <summary>
        /// Calculates a performance score for resource ranking
        /// </summary>
        public static decimal GetResourcePerformanceScore(this Contact contact)
        {
            if (!contact.IsResource()) return 0;

            var baseScore = contact.Rating ?? 2.5m;
            var reviewBonus = Math.Min(contact.ReviewCount * 0.1m, 1.0m);
            var preferredBonus = contact.IsPreferred ? 0.5m : 0;

            return Math.Min(baseScore + reviewBonus + preferredBonus, 5.0m);
        }

        /// <summary>
        /// Checks if contact matches resource criteria
        /// </summary>
        public static bool MatchesResourceCriteria(this Contact contact, string specialty, string? serviceArea = null, decimal? minRating = null)
        {
            if (!contact.IsResource()) return false;

            // Check specialty match
            if (!string.IsNullOrEmpty(specialty) && 
                !contact.Specialties.Any(s => s.Contains(specialty, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Check service area match
            if (!string.IsNullOrEmpty(serviceArea) && 
                contact.ServiceAreas.Any() &&
                !contact.ServiceAreas.Any(a => a.Contains(serviceArea, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Check minimum rating
            if (minRating.HasValue && (contact.Rating ?? 0) < minRating.Value)
            {
                return false;
            }

            return true;
        }
    }
}
