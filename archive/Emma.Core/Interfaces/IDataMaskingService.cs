using Emma.Models.Models;

namespace Emma.Core.Interfaces;

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
    /// Masks a collection of contacts.
    /// </summary>
    IEnumerable<Contact> MaskContacts(IEnumerable<Contact> contacts, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Masks a collection of interactions.
    /// </summary>
    IEnumerable<Interaction> MaskInteractions(IEnumerable<Interaction> interactions, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Masks a collection of messages.
    /// </summary>
    IEnumerable<Message> MaskMessages(IEnumerable<Message> messages, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Masks sensitive text content (emails, phone numbers, names).
    /// </summary>
    string MaskText(string text, MaskingLevel level = MaskingLevel.Standard);
    
    /// <summary>
    /// Masks email addresses.
    /// </summary>
    string MaskEmail(string email);
    
    /// <summary>
    /// Masks phone numbers.
    /// </summary>
    string MaskPhoneNumber(string phoneNumber);
    
    /// <summary>
    /// Gets the appropriate masking level for an agent.
    /// </summary>
    MaskingLevel GetMaskingLevel(string? agentId = null, bool isProduction = true);
    
    /// <summary>
    /// Converts an object to masked JSON representation.
    /// </summary>
    string ToMaskedJson(object obj, MaskingLevel level);
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
