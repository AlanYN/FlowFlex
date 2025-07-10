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
        /// Message (for backward compatibility)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Msg (new field for frontend compatibility)
        /// </summary>
        public string Msg { get; set; }

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
                Msg = message, // 同时设置两个字段
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
                Msg = message, // 同时设置两个字段
                Data = default
            };
        }
    }
}