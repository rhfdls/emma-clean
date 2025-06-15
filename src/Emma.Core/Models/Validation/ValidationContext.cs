using System;
using System.Collections.Generic;

namespace Emma.Core.Models.Validation
{
    /// <summary>
    /// Provides context information for validation operations.
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// Gets or sets the ID of the user performing the validation.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the organization the validation is being performed for.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the contact associated with the validation, if applicable.
        /// </summary>
        public Guid? ContactId { get; set; }

        /// <summary>
        /// Gets or sets the type of operation being validated (Create, Update, Delete, etc.).
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform strict validation.
        /// When true, additional validation rules may be applied.
        /// </summary>
        public bool IsStrictValidation { get; set; } = true;

        /// <summary>
        /// Gets a dictionary of additional data that can be used during validation.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets a value indicating whether to include warnings in the validation result.
        /// When false, only errors will be included.
        /// </summary>
        public bool IncludeWarnings { get; set; } = true;

        /// <summary>
        /// Gets or sets the validation scope, which can be used to limit the scope of validation.
        /// For example, "basic", "full", or "custom".
        /// </summary>
        public string ValidationScope { get; set; } = "full";

        /// <summary>
        /// Gets or sets the correlation ID for tracing the validation operation.
        /// </summary>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets the timestamp when the validation context was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Adds additional data to the validation context.
        /// </summary>
        /// <param name="key">The key of the data item.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>The current validation context instance for method chaining.</returns>
        public ValidationContext WithAdditionalData(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

            AdditionalData[key] = value;
            return this;
        }

        /// <summary>
        /// Gets additional data from the validation context.
        /// </summary>
        /// <typeparam name="T">The type of the data to retrieve.</typeparam>
        /// <param name="key">The key of the data item.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The value associated with the specified key, or the default value if the key is not found.</returns>
        public T GetAdditionalData<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));

            if (AdditionalData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }
    }
}
