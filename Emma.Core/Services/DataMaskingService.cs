using Emma.Models.Interfaces;
using Emma.Models.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Emma.Core.Extensions;
using System.Text.Json;

namespace Emma.Core.Services;

public class DataMaskingService : IDataMaskingService
{
    private readonly ILogger<DataMaskingService> _logger;
    private readonly PrivacySettings _privacySettings;

    public DataMaskingService(ILogger<DataMaskingService> logger, IOptions<PrivacySettings> privacySettings)
    {
        _logger = logger;
        _privacySettings = privacySettings.Value;
    }

    public Contact MaskContact(Contact contact, MaskingLevel level = MaskingLevel.Standard)
    {
        if (level == MaskingLevel.None)
            return contact;

        return new Contact
        {
            Id = contact.Id,
            FirstName = MaskText(contact.FirstName, level),
            LastName = MaskText(contact.LastName, level),
            Emails = MaskEmails(contact.Emails, level),
            Phones = MaskPhones(contact.Phones, level),
            Address = MaskAddress(contact.Address, level),
            RelationshipState = contact.RelationshipState,
            IsActiveClient = contact.IsActiveClient,
            ClientSince = contact.ClientSince,
            CompanyName = MaskText(contact.CompanyName ?? "", level),
            LicenseNumber = level >= MaskingLevel.Standard ? "[MASKED]" : contact.LicenseNumber,
            Specialties = contact.Specialties, // Business data, not PII
            ServiceAreas = contact.ServiceAreas, // Geographic data, typically not sensitive
            Rating = contact.Rating, // Business data, not PII
            ReviewCount = contact.ReviewCount,
            IsPreferred = contact.IsPreferred,
            Website = contact.Website, // Public information
            AgentId = contact.AgentId,
            Tags = contact.Tags, // Tags are typically not PII
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt,
            // Navigation properties - preserve references but don't deep mask
            Interactions = contact.Interactions,
            StateHistory = contact.StateHistory,
            AssignedResources = contact.AssignedResources,
            ResourceAssignments = contact.ResourceAssignments,
            Collaborators = contact.Collaborators,
            // CollaboratingOn = contact.CollaboratingOn  // Temporarily ignored in EF Core
        };
    }

    public Interaction MaskInteraction(Interaction interaction, MaskingLevel level = MaskingLevel.Standard)
    {
        if (level == MaskingLevel.None)
            return interaction;

        return new Interaction
        {
            Id = interaction.Id,
            ContactId = interaction.ContactId,
            OrganizationId = interaction.OrganizationId,
            ContactFirstName = MaskText(interaction.ContactFirstName, level),
            ContactLastName = MaskText(interaction.ContactLastName, level),
            CreatedAt = interaction.CreatedAt,
            ExternalIds = interaction.ExternalIds, // Business identifiers, not PII
            Type = interaction.Type, // Business data
            Direction = interaction.Direction, // Business data
            Timestamp = interaction.Timestamp,
            AgentId = interaction.AgentId,
            Content = level >= MaskingLevel.Standard ? "[MASKED]" : interaction.Content,
            Channel = interaction.Channel, // Business data
            Status = interaction.Status, // Business data
            RelatedEntities = interaction.RelatedEntities, // Business data
            Tags = interaction.Tags, // Business/privacy tags
            CustomFields = interaction.CustomFields, // May contain PII, but structure preserved
            Messages = interaction.Messages, // Navigation property
            // Navigation properties
            Contact = interaction.Contact,
            Agent = interaction.Agent,
            Organization = interaction.Organization
        };
    }

    public IEnumerable<Contact> MaskContacts(IEnumerable<Contact> contacts, MaskingLevel level = MaskingLevel.Standard)
    {
        return contacts.Select(c => MaskContact(c, level));
    }

    public IEnumerable<Interaction> MaskInteractions(IEnumerable<Interaction> interactions, MaskingLevel level = MaskingLevel.Standard)
    {
        return interactions.Select(i => MaskInteraction(i, level));
    }

