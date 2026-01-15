namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire
{
    /// <summary>
    /// Questionnaire file upload response DTO
    /// </summary>
    public class QuestionnaireFileUploadResponseDto
    {
        /// <summary>
        /// File ID (unique identifier)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Whether upload was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// File access URL
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// Generated file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File path on server
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File content type (MIME type)
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// File category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// File hash value for integrity verification
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// Upload timestamp
        /// </summary>
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// Error message if upload failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Current gateway/server information
        /// </summary>
        public string Gateway { get; set; }

        /// <summary>
        /// Full access URL with gateway prefix
        /// </summary>
        public string FullAccessUrl { get; set; }
    }
}