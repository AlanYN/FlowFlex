using System;

namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// Onboarding文件输出DTO
    /// </summary>
    public class OnboardingFileOutputDto
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Onboarding ID
        /// </summary>
        public long OnboardingId { get; set; }

        /// <summary>
        /// Stage ID
        /// </summary>
        public long? StageId { get; set; }

        /// <summary>
        /// Stage名称
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// 文件原始名称
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// 存储文件名
        /// </summary>
        public string StoredFileName { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// 文件大小 (字节)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件大小 (格式化显示)
        /// </summary>
        public string FileSizeFormatted { get; set; }

        /// <summary>
        /// 文件类型 (MIME类型)
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 文件分类
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 文件描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否为必需文件
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 文件标签
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// 文件访问URL
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// 下载URL
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 上传人ID
        /// </summary>
        public string UploadedById { get; set; }

        /// <summary>
        /// 上传人姓名
        /// </summary>
        public string UploadedByName { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTimeOffset UploadedDate { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTimeOffset? LastModifiedDate { get; set; }

        /// <summary>
        /// 文件状态 (Active, Deleted, Archived)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Is External Import - Indicates if the file was imported from external system
        /// </summary>
        public bool IsExternalImport { get; set; }

        /// <summary>
        /// Source of the file (e.g., CRM, Portal, Manual)
        /// </summary>
        public string Source { get; set; }
    }
}