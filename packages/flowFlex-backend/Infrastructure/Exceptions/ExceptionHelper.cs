using System.Net;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception helper for common error scenarios
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Throw business validation error
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">HTTP status code</param>
        public static void ThrowValidationError(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            throw new CRMException(statusCode, message);
        }

        /// <summary>
        /// Throw data not found error
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <param name="id">Entity ID</param>
        public static void ThrowNotFound(string entityName, object id)
        {
            throw new CRMException(HttpStatusCode.NotFound, $"{entityName} with ID '{id}' was not found");
        }

        /// <summary>
        /// Throw data not found error with custom message
        /// </summary>
        /// <param name="message">Custom message</param>
        public static void ThrowNotFound(string message)
        {
            throw new CRMException(HttpStatusCode.NotFound, message);
        }

        /// <summary>
        /// Throw unauthorized access error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void ThrowUnauthorized(string message = "You are not authorized to perform this action")
        {
            throw new CRMException(HttpStatusCode.Unauthorized, message);
        }

        /// <summary>
        /// Throw forbidden access error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void ThrowForbidden(string message = "Access to this resource is forbidden")
        {
            throw new CRMException(HttpStatusCode.Forbidden, message);
        }

        /// <summary>
        /// Throw conflict error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void ThrowConflict(string message)
        {
            throw new CRMException(HttpStatusCode.Conflict, message);
        }

        /// <summary>
        /// Throw bad request error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void ThrowBadRequest(string message)
        {
            throw new CRMException(HttpStatusCode.BadRequest, message);
        }

        /// <summary>
        /// Throw internal server error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void ThrowInternalError(string message)
        {
            throw new CRMException(HttpStatusCode.InternalServerError, message);
        }

        /// <summary>
        /// Throw service unavailable error
        /// </summary>
        /// <param name="message">Error message</param>
        public static void ThrowServiceUnavailable(string message = "Service is temporarily unavailable")
        {
            throw new CRMException(HttpStatusCode.ServiceUnavailable, message);
        }

        /// <summary>
        /// Throw error when required parameter is null or empty
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        public static void ThrowIfNullOrEmpty(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(parameterName, $"Parameter '{parameterName}' cannot be null or empty");
            }
        }

        /// <summary>
        /// Throw error when required object is null
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <param name="parameterName">Parameter name</param>
        public static void ThrowIfNull(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, $"Parameter '{parameterName}' cannot be null");
            }
        }

        /// <summary>
        /// Throw error with specific error code
        /// </summary>
        /// <param name="errorCode">Error code from ErrorCodeEnum</param>
        /// <param name="parameters">Format parameters</param>
        public static void ThrowWithCode(ErrorCodeEnum errorCode, params string[] parameters)
        {
            throw new CRMException(errorCode, parameters);
        }

        /// <summary>
        /// Validate string length and throw if too long
        /// </summary>
        /// <param name="value">String value</param>
        /// <param name="maxLength">Maximum allowed length</param>
        /// <param name="fieldName">Field name for error message</param>
        public static void ValidateStringLength(string value, int maxLength, string fieldName)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                ThrowValidationError($"{fieldName} cannot exceed {maxLength} characters. Current length: {value.Length}");
            }
        }

        /// <summary>
        /// Validate that a number is within range
        /// </summary>
        /// <param name="value">Number value</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="fieldName">Field name for error message</param>
        public static void ValidateRange(decimal value, decimal min, decimal max, string fieldName)
        {
            if (value < min || value > max)
            {
                ThrowValidationError($"{fieldName} must be between {min} and {max}. Current value: {value}");
            }
        }

        /// <summary>
        /// Execute action and return result, converting any exception to CRM exception
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="action">Action to execute</param>
        /// <param name="errorMessage">Custom error message if action fails</param>
        /// <returns>Action result</returns>
        public static T ExecuteSafe<T>(Func<T> action, string errorMessage = "Operation failed")
        {
            try
            {
                return action();
            }
            catch (CRMException)
            {
                // Re-throw CRM exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions
                throw new CRMException(HttpStatusCode.InternalServerError, $"{errorMessage}: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute async action and return result, converting any exception to CRM exception
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="action">Async action to execute</param>
        /// <param name="errorMessage">Custom error message if action fails</param>
        /// <returns>Action result</returns>
        public static async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> action, string errorMessage = "Operation failed")
        {
            try
            {
                return await action();
            }
            catch (CRMException)
            {
                // Re-throw CRM exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions
                throw new CRMException(HttpStatusCode.InternalServerError, $"{errorMessage}: {ex.Message}");
            }
        }
    }
}