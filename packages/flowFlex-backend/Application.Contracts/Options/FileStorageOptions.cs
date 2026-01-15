namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// File storage configuration options
    /// </summary>
    public class FileStorageOptions
    {
        /// <summary>
        /// Storage type: Local, Cloud, FTP
        /// </summary>
        public string StorageType { get; set; } = "Local";

        /// <summary>
        /// Local storage root path
        /// </summary>
        public string LocalStoragePath { get; set; } = "wwwroot/uploads";

        /// <summary>
        /// File access URL prefix
        /// </summary>
        public string FileUrlPrefix { get; set; } = "/uploads";

        /// <summary>
        /// Allowed file extensions (comma-separated)
        /// </summary>
        public string AllowedExtensions { get; set; } = ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip,.rar,.mp4,.avi,.mov,.eml,.msg";

        /// <summary>
        /// Maximum file size (bytes)
        /// </summary>
        public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50MB

        /// <summary>
        /// Enable file name encryption
        /// </summary>
        public bool EnableFileNameEncryption { get; set; } = true;

        /// <summary>
        /// Group files by date
        /// </summary>
        public bool GroupByDate { get; set; } = true;

        /// <summary>
        /// Enable access control
        /// </summary>
        public bool EnableAccessControl { get; set; } = true;

        /// <summary>
        /// Temporary file retention time (hours)
        /// </summary>
        public int TempFileRetentionHours { get; set; } = 24;
    }
}
