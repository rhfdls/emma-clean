using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents a physical address associated with a contact or organization in the system.
/// </summary>
public class Address : BaseEntity
{
    /// <summary>
    /// Gets or sets the first line of the street address.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Street1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the second line of the street address (e.g., apartment or suite number).
    /// </summary>
    [MaxLength(200)]
    public string? Street2 { get; set; }

    /// <summary>
    /// Gets or sets the city or locality.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or region (e.g., province, state, or region).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country (ISO 3166-1 alpha-2 or alpha-3 code).
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string Country { get; set; } = "US";

    /// <summary>
    /// Gets or sets the type of address (e.g., Home, Work, Billing, Shipping).
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary address.
    /// </summary>
    public bool IsPrimary { get; set; }

    
    /// <summary>
    /// Gets or sets any additional notes about this address.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    
    /// <summary>
    /// Gets or sets the ID of the contact this address belongs to.
    /// </summary>
    public Guid? ContactId { get; set; }
    
    /// <summary>
    /// Gets or sets the contact this address belongs to.
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the organization this address belongs to.
    /// </summary>
    public Guid? OrganizationId { get; set; }
    
    /// <summary>
    /// Gets or sets the organization this address belongs to.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }
}
