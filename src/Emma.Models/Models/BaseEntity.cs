using System;
using System.ComponentModel.DataAnnotations;

namespace Emma.Models.Models
{
    /// <summary>
    /// Base class for all entity types in the system.
    /// Provides common properties and behavior for all entities.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Gets or sets the primary key for this entity.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the date and time when this entity was created.
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when this entity was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }

        
        /// <summary>
        /// Gets or sets the ID of the user who created this entity.
        /// </summary>
        public Guid? CreatedById { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the user who last updated this entity.
        /// </summary>
        public Guid? UpdatedById { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this entity is deleted (soft delete).
        /// </summary>
        public bool IsDeleted { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time when this entity was deleted (for soft delete).
        /// </summary>
        public DateTimeOffset? DeletedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the user who deleted this entity.
        /// </summary>
        public Guid? DeletedById { get; set; }
        
        /// <summary>
        /// Gets or sets the concurrency token for optimistic concurrency control.
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
