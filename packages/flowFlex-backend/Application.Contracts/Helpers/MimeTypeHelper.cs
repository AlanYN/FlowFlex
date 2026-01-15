namespace FlowFlex.Application.Contracts.Helpers
{
    /// <summary>
    /// Helper class for MIME type operations
    /// Centralized management of file extension to MIME type mappings
    /// </summary>
    public static class MimeTypeHelper
    {
        /// <summary>
        /// Extension to MIME type mapping dictionary
        /// </summary>
        private static readonly Dictionary<string, string> MimeTypeMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".bmp", "image/bmp" },
            { ".webp", "image/webp" },
            { ".tiff", "image/tiff" },
            { ".svg", "image/svg+xml" },
            
            // Documents
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".md", "text/markdown" },
            { ".json", "application/json" },
            { ".xml", "application/xml" },
            { ".html", "text/html" },
            { ".css", "text/css" },
            { ".js", "text/javascript" },
            
            // Archives
            { ".zip", "application/zip" },
            { ".rar", "application/x-rar-compressed" },
            { ".7z", "application/x-7z-compressed" },
            
            // Videos
            { ".mp4", "video/mp4" },
            { ".avi", "video/x-msvideo" },
            { ".mov", "video/quicktime" },
            { ".wmv", "video/x-ms-wmv" },
            
            // Audio
            { ".mp3", "audio/mpeg" },
            { ".wav", "audio/wav" },
            
            // Email
            { ".eml", "message/rfc822" },
            { ".msg", "application/vnd.ms-outlook" }
        };

        /// <summary>
        /// MIME type to extension mapping (reverse lookup)
        /// </summary>
        private static readonly Dictionary<string, string> ExtensionMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { "image/jpeg", ".jpg" },
            { "image/png", ".png" },
            { "image/gif", ".gif" },
            { "image/bmp", ".bmp" },
            { "image/webp", ".webp" },
            { "image/tiff", ".tiff" },
            { "image/svg+xml", ".svg" },
            
            // Documents
            { "application/pdf", ".pdf" },
            { "application/msword", ".doc" },
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
            { "application/vnd.ms-excel", ".xls" },
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" },
            { "application/vnd.ms-powerpoint", ".ppt" },
            { "application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx" },
            { "text/plain", ".txt" },
            { "text/csv", ".csv" },
            { "text/markdown", ".md" },
            { "application/json", ".json" },
            { "application/xml", ".xml" },
            { "text/html", ".html" },
            { "text/css", ".css" },
            { "text/javascript", ".js" },
            { "application/javascript", ".js" },
            
            // Archives
            { "application/zip", ".zip" },
            { "application/x-rar-compressed", ".rar" },
            { "application/x-7z-compressed", ".7z" },
            
            // Videos
            { "video/mp4", ".mp4" },
            { "video/x-msvideo", ".avi" },
            { "video/quicktime", ".mov" },
            { "video/x-ms-wmv", ".wmv" },
            { "video/avi", ".avi" },
            { "video/mov", ".mov" },
            
            // Audio
            { "audio/mpeg", ".mp3" },
            { "audio/wav", ".wav" },
            
            // Email
            { "message/rfc822", ".eml" },
            { "application/vnd.ms-outlook", ".msg" },
            
            // Default binary
            { "application/octet-stream", ".bin" }
        };

        /// <summary>
        /// Binary content types that should be auto-downloaded
        /// </summary>
        private static readonly HashSet<string> BinaryContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/zip",
            "application/x-rar-compressed",
            "application/x-7z-compressed",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/octet-stream",
            "application/vnd.ms-outlook",
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp",
            "image/tiff",
            "image/webp",
            "audio/mpeg",
            "audio/wav",
            "video/mp4",
            "video/x-msvideo",
            "video/quicktime",
            "video/avi",
            "video/mov"
        };

        /// <summary>
        /// Get MIME type for a file extension
        /// </summary>
        /// <param name="extension">File extension (with or without leading dot)</param>
        /// <returns>MIME type string, defaults to application/octet-stream if not found</returns>
        public static string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            // Ensure extension starts with dot
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return MimeTypeMappings.TryGetValue(extension.ToLowerInvariant(), out var mimeType)
                ? mimeType
                : "application/octet-stream";
        }

        /// <summary>
        /// Get MIME type from file name
        /// </summary>
        /// <param name="fileName">File name with extension</param>
        /// <returns>MIME type string</returns>
        public static string GetMimeTypeFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            var extension = Path.GetExtension(fileName);
            return GetMimeType(extension);
        }

        /// <summary>
        /// Validate if content type matches the file extension
        /// </summary>
        /// <param name="contentType">Content type from request</param>
        /// <param name="extension">File extension</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidMimeType(string contentType, string extension)
        {
            if (string.IsNullOrEmpty(contentType) || string.IsNullOrEmpty(extension))
                return false;

            // Ensure extension starts with dot
            if (!extension.StartsWith("."))
                extension = "." + extension;

            extension = extension.ToLowerInvariant();
            var expectedContentType = GetMimeType(extension);

            // Exact match
            if (contentType.Equals(expectedContentType, StringComparison.OrdinalIgnoreCase))
                return true;

            // Allow generic binary type for any file
            if (contentType == "application/octet-stream")
                return true;

            // Allow MIME type category matches
            return contentType switch
            {
                var ct when ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
                           IsImageExtension(extension) => true,
                var ct when ct.StartsWith("video/", StringComparison.OrdinalIgnoreCase) &&
                           IsVideoExtension(extension) => true,
                var ct when ct.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) &&
                           IsAudioExtension(extension) => true,
                var ct when ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase) &&
                           IsTextExtension(extension) => true,
                var ct when ct.StartsWith("message/", StringComparison.OrdinalIgnoreCase) &&
                           IsEmailExtension(extension) => true,
                var ct when ct.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
                           IsApplicationExtension(extension) => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if extension is an image type
        /// </summary>
        public static bool IsImageExtension(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            return imageExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Check if extension is a video type
        /// </summary>
        public static bool IsVideoExtension(string extension)
        {
            var videoExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv" };
            return videoExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Check if extension is an audio type
        /// </summary>
        public static bool IsAudioExtension(string extension)
        {
            var audioExtensions = new[] { ".mp3", ".wav" };
            return audioExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Check if extension is a text type
        /// </summary>
        public static bool IsTextExtension(string extension)
        {
            var textExtensions = new[] { ".txt", ".csv", ".md", ".json", ".xml" };
            return textExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Check if extension is an email type
        /// </summary>
        public static bool IsEmailExtension(string extension)
        {
            var emailExtensions = new[] { ".eml", ".msg" };
            return emailExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Check if extension is an application type (documents, archives, etc.)
        /// </summary>
        public static bool IsApplicationExtension(string extension)
        {
            var appExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".7z", ".msg" };
            return appExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Get all supported extensions
        /// </summary>
        public static IEnumerable<string> GetAllSupportedExtensions()
        {
            return MimeTypeMappings.Keys;
        }

        /// <summary>
        /// Get file extension from MIME type (reverse lookup)
        /// </summary>
        /// <param name="contentType">MIME type string</param>
        /// <returns>File extension with leading dot, defaults to .bin if not found</returns>
        public static string GetExtensionFromMimeType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return ".bin";

            return ExtensionMappings.TryGetValue(contentType.ToLowerInvariant(), out var extension)
                ? extension
                : ".bin";
        }

        /// <summary>
        /// Check if content type is a binary type that should be auto-downloaded
        /// </summary>
        /// <param name="contentType">MIME type string</param>
        /// <returns>True if binary content type</returns>
        public static bool IsBinaryContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            return BinaryContentTypes.Contains(contentType.ToLowerInvariant());
        }

        /// <summary>
        /// Get all binary content types
        /// </summary>
        public static IEnumerable<string> GetBinaryContentTypes()
        {
            return BinaryContentTypes;
        }
    }
}
