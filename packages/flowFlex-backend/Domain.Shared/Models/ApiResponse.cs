using System.Text.Json.Serialization;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// API response model
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Data
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Success flag
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Msg { get; set; }

        /// <summary>
        /// Status code
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Create success response
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="message">Message</param>
        /// <returns>API response</returns>
        public static ApiResponse<T> Ok(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                Data = data,
                Success = true,
                Msg = message,
                Code = 200
            };
        }

        /// <summary>
        /// Create failure response
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="errorCode">Error code</param>
        /// <returns>API response</returns>
        public static ApiResponse<T> Fail(string message = "Operation failed", int errorCode = 400)
        {
            return new ApiResponse<T>
            {
                Data = default,
                Success = false,
                Msg = message,
                Code = errorCode
            };
        }
    }
}