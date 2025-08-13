using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents a phone number associated with a contact in the system.
/// </summary>
public class PhoneNumber : BaseEntity
{
    /// <summary>
    /// Gets or sets the phone number value in E.164 format (e.g., +14155552671).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of phone number (e.g., Mobile, Home, Work).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "Mobile";

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary phone number.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this phone number has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    // SPRINT2: For DbContext alignment
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets any additional notes about this phone number.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    
    /// <summary>
    /// Gets or sets the ID of the contact this phone number belongs to.
    /// </summary>
    public Guid? ContactId { get; set; }
    
    /// <summary>
    /// Gets or sets the contact this phone number belongs to.
    /// </summary>
    [ForeignKey(nameof(ContactId))]
    public virtual Contact? Contact { get; set; }
    

}
