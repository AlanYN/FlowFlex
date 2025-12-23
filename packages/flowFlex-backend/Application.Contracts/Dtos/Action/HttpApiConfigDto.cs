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
        /// Request parameters (query params for GET, or body params for POST/PUT/PATCH when body is empty)
        /// </summary>
        public Dictionary<string, string>? Params { get; set; }

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Whether to follow redirects
        /// </summary>
        public bool FollowRedirects { get; set; } = true;

        /// <summary>
        /// Whether to automatically download and save response content as file
        /// </summary>
        public bool AutoDownloadFile { get; set; } = false;

        /// <summary>
        /// File category for storage (used when AutoDownloadFile is true)
        /// </summary>
        public string FileCategory { get; set; } = "HttpApiDownload";

        /// <summary>
        /// Custom file name for downloaded file (optional, if not provided, will be generated)
        /// </summary>
        public string? CustomFileName { get; set; }

        /// <summary>
        /// Maximum file size to download in bytes (default: 100MB)
        /// </summary>
        public long MaxDownloadSize { get; set; } = 104857600; // 100MB
    }
}