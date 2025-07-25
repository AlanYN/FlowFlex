namespace FlowFlex.Application.Contracts.Dtos.Action
{
    /// <summary>
    /// HTTP API action configuration DTO
    /// </summary>
    public class HttpApiConfigDto
    {
        /// <summary>
        /// Target URL for the HTTP request
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method (GET, POST, PUT, DELETE, PATCH)
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// HTTP headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Request body content
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Whether to follow redirects
        /// </summary>
        public bool FollowRedirects { get; set; } = true;
    }
} 