    public IEnumerable<Message> MaskMessages(IEnumerable<Message> messages, MaskingLevel level = MaskingLevel.Standard)
    {
        // TODO: Implement proper data masking based on actual Message model structure
        // Temporarily returning original messages to avoid build errors
        _logger.LogInformation("Message collection masking stubbed - returning original messages");
        return messages;
        
        /*
        if (level == MaskingLevel.None)
            return messages;

        return messages.Select(m => new Message
        {
            Id = m.Id,
            InteractionId = m.InteractionId,
            Type = m.Type, // Required property
            // TODO: Fix property mappings based on actual Message model
            // ... other properties
        });
        */
    }

    public string MaskText(string text, MaskingLevel level = MaskingLevel.Standard)
    {
        if (string.IsNullOrEmpty(text) || level == MaskingLevel.None)
            return text;

        return level switch
        {
            MaskingLevel.Partial => text.Length <= 2 ? "**" : text[0] + new string('*', text.Length - 2) + text[^1],
            MaskingLevel.Standard => text.Length <= 1 ? "*" : text[0] + new string('*', Math.Max(1, text.Length - 1)),
            MaskingLevel.Full => new string('*', Math.Max(1, text.Length)),
            _ => text
        };
    }

    public string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return email;

        var localPart = parts[0];
        var domain = parts[1];

        var maskedLocal = localPart.Length <= 2 ? "**" : localPart[0] + new string('*', localPart.Length - 2) + localPart[^1];
        return $"{maskedLocal}@{domain}";
    }

    public string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return phoneNumber;

        // Remove all non-digits
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        if (digits.Length < 4)
            return new string('*', phoneNumber.Length);

        // Show last 4 digits, mask the rest
        var masked = new string('*', digits.Length - 4) + digits[^4..];
        
        // Try to preserve original formatting
        var result = phoneNumber;
        var digitIndex = 0;
        for (int i = 0; i < phoneNumber.Length && digitIndex < masked.Length; i++)
        {
            if (char.IsDigit(phoneNumber[i]))
            {
                result = result.Remove(i, 1).Insert(i, masked[digitIndex].ToString());
                digitIndex++;
            }
        }
        
        return result;
    }

    private List<EmailAddress> MaskEmails(List<EmailAddress> emails, MaskingLevel level)
    {
        if (level == MaskingLevel.None)
            return emails;

        return emails.Select(email => new EmailAddress
        {
            Id = email.Id,
            Address = MaskEmail(email.Address),
            Type = email.Type,
            Verified = email.Verified,
            ContactId = email.ContactId,
            Contact = email.Contact
        }).ToList();
    }

    private List<PhoneNumber> MaskPhones(List<PhoneNumber> phones, MaskingLevel level)
    {
        if (level == MaskingLevel.None)
            return phones;

        return phones.Select(phone => new PhoneNumber
        {
            Id = phone.Id,
            Number = MaskPhoneNumber(phone.Number),
            Type = phone.Type,
            Verified = phone.Verified
        }).ToList();
    }

    private Address? MaskAddress(Address? address, MaskingLevel level)
    {
        if (address == null || level == MaskingLevel.None)
            return address;

        return new Address
        {
            Id = address.Id,
            Street = MaskText(address.Street, level),
            City = MaskText(address.City, level),
            State = address.State, // State codes typically not sensitive
            PostalCode = level >= MaskingLevel.Full ? "****" : address.PostalCode,
            Country = address.Country // Country codes typically not sensitive
        };
    }

    public MaskingLevel GetMaskingLevel(string? agentId = null, bool isProduction = false)
    {
        // In production, always use configured default level or higher
        if (isProduction)
        {
            return _privacySettings.DefaultMaskingLevel;
        }

        // In development, allow more flexible masking
        if (string.IsNullOrEmpty(agentId))
        {
            return MaskingLevel.Partial; // Default for anonymous access
        }

        // TODO: Implement agent-specific masking level logic
        // For now, return the configured default
        return _privacySettings.DefaultMaskingLevel;
    }

    public string ToMaskedJson(object obj, MaskingLevel level)
    {
        if (obj == null)
            return "null";

        try
        {
            // For high security levels, return minimal information
            if (level >= MaskingLevel.Full)
            {
                return "{\"data\":\"[REDACTED]\"}";
            }

            // Serialize the object (which should already be masked)
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            return JsonSerializer.Serialize(obj, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize object to masked JSON");
            return "{\"error\":\"[SERIALIZATION_ERROR]\"}";
        }
    }
}
