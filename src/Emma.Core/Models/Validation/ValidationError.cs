using System;

namespace Emma.Core.Models.Validation
{
    /// <summary>
    /// Represents a validation error that occurred during validation.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets the name of the property that failed validation.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the validation error message.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the severity level of the validation error.
        /// </summary>
        public ValidationSeverity Severity { get; }

        /// <summary>
        /// Gets the error code associated with this validation error.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationError"/> class.
        /// </summary>
        /// <param name="propertyName">The name of the property that failed validation.</param>
        /// <param name="errorMessage">The validation error message.</param>
        /// <param name="severity">The severity of the validation error.</param>
        /// <param name="errorCode">An optional error code for programmatic handling.</param>
        public ValidationError(
            string propertyName, 
            string errorMessage, 
            ValidationSeverity severity = ValidationSeverity.Error,
            string errorCode = null)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            Severity = severity;
            ErrorCode = errorCode ?? $"VALIDATION_{severity}";
        }

        /// <summary>
        /// Returns a string that represents the current validation error.
        /// </summary>
        public override string ToString()
        {
            return $"{PropertyName}: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Defines the severity levels for validation errors.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Indicates an informational message that doesn't prevent validation from succeeding.
        /// </summary>
        Info,

        /// <summary>
        /// Indicates a warning that might need attention but doesn't prevent validation from succeeding.
        /// </summary>
        Warning,


        /// <summary>
        /// Indicates a validation error that prevents the operation from succeeding.
        /// </summary>
        Error
    }
}
