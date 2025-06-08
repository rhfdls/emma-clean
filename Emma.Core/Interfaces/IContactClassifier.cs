using Emma.Data.Models;

namespace Emma.Core.Interfaces;

/// <summary>
/// Classifies contacts and interactions as business or personal
/// </summary>
public interface IContactClassifier
{
    /// <summary>
    /// Classify a contact based on available information
    /// </summary>
    Task<ContactClassificationResult> ClassifyContactAsync(Guid contactId, string? phoneNumber = null, string? content = null);
    
    /// <summary>
    /// Classify an interaction as business or personal
    /// </summary>
    Task<InteractionClassificationResult> ClassifyInteractionAsync(string content, Guid? contactId = null);
}

/// <summary>
/// Result of contact classification
/// </summary>
public class ContactClassificationResult
{
    public ContactType Type { get; set; }
    public RelationshipState SuggestedRelationshipState { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
}

/// <summary>
/// Result of interaction classification
/// </summary>
public class InteractionClassificationResult
{
    public InteractionType Type { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool ShouldSyncToCrm { get; set; }
}

/// <summary>
/// Contact type classification
/// </summary>
public enum ContactType
{
    Business,
    Personal,
    Mixed,
    Unknown
}

/// <summary>
/// Interaction type classification
/// </summary>
public enum InteractionType
{
    Business,
    Personal,
    Urgent,
    Routine
}
