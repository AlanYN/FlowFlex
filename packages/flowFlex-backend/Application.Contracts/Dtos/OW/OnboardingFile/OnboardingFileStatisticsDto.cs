using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile
{
    /// <summary>
    /// Onboarding文件统计信息DTO
    /// </summary>
    public class OnboardingFileStatisticsDto
    {
        /// <summary>
        /// 总文件数量
        /// </summary>
        public int TotalFileCount { get; set; }

        /// <summary>
        /// 总文件大小 (字节)
        /// </summary>
        public long TotalFileSize { get; set; }

        /// <summary>
        /// 总文件大小 (格式化显示)
        /// </summary>
        public string TotalFileSizeFormatted { get; set; }

        /// <summary>
        /// 按分类统计
        /// </summary>
        public Dictionary<string, int> FileCountByCategory { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 按Stage统计
        /// </summary>
        public Dictionary<string, int> FileCountByStage { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 按文件类型统计
        /// </summary>
        public Dictionary<string, int> FileCountByType { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// 必需文件数量
        /// </summary>
        public int RequiredFileCount { get; set; }

        /// <summary>
        /// 可选文件数量
        /// </summary>
        public int OptionalFileCount { get; set; }

        /// <summary>
        /// 活跃文件数量
        /// </summary>
        public int ActiveFileCount { get; set; }

        /// <summary>
        /// 已归档文件数量
        /// </summary>
        public int ArchivedFileCount { get; set; }

        /// <summary>
        /// 最近上传的文件
        /// </summary>
        public List<OnboardingFileOutputDto> RecentFiles { get; set; } = new List<OnboardingFileOutputDto>();
    }
}