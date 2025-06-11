using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Emma.Core.Extensions
{
    /// <summary>
    /// Extension methods for serializing userOverrides for LLM prompts and audit logging
    /// </summary>
    public static class UserOverrideExtensions
    {
        /// <summary>
        /// Serializes userOverrides to a structured string for LLM prompt inclusion
        /// </summary>
        /// <param name="userOverrides">User override preferences</param>
        /// <param name="maxLength">Maximum length for LLM prompt (default 4KB)</param>
        /// <returns>Structured string representation for LLM consumption</returns>
        public static string SerializeForLLMPrompt(this Dictionary<string, object> userOverrides, int maxLength = 4096)
        {
            if (userOverrides == null || !userOverrides.Any())
                return "No user overrides specified.";

            try
            {
                var structured = new StringBuilder();
                structured.AppendLine("User Override Preferences:");
                
                foreach (var kvp in userOverrides.Take(20)) // Limit entries for prompt size
                {
                    var value = kvp.Value?.ToString() ?? "null";
                    if (value.Length > 100) value = value.Substring(0, 97) + "...";
                    structured.AppendLine($"- {kvp.Key}: {value}");
                }

                if (userOverrides.Count > 20)
                    structured.AppendLine($"... and {userOverrides.Count - 20} more preferences");

                var result = structured.ToString();
                return result.Length > maxLength ? result.Substring(0, maxLength - 3) + "..." : result;
            }
            catch (Exception)
            {
                return "Error serializing user overrides for LLM prompt.";
            }
        }

        /// <summary>
        /// Serializes userOverrides to JSON for audit logging
        /// </summary>
        /// <param name="userOverrides">User override preferences</param>
        /// <param name="maxLength">Maximum length for audit log (default 1KB)</param>
        /// <returns>JSON string for audit trail</returns>
        public static string SerializeForAuditLog(this Dictionary<string, object> userOverrides, int maxLength = 1024)
        {
            if (userOverrides == null || !userOverrides.Any())
                return "{}";

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(userOverrides, options);
                return json.Length > maxLength ? json.Substring(0, maxLength - 3) + "..." : json;
            }
            catch (Exception)
            {
                return "{\"error\":\"Failed to serialize userOverrides\"}";
            }
        }

        /// <summary>
        /// Validates userOverrides for security and size constraints
        /// </summary>
        /// <param name="userOverrides">User override preferences to validate</param>
        /// <returns>Validation result with any issues</returns>
        public static (bool IsValid, string[] Issues) ValidateUserOverrides(this Dictionary<string, object> userOverrides)
        {
            var issues = new List<string>();

            if (userOverrides == null)
            {
                issues.Add("UserOverrides cannot be null");
                return (false, issues.ToArray());
            }

            // Size validation
            if (userOverrides.Count > 50)
                issues.Add($"Too many override entries: {userOverrides.Count} (max 50)");

            // Key validation
            foreach (var key in userOverrides.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    issues.Add("Override keys cannot be null or empty");
                else if (key.Length > 100)
                    issues.Add($"Override key too long: {key.Substring(0, 20)}... (max 100 chars)");
            }

            // Value validation
            foreach (var kvp in userOverrides)
            {
                var valueStr = kvp.Value?.ToString();
                if (valueStr != null && valueStr.Length > 1000)
                    issues.Add($"Override value too long for key '{kvp.Key}' (max 1000 chars)");
            }

            return (issues.Count == 0, issues.ToArray());
        }
    }
}
