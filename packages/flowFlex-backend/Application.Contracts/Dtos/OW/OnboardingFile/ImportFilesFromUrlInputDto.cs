using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// Import files from URL input DTO
    /// </summary>
    public class ImportFilesFromUrlInputDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>
        [Required]
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID (required)
        /// </summary>
        [Required(ErrorMessage = "Stage ID is required")]
        public long StageId { get; set; }

        /// <summary>
        /// List of files to import
        /// </summary>
        [Required]
        public List<ImportFileItemDto> Files { get; set; } = new List<ImportFileItemDto>();

        /// <summary>
        /// File category (Document, Image, Certificate, Other)
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; } = "Document";

        /// <summary>
        /// File description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Operator ID (set by controller, not from request)
        /// </summary>
        public string OperatorId { get; set; }

        /// <summary>
        /// Operator display name (set by controller, not from request)
        /// </summary>
        public string OperatorName { get; set; }

        /// <summary>
        /// Tenant ID (set by controller from X-Tenant-Id header, used for background task)
        /// </summary>
        public string TenantId { get; set; }
    }

    /// <summary>
    /// Single file item for import
    /// </summary>
    public class ImportFileItemDto
    {
        /// <summary>
        /// Download link (OSS URL with signature)
        /// </summary>
        [Required]
        public string DownloadLink { get; set; }

        /// <summary>
        /// Original file name (optional, will be extracted from URL if not provided)
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File description (optional)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Source of the file (e.g., CRM, Portal, Manual)
        /// </summary>
        public string Source { get; set; }
    }
}

