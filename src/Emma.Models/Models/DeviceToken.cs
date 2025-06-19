using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models;

/// <summary>
/// Represents a device token used for push notifications and device identification.
/// </summary>
public class DeviceToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the user ID this device token is associated with.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier for the device.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the push notification token for the device.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the platform of the device (e.g., "ios", "android", "web").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the name of the device (e.g., "David's iPhone").
    /// </summary>
    [MaxLength(100)]
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Gets or sets the model of the device (e.g., "iPhone 13").
    /// </summary>
    [MaxLength(100)]
    public string? DeviceModel { get; set; }
    
    /// <summary>
    /// Gets or sets the operating system version (e.g., "15.4.1").
    /// </summary>
    [MaxLength(50)]
    public string? OsVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the application version (e.g., "1.0.0").
    /// </summary>
    [MaxLength(50)]
    public string? AppVersion { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this device is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the date and time when this device token was last used.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
    
    // Navigation properties
    
    /// <summary>
    /// Gets or sets the user associated with this device token.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
