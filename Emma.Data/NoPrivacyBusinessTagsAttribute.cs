using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Emma.Data.Validation
{
    /// <summary>
    /// Validation attribute to ensure Contact.Tags does not contain privacy/business tags (CRM, PERSONAL, PRIVATE).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NoPrivacyBusinessTagsAttribute : ValidationAttribute
    {
        private static readonly HashSet<string> ForbiddenTags = new HashSet<string>(
            new[] { "CRM", "PERSONAL", "PRIVATE" },
            StringComparer.OrdinalIgnoreCase);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is IEnumerable<string> tags)
            {
                var forbidden = tags.Where(tag => ForbiddenTags.Contains(tag)).ToList();
                if (forbidden.Any())
                {
                    return new ValidationResult($"Contact.Tags must not contain privacy/business tags: CRM, PERSONAL, PRIVATE. Found: {string.Join(", ", forbidden)}");
                }
            }
            return ValidationResult.Success;
        }
    }
}
