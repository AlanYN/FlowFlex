using System;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared.Const;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Helpers
{
    /// <summary>
    /// Helper class for retry operations with exponential backoff
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Execute an async operation with retry logic
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="logger">Logger for recording retry attempts</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>Result of the operation</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            ILogger logger,
            string operationName,
            int maxRetries = StageConditionConstants.MaxRetryAttempts)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsTransientException(ex) && attempt < maxRetries)
                {
                    lastException = ex;
                    var delay = CalculateBackoffDelay(attempt);
                    
                    logger.LogWarning(ex, 
                        "{OperationName} failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms. Error: {ErrorMessage}",
                        operationName, attempt, maxRetries, delay, ex.Message);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    // Non-transient exception or last attempt - don't retry
                    logger.LogError(ex, 
                        "{OperationName} failed on attempt {Attempt}/{MaxRetries}. Error: {ErrorMessage}",
                        operationName, attempt, maxRetries, ex.Message);
                    throw;
                }
            }

            // Should not reach here, but just in case
            throw lastException ?? new InvalidOperationException($"{operationName} failed after {maxRetries} attempts");
        }

        /// <summary>
        /// Execute an async operation with retry logic (no return value)
        /// </summary>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            ILogger logger,
            string operationName,
            int maxRetries = StageConditionConstants.MaxRetryAttempts)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await operation();
                return true;
            }, logger, operationName, maxRetries);
        }

        /// <summary>
        /// Execute an async operation with retry logic, returning a result with success/failure info
        /// </summary>
        public static async Task<RetryResult<T>> ExecuteWithRetryResultAsync<T>(
            Func<Task<T>> operation,
            ILogger logger,
            string operationName,
            int maxRetries = StageConditionConstants.MaxRetryAttempts)
        {
            Exception lastException = null;
            int totalAttempts = 0;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                totalAttempts = attempt;
                try
                {
                    var result = await operation();
                    return new RetryResult<T>
                    {
                        Success = true,
                        Result = result,
                        Attempts = attempt
                    };
                }
                catch (Exception ex) when (IsTransientException(ex) && attempt < maxRetries)
                {
                    lastException = ex;
                    var delay = CalculateBackoffDelay(attempt);
                    
                    logger.LogWarning(ex, 
                        "{OperationName} failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms",
                        operationName, attempt, maxRetries, delay);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    logger.LogError(ex, 
                        "{OperationName} failed on attempt {Attempt}/{MaxRetries}",
                        operationName, attempt, maxRetries);
                    break;
                }
            }

            return new RetryResult<T>
            {
                Success = false,
                Exception = lastException,
                ErrorMessage = lastException?.Message ?? "Unknown error",
                Attempts = totalAttempts
            };
        }

        /// <summary>
        /// Calculate exponential backoff delay with jitter
        /// </summary>
        private static int CalculateBackoffDelay(int attempt)
        {
            // Exponential backoff: baseDelay * 2^(attempt-1)
            var exponentialDelay = StageConditionConstants.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
            
            // Cap at max delay
            var cappedDelay = Math.Min(exponentialDelay, StageConditionConstants.RetryMaxDelayMs);
            
            // Add jitter (±20%) to prevent thundering herd
            var jitter = new Random().Next(-cappedDelay / 5, cappedDelay / 5);
            
            return Math.Max(100, cappedDelay + jitter);
        }

        /// <summary>
        /// Determine if an exception is transient and should be retried
        /// </summary>
        private static bool IsTransientException(Exception ex)
        {
            // Network-related exceptions
            if (ex is System.Net.Http.HttpRequestException)
                return true;

            // Timeout exceptions
            if (ex is TimeoutException || ex is TaskCanceledException)
                return true;

            // IO exceptions (often transient)
            if (ex is System.IO.IOException)
                return true;

            // Check inner exception
            if (ex.InnerException != null && IsTransientException(ex.InnerException))
                return true;

            // Check exception message for common transient patterns
            var message = ex.Message?.ToLower() ?? "";
            if (message.Contains("timeout") || 
                message.Contains("connection") ||
                message.Contains("temporarily unavailable") ||
                message.Contains("service unavailable") ||
                message.Contains("too many requests") ||
                message.Contains("rate limit"))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Result of a retry operation
    /// </summary>
    public class RetryResult<T>
    {
        public bool Success { get; set; }
        public T Result { get; set; }
        public Exception Exception { get; set; }
        public string ErrorMessage { get; set; }
        public int Attempts { get; set; }
    }
}
