namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// Import files from URL result DTO
    /// </summary>
    public class ImportFilesResultDto
    {
        /// <summary>
        /// Total number of files to import
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Number of successfully imported files
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of failed imports
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// Overall progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage => TotalCount > 0 ? (int)((SuccessCount + FailedCount) * 100.0 / TotalCount) : 0;

        /// <summary>
        /// Whether all files have been processed
        /// </summary>
        public bool IsCompleted => (SuccessCount + FailedCount) >= TotalCount;

        /// <summary>
        /// Details of each file import
        /// </summary>
        public List<ImportFileProgressItemDto> Items { get; set; } = new List<ImportFileProgressItemDto>();
    }

    /// <summary>
    /// Single file import progress item
    /// </summary>
    public class ImportFileProgressItemDto
    {
        /// <summary>
        /// Original download link
        /// </summary>
        public string DownloadLink { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Import status (Pending, Downloading, Processing, Success, Failed)
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Progress percentage for this file (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Imported file ID (if successful)
        /// </summary>
        public long? FileId { get; set; }

        /// <summary>
        /// Imported file output (if successful)
        /// </summary>
        public OnboardingFileOutputDto FileOutput { get; set; }
    }
}

