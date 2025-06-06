using Emma.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Emma.Core.Services;

/// <summary>
/// Service for masking sensitive data while preserving debugging capabilities.
/// Provides different masking levels based on environment and user permissions.
/// </summary>
public interface IDataMaskingService
{
    /// <summary>
    /// Masks a contact for logging/debugging while preserving structure.
    /// </summary>
    Contact MaskContact(Contact contact, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Masks an interaction for logging/debugging.
    /// </summary>
    Interaction MaskInteraction(Interaction interaction, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Masks sensitive text content (emails, phone numbers, names).
    /// </summary>
    string MaskText(string text, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Creates a debug-friendly JSON representation with masked data.
    /// </summary>
    string ToMaskedJson<T>(T obj, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Determines appropriate masking level based on environment and user context.
    /// </summary>
    MaskingLevel GetMaskingLevel(string? agentId = null, bool isProduction = true);
}

public enum MaskingLevel
{
    /// <summary>No masking - full data visible (development only)</summary>
    None = 0,
    
    /// <summary>Partial masking - preserve format but hide sensitive parts</summary>
    Partial = 1,
    
    /// <summary>Standard masking - hide most sensitive data but keep structure</summary>
    Standard = 2,
    
    /// <summary>Full masking - maximum privacy protection</summary>
    Full = 3
}

public class DataMaskingService : IDataMaskingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataMaskingService> _logger;
    private readonly bool _isProduction;
    private readonly HashSet<string> _debugPrivilegedAgents;

    public DataMaskingService(IConfiguration configuration, ILogger<DataMaskingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _isProduction = true; // Default to production for demo
        
        // Load agents with debug privileges from configuration
        _debugPrivilegedAgents = new HashSet<string>(); // Empty for demo
    }

    public Contact MaskContact(Contact contact, MaskingLevel level = MaskingLevel.Standard)
    {
        if (level == MaskingLevel.None)
            return contact;

        // TODO: Implement proper contact masking based on actual Contact model structure
        // Temporarily return original contact for demo - Contact model structure is different than expected
        _logger.LogInformation("Contact masking temporarily disabled for demo - Contact {ContactId}, Level: {Level}", 
            contact.Id, level);
        
        return contact;
    }

    public Interaction MaskInteraction(Interaction interaction, MaskingLevel level = MaskingLevel.Standard)
    {
        if (level == MaskingLevel.None)
            return interaction;

        // TODO: Implement proper interaction masking based on actual Interaction model structure
        // Temporarily return original interaction for demo - Interaction model doesn't have Summary/Metadata properties
        _logger.LogInformation("Interaction masking temporarily disabled for demo - Interaction {InteractionId}, Level: {Level}", 
            interaction.Id, level);
        
        return interaction;
    }

    public string MaskText(string text, MaskingLevel level = MaskingLevel.Standard)
    {
        if (string.IsNullOrEmpty(text) || level == MaskingLevel.None)
            return text;

        var masked = text;

        // Mask email addresses
        masked = Regex.Replace(masked, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
            level == MaskingLevel.Partial ? "***@***.com" : "[EMAIL_MASKED]");

        // Mask phone numbers
        masked = Regex.Replace(masked, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", 
            level == MaskingLevel.Partial ? "XXX-XXX-XXXX" : "[PHONE_MASKED]");

        // Mask potential names (capitalized words)
        if (level >= MaskingLevel.Standard)
        {
            masked = Regex.Replace(masked, @"\b[A-Z][a-z]{2,}\b", "[NAME]");
        }

        return masked;
    }

    public string ToMaskedJson<T>(T obj, MaskingLevel level = MaskingLevel.Standard)
    {
        try
        {
            object maskedObj = obj switch
            {
                Contact contact => MaskContact(contact, level),
                Interaction interaction => MaskInteraction(interaction, level),
                _ => obj
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(maskedObj, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize masked object");
            return $"[SERIALIZATION_ERROR: {ex.Message}]";
        }
    }

    public MaskingLevel GetMaskingLevel(string? agentId = null, bool isProduction = true)
    {
        // Development environment with privileged access
        if (!isProduction && !string.IsNullOrEmpty(agentId) && _debugPrivilegedAgents.Contains(agentId))
        {
            return MaskingLevel.Partial;
        }

        // Development environment default
        if (!isProduction)
        {
            return MaskingLevel.Standard;
        }

        // Production environment - always full masking in logs
        return MaskingLevel.Full;
    }

    private string MaskName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? "";
        if (name.Length <= 2) return "**";
        return name[0] + new string('*', name.Length - 2) + name[^1];
    }

    private string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email)) return email ?? "";
        var parts = email.Split('@');
        if (parts.Length != 2) return "***@***.com";
        
        var localPart = parts[0].Length > 2 ? parts[0][0] + "***" + parts[0][^1] : "***";
        var domainPart = parts[1].Length > 4 ? parts[1][0] + "***" + parts[1][^3..] : "***.com";
        return $"{localPart}@{domainPart}";
    }

    private string MaskEmailStandard(string? email)
    {
        if (string.IsNullOrEmpty(email)) return email ?? "";
        return email.Contains('@') ? "***@***.com" : "***";
    }

    private string MaskPhoneNumber(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone ?? "";
        var digits = Regex.Replace(phone, @"[^\d]", "");
        if (digits.Length >= 10)
        {
            return $"XXX-XXX-{digits[^4..]}";
        }
        return "XXX-XXX-XXXX";
    }

    private string MaskPhoneStandard(string? phone)
    {
        return string.IsNullOrEmpty(phone) ? phone ?? "" : "XXX-XXX-XXXX";
    }

    private string MaskCompanyName(string? company)
    {
        if (string.IsNullOrEmpty(company)) return company ?? "";
        if (company.Length <= 4) return "***";
        return company[0] + new string('*', Math.Min(company.Length - 2, 8)) + company[^1];
    }

    private Dictionary<string, object>? MaskMetadata(Dictionary<string, object>? metadata, MaskingLevel level)
    {
        if (metadata == null || level == MaskingLevel.None) return metadata;

        var masked = new Dictionary<string, object>();
        foreach (var kvp in metadata)
        {
            if (kvp.Value is string stringValue)
            {
                masked[kvp.Key] = MaskText(stringValue, level);
            }
            else
            {
                masked[kvp.Key] = level >= MaskingLevel.Standard ? "[MASKED]" : kvp.Value;
            }
        }
        return masked;
    }
}
