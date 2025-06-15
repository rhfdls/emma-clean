using System.Collections.Generic;
using System.Linq;
using Emma.Core.Models.Validation;

namespace Emma.Core.Models.Results
{
    /// <summary>
    /// Represents the result of a service operation.
    /// </summary>
    public class ServiceResult
    {
        private static readonly ServiceResult _successResult = new ServiceResult { Succeeded = true };
        private List<ValidationError> _errors = new List<ValidationError>();

        /// <summary>
        /// Flag indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; protected set; }

        /// <summary>
        /// Gets the error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; protected set; }

        /// <summary>
        /// Gets the collection of validation errors if the operation failed.
        /// </summary>
        public IReadOnlyCollection<ValidationError> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Gets a value indicating whether the operation has validation errors.
        /// </summary>
        public bool HasErrors => _errors.Any();

        /// <summary>
        /// Returns a success result.
        /// </summary>
        public static ServiceResult Success() => _successResult;

        /// <summary>
        /// Returns a failure result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public static ServiceResult Failure(string errorMessage)
            => new ServiceResult { Succeeded = false, ErrorMessage = errorMessage };

        /// <summary>
        /// Returns a failure result with the specified validation errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public static ServiceResult Failure(IEnumerable<ValidationError> errors)
            => new ServiceResult { Succeeded = false } .WithErrors(errors);

        /// <summary>
        /// Returns a failure result with the specified error message and validation errors.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errors">The validation errors.</param>
        public static ServiceResult Failure(string errorMessage, IEnumerable<ValidationError> errors)
            => new ServiceResult { Succeeded = false, ErrorMessage = errorMessage }.WithErrors(errors);

        /// <summary>
        /// Returns a not found result.
        /// </summary>
        /// <param name="message">The not found message.</param>
        public static ServiceResult NotFound(string message = "The requested resource was not found.")
            => new ServiceResult { Succeeded = false, ErrorMessage = message };

        /// <summary>
        /// Returns a not implemented result.
        /// </summary>
        /// <param name="message">The not implemented message.</param>
        public static ServiceResult NotImplemented(string message = "This feature is not implemented yet.")
            => new ServiceResult { Succeeded = false, ErrorMessage = message };

        /// <summary>
        /// Returns an unauthorized result.
        /// </summary>
        /// <param name="message">The unauthorized message.</param>
        public static ServiceResult Unauthorized(string message = "You are not authorized to perform this action.")
            => new ServiceResult { Succeeded = false, ErrorMessage = message };

        /// <summary>
        /// Adds validation errors to the result.
        /// </summary>
        /// <param name="errors">The validation errors to add.</param>
        public ServiceResult WithErrors(IEnumerable<ValidationError> errors)
        {
            if (errors != null)
            {
                _errors = _errors.Concat(errors).ToList();
            }
            return this;
        }

        /// <summary>
        /// Adds a validation error to the result.
        /// </summary>
        /// <param name="propertyName">The name of the property that failed validation.</param>
        /// <param name="errorMessage">The validation error message.</param>
        public ServiceResult WithError(string propertyName, string errorMessage)
        {
            _errors.Add(new ValidationError(propertyName, errorMessage));
            return this;
        }
    }

    /// <summary>
    /// Represents the result of a service operation that returns a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class ServiceResult<T> : ServiceResult
    {
        /// <summary>
        /// Gets the value of the result.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Returns a success result with the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public static ServiceResult<T> Success(T value)
            => new ServiceResult<T> { Succeeded = true, Value = value };

        /// <summary>
        /// Returns a failure result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        public new static ServiceResult<T> Failure(string errorMessage)
            => new ServiceResult<T> { Succeeded = false, ErrorMessage = errorMessage };

        /// <summary>
        /// Returns a failure result with the specified validation errors.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public new static ServiceResult<T> Failure(IEnumerable<ValidationError> errors)
            => new ServiceResult<T> { Succeeded = false }.WithErrors(errors) as ServiceResult<T>;

        /// <summary>
        /// Returns a failure result with the specified error message and validation errors.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="errors">The validation errors.</param>
        public new static ServiceResult<T> Failure(string errorMessage, IEnumerable<ValidationError> errors)
            => new ServiceResult<T> { Succeeded = false, ErrorMessage = errorMessage }.WithErrors(errors) as ServiceResult<T>;

        /// <summary>
        /// Returns a not found result.
        /// </summary>
        /// <param name="message">The not found message.</param>
        public new static ServiceResult<T> NotFound(string message = "The requested resource was not found.")
            => new ServiceResult<T> { Succeeded = false, ErrorMessage = message };

        /// <summary>
        /// Returns a not implemented result.
        /// </summary>
        /// <param name="message">The not implemented message.</param>
        public new static ServiceResult<T> NotImplemented(string message = "This feature is not implemented yet.")
            => new ServiceResult<T> { Succeeded = false, ErrorMessage = message };

        /// <summary>
        /// Returns an unauthorized result.
        /// </summary>
        /// <param name="message">The unauthorized message.</param>
        public new static ServiceResult<T> Unauthorized(string message = "You are not authorized to perform this action.")
            => new ServiceResult<T> { Succeeded = false, ErrorMessage = message };

        /// <summary>
        /// Adds validation errors to the result.
        /// </summary>
        /// <param name="errors">The validation errors to add.</param>
        public new ServiceResult<T> WithErrors(IEnumerable<ValidationError> errors)
        {
            base.WithErrors(errors);
            return this;
        }

        /// <summary>
        /// Adds a validation error to the result.
        /// </summary>
        /// <param name="propertyName">The name of the property that failed validation.</param>
        /// <param name="errorMessage">The validation error message.</param>
        public new ServiceResult<T> WithError(string propertyName, string errorMessage)
        {
            base.WithError(propertyName, errorMessage);
            return this;
        }
    }
}
