namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// API response model
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Status code
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Data
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Create success response
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="message">Message</param>
        /// <returns>API response</returns>
        public static ApiResponse<T> Success(T data, string message = "Operation successful")
        {
            return new ApiResponse<T>
            {
                Code = 200,
                Message = message,
                Data = data
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
                Code = errorCode,
                Message = message,
                Data = default
            };
        }
    }
}