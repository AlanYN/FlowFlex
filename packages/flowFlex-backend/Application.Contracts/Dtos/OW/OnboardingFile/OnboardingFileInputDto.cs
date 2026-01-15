using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// Onboarding文件上传输入DTO
    /// </summary>
    public class OnboardingFileInputDto
    {
        /// <summary>
        /// Onboarding ID
        /// </summary>

        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID (可选，如果指定则关联到特定Stage)
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// 上传的文件
        /// </summary>

        public IFormFile FormFile { get; set; }

        /// <summary>
        /// 文件分类 (Document, Image, Certificate, Other)
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; } = "Document";

        /// <summary>
        /// 文件描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否为必需文件
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 文件标签 (用于分组或搜索)
        /// </summary>
        [StringLength(200)]
        public string Tags { get; set; }

        /// <summary>
        /// Override uploader ID (for background import tasks)
        /// If not set, uses current user context
        /// </summary>
        public string OverrideUploaderId { get; set; }

        /// <summary>
        /// Override uploader name (for background import tasks)
        /// If not set, uses current user context
        /// </summary>
        public string OverrideUploaderName { get; set; }

        /// <summary>
        /// Override tenant ID (for background import tasks)
        /// If not set, uses current user context
        /// </summary>
        public string OverrideTenantId { get; set; }

        /// <summary>
        /// Is External Import - Indicates if the file was imported from external system (e.g., CRM)
        /// Files with IsExternalImport=true will be excluded from inbound-attachments API
        /// </summary>
        public bool IsExternalImport { get; set; } = false;

        /// <summary>
        /// Source of the file (e.g., CRM, Portal, Manual)
        /// </summary>
        [StringLength(100)]
        public string Source { get; set; }
    }

    /// <summary>
    /// Onboarding文件更新输入DTO
    /// </summary>
    public class OnboardingFileUpdateInputDto
    {
        /// <summary>
        /// 文件分类 (Document, Image, Certificate, Other)
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; }

        /// <summary>
        /// 文件描述
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// 是否为必需文件
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 文件标签 (用于分组或搜索)
        /// </summary>
        [StringLength(200)]
        public string Tags { get; set; }

        /// <summary>
        /// 文件状态 (Active, Archived, Deleted)
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; }
    }
}