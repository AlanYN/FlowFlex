namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// File import task DTO - represents an ongoing or completed import task
    /// </summary>
    public class FileImportTaskDto
    {
        /// <summary>
        /// Unique task ID
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Task status (Pending, InProgress, Completed, PartiallyCompleted, Failed, Cancelled)
        /// </summary>
        public string Status { get; set; } = "Pending";

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
        /// Number of cancelled imports
        /// </summary>
        public int CancelledCount { get; set; }

        /// <summary>
        /// Overall progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage => TotalCount > 0 ? (int)((SuccessCount + FailedCount + CancelledCount) * 100.0 / TotalCount) : 0;

        /// <summary>
        /// Whether the task is completed (all files processed)
        /// </summary>
        public bool IsCompleted => (SuccessCount + FailedCount + CancelledCount) >= TotalCount;

        /// <summary>
        /// Task creation time
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Task completion time (if completed)
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }

        /// <summary>
        /// Created by user name
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Details of each file import
        /// </summary>
        public List<FileImportItemDto> Items { get; set; } = new List<FileImportItemDto>();
    }

    /// <summary>
    /// Single file import item in a task
    /// </summary>
    public class FileImportItemDto
    {
        /// <summary>
        /// Unique item ID within the task
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// Original download link
        /// </summary>
        public string DownloadLink { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Item status (Pending, Downloading, Processing, Success, Failed, Cancelled)
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

        /// <summary>
        /// Whether this item can be cancelled
        /// </summary>
        public bool CanCancel => Status == "Pending" || Status == "Downloading";
    }

    /// <summary>
    /// Start import task response
    /// </summary>
    public class StartImportTaskResponseDto
    {
        /// <summary>
        /// Task ID for tracking progress
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Total files to import
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Cancel import file request
    /// </summary>
    public class CancelImportFileRequestDto
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// Item ID to cancel (optional, if not provided cancels all pending items)
        /// </summary>
        public string ItemId { get; set; }
    }

    /// <summary>
    /// Cancel import file response
    /// </summary>
    public class CancelImportFileResponseDto
    {
        /// <summary>
        /// Whether the cancellation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Number of items cancelled
        /// </summary>
        public int CancelledCount { get; set; }
    }
}

