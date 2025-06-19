using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Models.Models
{
    /// <summary>
    /// Represents an email address associated with a user or contact in the system.
    /// </summary>
    public class EmailAddress : BaseEntity
    {
        /// <summary>
        /// Gets or sets the email address value.
        /// </summary>
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is the primary email address.
        /// </summary>
        public bool IsPrimary { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether this email address has been verified.
        /// </summary>
        public bool IsVerified { get; set; }


        /// <summary>
        /// Gets or sets the type of the email address (e.g., Personal, Work, Other).
        /// </summary>
        [MaxLength(50)]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user this email address belongs to.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user this email address belongs to.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }

        /// <summary>
        /// Gets or sets the ID of the contact this email address belongs to.
        /// </summary>
        public Guid? ContactId { get; set; }

        /// <summary>
        /// Gets or sets the contact this email address belongs to.
        /// </summary>
        [ForeignKey(nameof(ContactId))]
        public virtual Contact Contact { get; set; }

        /// <summary>
        /// Gets or sets the date and time when this email address was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when this email address was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
