namespace Emma.Data.Models;

public class Contact
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<EmailAddress> Emails { get; set; } = new();
    public List<PhoneNumber> Phones { get; set; } = new();
    public Address? Address { get; set; }
    /// <summary>
    /// Segmentation tags only (e.g., VIP, Buyer, Region). DO NOT use for privacy/business logic (CRM, PERSONAL, PRIVATE, etc.).
    /// All privacy/business logic must be enforced via Interaction.Tags.
    /// </summary>
    [Emma.Data.Validation.NoPrivacyBusinessTags]
    public List<string> Tags { get; set; } = new();
    public string? LeadSource { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string>? CustomFields { get; set; }
    // NOTE: PrivacyLevel property has been removed. Run EF Core migration to drop the PrivacyLevel column from the database.
}

public class EmailAddress
{
    public string Address { get; set; } = string.Empty;
    public string Type { get; set; } = "primary"; // primary|work|personal|other
    public bool Verified { get; set; } = false;
}

public class PhoneNumber
{
    public string Number { get; set; } = string.Empty;
    public string Type { get; set; } = "mobile"; // mobile|work|home|other
    public bool Verified { get; set; } = false;
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

