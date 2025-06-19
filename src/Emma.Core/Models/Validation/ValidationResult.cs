using System;
using System.Collections.Generic;
using System.Linq;

namespace Emma.Core.Models.Validation
{
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<ValidationError> _errors = new List<ValidationError>();

        /// <summary>
        /// Gets a value indicating whether the validation was successful (no errors).
        /// </summary>
        public bool IsValid => !_errors.Any();

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public IReadOnlyCollection<ValidationError> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Gets the most severe validation error level in the result.
        /// </summary>
        public ValidationSeverity? MaxSeverity => _errors.Any() 
            ? _errors.Max(e => e.Severity)
            : (ValidationSeverity?)null;

        /// <summary>
        /// Gets a value indicating whether there are any validation errors with Error severity.
        /// </summary>
        public bool HasErrors => _errors.Any(e => e.Severity == ValidationSeverity.Error);

        /// <summary>
        /// Gets a value indicating whether there are any validation warnings.
        /// </summary>
        public bool HasWarnings => _errors.Any(e => e.Severity == ValidationSeverity.Warning);

        /// <summary>
        /// Gets a value indicating whether there are any informational messages.
        /// </summary>
        public bool HasInfo => _errors.Any(e => e.Severity == ValidationSeverity.Info);

        /// <summary>
        /// Gets all errors with the specified severity.
        /// </summary>
        /// <param name="severity">The severity level to filter by.</param>
        /// <returns>A collection of validation errors with the specified severity.</returns>
        public IEnumerable<ValidationError> GetErrors(ValidationSeverity severity) 
            => _errors.Where(e => e.Severity == severity);

        /// <summary>
        /// Adds a validation error.
        /// </summary>
        /// <param name="propertyName">The name of the property that failed validation.</param>
        /// <param name="errorMessage">The validation error message.</param>
        /// <param name="severity">The severity of the validation error (defaults to Error).</param>
        /// <param name="errorCode">An optional error code for programmatic handling.</param>
        /// <returns>The current ValidationResult instance for method chaining.</returns>
        public ValidationResult AddError(
            string propertyName, 
            string errorMessage, 
            ValidationSeverity severity = ValidationSeverity.Error,
            string errorCode = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(propertyName));
            
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message cannot be null or whitespace.", nameof(errorMessage));
            
            _errors.Add(new ValidationError(propertyName, errorMessage, severity, errorCode));
            return this;
        }

        /// <summary>
        /// Adds a validation error for the entire object.
        /// </summary>
        /// <param name="errorMessage">The validation error message.</param>
        /// <param name="severity">The severity of the validation error (defaults to Error).</param>
        /// <param name="errorCode">An optional error code for programmatic handling.</param>
        /// <returns>The current ValidationResult instance for method chaining.</returns>
        public ValidationResult AddError(
            string errorMessage,
            ValidationSeverity severity = ValidationSeverity.Error,
            string errorCode = null)
            => AddError(string.Empty, errorMessage, severity, errorCode);

        /// <summary>
        /// Adds multiple validation errors.
        /// </summary>
        /// <param name="errors">The validation errors to add.</param>
        /// <returns>The current ValidationResult instance for method chaining.</returns>
        public ValidationResult AddErrors(IEnumerable<ValidationError> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));
                
            _errors.AddRange(errors.Where(e => e != null));
            return this;
        }

        /// <summary>
        /// Combines this validation result with another validation result.
        /// </summary>
        /// <param name="other">The other validation result to combine with.</param>
        /// <returns>The current ValidationResult instance for method chaining.</returns>
        public ValidationResult Combine(ValidationResult other)
        {
            if (other == null)
                return this;
                
            _errors.AddRange(other._errors);
            return this;
        }

        /// <summary>
        /// Throws a ValidationException if the validation result is not valid.
        /// </summary>
        /// <param name="message">An optional message to include in the exception.</param>
        /// <returns>The current ValidationResult instance for method chaining.</returns>
        /// <exception cref="ValidationException">Thrown if the validation result is not valid.</exception>
        public ValidationResult ThrowIfInvalid(string message = null)
        {
            if (!IsValid)
            {
                throw new ValidationException(message ?? "Validation failed.", this);
            }
            return this;
        }

        /// <summary>
        /// Creates a new successful validation result.
        /// </summary>
        public static ValidationResult Success() => new ValidationResult();

        /// <summary>
        /// Creates a new failed validation result with the specified error.
        /// </summary>
        /// <param name="propertyName">The name of the property that failed validation.</param>
        /// <param name="errorMessage">The validation error message.</param>
        /// <param name="severity">The severity of the validation error (defaults to Error).</param>
        /// <param name="errorCode">An optional error code for programmatic handling.</param>
        public static ValidationResult Failure(
            string propertyName,
            string errorMessage,
            ValidationSeverity severity = ValidationSeverity.Error,
            string errorCode = null)
            => new ValidationResult().AddError(propertyName, errorMessage, severity, errorCode);

        /// <summary>
        /// Creates a new failed validation result with the specified error for the entire object.
        /// </summary>
        /// <param name="errorMessage">The validation error message.</param>
        /// <param name="severity">The severity of the validation error (defaults to Error).</param>
        /// <param name="errorCode">An optional error code for programmatic handling.</param>
        public static ValidationResult Failure(
            string errorMessage,
            ValidationSeverity severity = ValidationSeverity.Error,
            string errorCode = null)
            => Failure(string.Empty, errorMessage, severity, errorCode);

        /// <summary>
        /// Creates a new failed validation result with the specified errors.
        /// </summary>
        /// <param name="errors">The validation errors to include.</param>
        public static ValidationResult Failure(IEnumerable<ValidationError> errors)
            => new ValidationResult().AddErrors(errors);

        /// <summary>
        /// Creates a new validation result that is the combination of multiple validation results.
        /// </summary>
        /// <param name="results">The validation results to combine.</param>
        public static ValidationResult Combine(params ValidationResult[] results)
        {
            if (results == null || results.Length == 0)
                return Success();
                
            var combined = new ValidationResult();
            foreach (var result in results)
            {
                if (result != null)
                {
                    combined.AddErrors(result.Errors);
                }
            }
            return combined;
        }

        /// <summary>
        /// Returns a string that represents the current validation result.
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
                return "Validation successful";
                
            var errorCounts = _errors
                .GroupBy(e => e.Severity)
                .OrderByDescending(g => g.Key)
                .Select(g => $"{g.Count()} {g.Key}(s)")
                .ToList();
                
            var errorSummary = string.Join(", ", errorCounts);
            var errorDetails = string.Join("; ", _errors.Select(e => e.ToString()));
            
            return $"Validation failed with {errorSummary}: {errorDetails}";
        }
    }

    /// <summary>
    /// Exception thrown when validation fails.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the validation result that caused the exception.
        /// </summary>
        public ValidationResult ValidationResult { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="validationResult">The validation result that caused the exception.</param>
        public ValidationException(string message, ValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
        }
    }
}
